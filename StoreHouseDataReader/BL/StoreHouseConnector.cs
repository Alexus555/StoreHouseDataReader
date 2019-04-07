using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExceptionLogging;

namespace StoreHouseDataReader.BL
{
    public class StoreHouseConnector : IDisposable
    {
        Sh4Ole.SH4App sH;

        public Boolean IsConnected { get; private set; }

        //
        private Dictionary<int, string> cashe = new Dictionary<int, string>();

        private SHConnectorException lastException;

        public SHConnectorException LastException
        {
            get { return lastException; }
            private set
            {
                lastException = value;
                ExceptionLoggingService.Instance.WriteLog(value);
            }
        }

        #region Init
        public StoreHouseConnector()
        {
            sH = new Sh4Ole.SH4App();
            IsConnected = false;
        }

        public void Connect(string connectionString)
        {
            string[] connParamsArray;

            try
            {
                connParamsArray = connectionString.Split(';');
                Dictionary<string, string> connectionParams = connParamsArray.Select(s => s.Split('=')).ToDictionary(arr => arr[0].ToLower(), arr => arr[1]);

                Connect(
                    connectionParams["address"],
                    connectionParams["port"],
                    connectionParams["protocol"],
                    connectionParams["user"],
                    connectionParams["psw"]
                    );
            }
            catch (Exception e)
            {
                LastException = new SHConnectorException("Connect Method Exception", e);
                throw LastException;
            }
        }

        public void Connect(string address, string port, string protocol, string user, string password)
        {
            string timeOut = "30000";

            try
            {
                sH.SetServerName(address + protocol + port + "t" + timeOut);

                IsConnected = sH.DBLoginEx(user, password) == 0;

                if (!IsConnected)
                {
                    throw new SHConnectorException(sH.GetExcMessage(), sH.GetExcCode());
                };
            }
            catch (Exception e)
            {
                LastException = new SHConnectorException("Connect Method Exception", e);
                throw LastException;
            }
        }

        #endregion

        #region GetData
        public IEnumerable<StoreHouseData> GetGoodsWithCostPrice(DateTime beginDate, DateTime endDate)
        {
            List<StoreHouseData> result = new List<StoreHouseData>();

            if(!IsConnected)
            {
                LastException = new SHConnectorException("Not connected", -1);
                throw LastException;
            };

            string period = $"{beginDate} - {endDate}:";

            try
            {
                var shQuery = sH.DocFList(beginDate.ToOADate(), endDate.ToOADate(), 0, 1, 0);
                if (shQuery < 0)
                {
                    LastException = new SHConnectorException(sH.GetExcMessage(), sH.GetExcCode());
                    throw LastException;
                }

                //int recordCount = sH.RecordCount(shQuery);
                //int countProcessed = 0;

                while (sH.EOF(shQuery) != 1)
                {
                    //countProcessed ++;

                    //if (countProcessed % 5 == 0)
                    //{
                    //    Console.SetCursorPosition(0, Console.CursorTop);

                    //    Console.Write($"{period} Processed {countProcessed} from {recordCount} records");
                    //};

                    int DocType = sH.ValByName(shQuery, "1.103.10.1");
                    if (DocType != 12)
                    {
                        sH.Next(shQuery);
                        continue;
                    }

                    int docKey = sH.ValByName(shQuery, "1.103.1.1");

                    GetGoodsFromDoc(docKey, ref result);

                    sH.Next(shQuery);
                }

                sH.CloseQuery(shQuery);
            }
            catch(Exception e)
            {
                lastException = new SHConnectorException("Connect Method Exception", e);
                throw LastException;
            };

            result = (from item in result
                     group item by new { Date = item.Date, Code = item.Code } into grouped
                     select new StoreHouseData()
                     {
                         Date = grouped.Key.Date,
                         Code = grouped.Key.Code,
                         CostPrice = grouped.Average(x => x.CostPrice)
                     }).ToList<StoreHouseData>();

            return result;
        }

        private void GetGoodsFromDoc(int docKey, ref List<StoreHouseData> storeHouseDatas)
        {
            int IndProc = sH.pr_CreateProc("Doc12");

            int docRecOptions = Convert.ToInt32("1000", 2);

            sH.pr_SetValByName(IndProc, 0, "103.1.1", docKey); //RID
            sH.pr_SetValByName(IndProc, 0, "103.11.1", docRecOptions);  //Доп параметры
            sH.pr_Post(IndProc, 0);
            sH.pr_ExecuteProc(IndProc);
            
            if(sH.GetExcCode() != 0)
            {
                throw new SHConnectorException(sH.GetExcMessage(), sH.GetExcCode());
            };

            if(sH.pr_RecordCount(IndProc, 1) == 0)
            {
                throw new Exception($"Document {docKey} not found!");
            };

            DateTime docDate = Convert.ToDateTime(sH.pr_ValByName(IndProc, 1, "103.3.1"));

            int dataSet = 2;

            while (sH.pr_EOF(IndProc, dataSet) != 1 && sH.GetExcCode() == 0)
            {
                int goodId = sH.pr_ValByName(IndProc, dataSet, "210.1.1");

                GetGoodInfoById(goodId, out string code);

                double summ = sH.pr_ValByName(IndProc, dataSet, "105.4.8");
                double vat = sH.pr_ValByName(IndProc, dataSet, "105.5.8");
                double count = sH.pr_ValByName(IndProc, dataSet, "105.3.0");

                storeHouseDatas
                    .Add(
                        new StoreHouseData()
                        {
                            Date = docDate,
                            Code = code,
                            CostPrice = count == 0 ? 0 : summ / count
                        });

                sH.pr_Next(IndProc, dataSet);
            };

            sH.pr_CloseProc(IndProc);

        }

        private void GetGoodInfoById(int id, out string code)
        {
            code = String.Empty;
            //name = String.Empty;

            if(cashe.ContainsKey(id))
            {
                code = cashe[id];
                return;
            };

            int indProc = sH.GoodByRID(id);
            code = sH.ValByName(indProc, "1.210.3.0");//Код
            //name = sH.ValByName(indProc, "1.210.2.0");//наименование
            sH.pr_CloseProc(indProc);

            cashe.Add(id, code);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Disconnect()
        {
            IsConnected = false;

            try
            {
                sH.DBLogout();
            }
            catch (Exception e)
            {
                LastException = new SHConnectorException("Disonnect Method exception", e);
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                    sH = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StoreHouseConnector() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
