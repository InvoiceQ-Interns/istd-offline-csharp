using System;
using System.Collections.Generic;

namespace ISTD_OFFLINE_CSHARP.properties.impl
{
    public class ProdProperties : PropertiesManager
    {
        private static PropertiesManager instance;
        private readonly Dictionary<string, string> data = new();

        private ProdProperties()
        {
            data["environment"] = "simulation";
            data["fotara.api.url.compliance.csr"] = "https://prod.fotara.com/v1/compliance/csr";
            data["fotara.api.url.compliance.invoice"] = "https://prod.fotara.com/v1/compliance/invoice";
            data["fotara.api.url.prod.certificate"] = "https://prod.fotara.com/v1/prod/certificate";
            data["fotara.api.url.prod.invoice"] = "https://prod.fotara.com/v1/prod/invoice";
            data["fotara.certificate.template"] = "PROD_TEMP";
        }

        public static PropertiesManager getInstance()
        {
            if (instance == null)
            {
                instance = new ProdProperties();
            }
            return instance;
        }

        public string getProperty(string key)
        {
            return data.TryGetValue(key, out var value) ? value : null;
        }
    }
}
