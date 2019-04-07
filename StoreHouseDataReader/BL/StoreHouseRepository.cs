using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreHouseDataReader.BL
{
    public static class StoreHouseRepository
    {
        public static void WriteToCSV(string fileName, IEnumerable<StoreHouseData> storeHouseDatas)
        {
            using (StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine("Date;Code;Cost price;");

                foreach (var data in storeHouseDatas)
                {
                    sw.WriteLine($"{data.Date};{data.Code};{data.CostPrice};");
                }

            }
        }
    }
}
