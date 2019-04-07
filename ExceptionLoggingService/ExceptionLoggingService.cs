using System;
using System.IO;
using System.Text;

namespace ExceptionLogging
{
    public class ExceptionLoggingService
    {
        private readonly FileStream _fileStream;
        private readonly StreamWriter _streamWriter;
        private static ExceptionLoggingService _instance;

        // public property
        public static ExceptionLoggingService Instance
        {
            get
            {
                return _instance ?? (_instance = new ExceptionLoggingService());
            }
        }
        //private constructor
        private ExceptionLoggingService()
        {
            //_fileStream = File.OpenWrite(GetExecutionFolder() + "\\Log.log");
            _streamWriter = new StreamWriter(GetExecutionFolder() + "\\Log.log", true, Encoding.UTF8);
        }
        // <!-- Singleton code

        public void WriteLog(string message)
        {
            //if (!HHSConst.Logging) return;
            StringBuilder formattedMessage = new StringBuilder();
            formattedMessage.AppendLine("Date: " + DateTime.Now.ToString());
            formattedMessage.AppendLine("Message: " + message);
            _streamWriter.WriteLine(formattedMessage.ToString());
            _streamWriter.Flush();
        }

        public void WriteLog(Exception exception)
        {
            //while(exception != null)
            {
                String msgInnerExAndStackTrace =
                    String.Format(
                    "{0}; Inner Ex: {1}; Stack Trace: {2}",
                    exception.Message, exception.InnerException, exception.StackTrace);

                WriteLog(msgInnerExAndStackTrace);

                //exception = exception.InnerException;
            }


            
        }

        private string GetExecutionFolder()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

    }
}
