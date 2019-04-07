using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreHouseDataReader.BL
{
    public class SHConnectorException : Exception
    {
        public SHConnectorException(string message, int hresult) 
            : base(message)
        {
            HResult = hresult;
        }

        public SHConnectorException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
