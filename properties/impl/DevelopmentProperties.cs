using System;
using System.Collections.Generic;
using ISTD_OFFLINE_CSHARP.properties;

namespace ISTD_OFFLINE_CSHARP.properties
{
    public class DevelopmentProperties : PropertiesManager
    {
        private static PropertiesManager instance;
        private readonly Dictionary<string, string> data = new();

        private DevelopmentProperties()
        {
            data["environment"] = "development";
            data["fotara.api.url.compliance.csr"] = "http://localhost:5212/v1/compliance/csr";
            data["fotara.api.url.compliance.invoice"] = "http://localhost:5212/v1/compliance/invoice";
            data["fotara.api.url.prod.certificate"] = "http://localhost:5212/v1/prod/certificate";
            data["fotara.api.url.prod.invoice"] = "http://qpt.invoiceq.com/service/core/invoices/clearance";
            data["fotara.api.url.prod.report.invoice"] =  "http://qpt.invoiceq.com/service/core/invoices/reporting" ;
            data["fotara.certificate.template"] = "DEV_TEMP";
            data["fotara.certificate.template"] = "NQCSignature";
        }

        public static PropertiesManager getInstance()
        {
            if (instance == null)
            {
                instance = new DevelopmentProperties();
            }
            return instance;
        }

        public string getProperty(string key)
        {
            return data.TryGetValue(key, out var value) ? value : null;
        }
    }
}
