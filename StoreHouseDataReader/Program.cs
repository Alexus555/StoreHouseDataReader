using System;
using System.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoreHouseDataReader.BL;
using ExceptionLogging;
using System.Threading;

namespace StoreHouseDataReader
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Unhandled;

            var connectionString = ConfigurationManager.ConnectionStrings["SHConnectionString"].ConnectionString;

            string fileName = ConfigurationManager.AppSettings.Get("FileNameForSaving");
            
            //var storeHouseData = GetData(connectionString, DateTime.Today.AddDays(-10), DateTime.Today);

            TaskFactory taskFactory = new TaskFactory();
            List<Task<IEnumerable<StoreHouseData>>> taskList = new List<Task<IEnumerable<StoreHouseData>>>();

            int maxParallelTask = 100;
            DateTime date = DateTime.Today.AddDays(-maxParallelTask);
            TimeSpan timeSpan = new TimeSpan(23, 59, 59);

            for(int i = 1; i <= maxParallelTask; i++)
            {
                DateTime newdate = date.AddDays(i);
                taskList.Add(GetDataAsync(connectionString, newdate, newdate.Add(timeSpan),i));
            }

            Task.WaitAll(taskList.ToArray());

            List<StoreHouseData> storeHouseData = new List<StoreHouseData>();
            foreach(var task in taskList)
            {
                if (task.IsCompleted)
                {
                    storeHouseData.AddRange(task.Result);
                }
            }

            Console.SetCursorPosition(0, maxParallelTask + 1);
            Console.WriteLine("Saving data to file...");
            StoreHouseRepository.WriteToCSV(fileName, storeHouseData);

        }

        private async static Task<IEnumerable<StoreHouseData>> GetDataAsync(string connectionString, DateTime beginDate, DateTime endDate, int priority)
        {
            IEnumerable<StoreHouseData> result = await Task.Run(() => GetData(connectionString, beginDate, endDate, priority));

            return result;
        }

        private static IEnumerable<StoreHouseData> GetData(string connectionString, DateTime beginDate, DateTime endDate, int priority)
        {
            IEnumerable<StoreHouseData> storeHouseData = new List<StoreHouseData>();

            string period = $"{beginDate} - {endDate}:";

            using (StoreHouseConnector storeHouse = new StoreHouseConnector())
            {
                try
                {
                    Thread.Sleep(new Random().Next(10,15) * 10);
                    Console.SetCursorPosition(0, priority);
                    Console.WriteLine($"{period} Connecting to StoreHouse...");

                    storeHouse.Connect(connectionString);

                    Console.SetCursorPosition(0, priority);
                    Console.Write($"{period} Getting data from StoreHouse...");

                    storeHouseData = storeHouse.GetGoodsWithCostPrice(beginDate, endDate);

                    Console.SetCursorPosition(0, priority);
                    Console.Write($"{period} Completed!                      ");
                }
                catch (Exception e)
                {
                    Console.SetCursorPosition(0, priority);
                    Console.Write($"{period} Error!                          ");

                    Exception exception = new Exception($"Exception in {period}", e);
                    ExceptionLoggingService.Instance.WriteLog(exception);
                }
            }

            return storeHouseData;
        }

        static void Unhandled(object sender, UnhandledExceptionEventArgs exArgs)
        {
            ExceptionLoggingService.Instance.WriteLog(
                String.Format(
                    "From application-wide exception handler: {0}", 
                    exArgs.ExceptionObject));
        }
    }
}
