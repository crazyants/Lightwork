using System.Collections.Generic;
using System.Configuration;

namespace D3.Lightwork.ServiceApi
{
    public static class AppSettings
    {
        public static readonly string[] DefaultApiAddresses = 
        {
            "http://*/LightworkApi/"
        };

        public static IList<string> ApiAddresses
        {
            get
            {
                var addresses = ConfigurationManager.AppSettings["ApiAddress"];
                if (addresses == null)
                {
                    return DefaultApiAddresses;
                }

                return addresses.Split(';');
            }
        }
    }
}
