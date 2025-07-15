using System;
using System.Text;
using System.Xml;
using ISTD_OFFLINE_CSHARP.utils;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class RequesterGeneratorHelper
    {
        private readonly ILogger log;

        public RequesterGeneratorHelper(ILogger log)
        {
            log.LogInformation("RequesterGeneratorHelper");
        }

        public string generateEInvoiceRequest(string invoiceHash, string uuid, string signedXml)
        {
            return $"{{\"invoiceHash\":\"{invoiceHash}\",\"uuid\":\"{uuid}\",\"invoice\":\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(signedXml))}\"}}";
        }

        public string generateEInvoiceRequest(string signedXml)
        {
            try
            {
                XmlDocument document = XmlUtil.transform(signedXml);
                string uuidValue = "";
                XmlNodeList uuidNodeList = XmlUtil.evaluateXpath(document, "/Invoice/UUID");
                if (uuidNodeList != null && uuidNodeList.Count > 0)
                {
                    uuidValue = uuidNodeList[0].FirstChild?.Value ?? "";
                }

                string invoiceHashValue = "";
                XmlNodeList invoiceHashNodeList = XmlUtil.evaluateXpath(document, "/Invoice/UBLExtensions/UBLExtension/ExtensionContent/UBLDocumentSignatures/SignatureInformation/Signature/SignedInfo/Reference/DigestValue");
                if (invoiceHashNodeList != null && invoiceHashNodeList.Count > 0)
                {
                    invoiceHashValue = invoiceHashNodeList[0].FirstChild?.Value ?? "";
                }

                return generateEInvoiceRequest(invoiceHashValue, uuidValue, signedXml);
            }
            catch (Exception e)
            {
                log?.LogError(e, "failed to get invoice data");
                return null;
            }
        }
    }
}
