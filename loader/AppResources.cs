using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.loader
{
    public class AppResources
    {
        private readonly ILogger<AppResources> log;

        private XslCompiledTransform invoiceXslTransformer;
        private XslCompiledTransform removeElementXslTransformer;
        private XslCompiledTransform addUBLElementTransformer;
        private XslCompiledTransform addQRElementTransformer;
        private XslCompiledTransform addSignatureElementTransformer;

        private string ublXml;
        private string qrXml;
        private string signatureXml;

        private readonly Assembly assembly;
        private readonly string resourceBaseNamespace = "ISTD_OFFLINE_CSHARP.resources";

        public AppResources()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<AppResources>();
            assembly = Assembly.GetExecutingAssembly();
            setTransformers();
            setXmlsValues();
        }

        private void setXmlsValues()
        {
            ublXml = readEmbeddedResourceText("xml.ubl.xml");
            qrXml = readEmbeddedResourceText("xml.qr.xml");
            signatureXml = readEmbeddedResourceText("xml.signature.xml");
        }

        private void setTransformers()
        {
            try
            {
                invoiceXslTransformer = loadTransformer("invoice.xsl");
                removeElementXslTransformer = loadTransformer("xslt/removeElements.xsl");
                touchTransformer(removeElementXslTransformer);

                addUBLElementTransformer = loadTransformer("xslt/addUBLElement.xsl");
                touchTransformer(addUBLElementTransformer);

                addQRElementTransformer = loadTransformer("xslt/addQRElement.xsl");
                touchTransformer(addQRElementTransformer);

                addSignatureElementTransformer = loadTransformer("xslt/addSignatureElement.xsl");
                touchTransformer(addSignatureElementTransformer);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to initialize transformers", ex);
            }
        }

        private void touchTransformer(XslCompiledTransform transformer)
        {
            //leave empty
            //cant be implemented like the java version (XslCompiledTransform does not support output properties directly
        }

        private XslCompiledTransform loadTransformer(string resourceName)
        {
            string fullResourceName = $"{resourceBaseNamespace}.{resourceName.Replace('/', '.').Replace('\\', '.')}";
            using Stream xsltStream = assembly.GetManifestResourceStream(fullResourceName);
            if (xsltStream == null)
                throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");

            // Read the content and clean it before parsing
            using StreamReader reader = new StreamReader(xsltStream);
            string content = reader.ReadToEnd().Trim();
    
            // Ensure the XML declaration is at the start with no preceding whitespace
            if (content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                // Content is already starting with XML declaration
            }
            else if (content.Contains("<?xml"))
            {
                // Remove everything before the XML declaration
                int index = content.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
                content = content.Substring(index);
            }
            else
            {
                content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + content;
            }
    
            var xslt = new XslCompiledTransform();
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new XmlUrlResolver()
            };

            try
            {
                using StringReader stringReader = new StringReader(content);
                using XmlReader xmlReader = XmlReader.Create(stringReader, settings);
                xslt.Load(xmlReader, new XsltSettings(true, false), new XmlUrlResolver());
                return xslt;
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to load transformer from resource '{fullResourceName}'");
                throw new InvalidOperationException($"Failed to load XSLT from resource '{resourceName}'", ex);
            }
        }

        private string readEmbeddedResourceText(string resourceName)
        {
            string fullResourceName = $"{resourceBaseNamespace}.{resourceName.Replace('/', '.').Replace('\\', '.')}";
            using Stream stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public XslCompiledTransform getInvoiceXslTransformer() => invoiceXslTransformer;
        public XslCompiledTransform getRemoveElementXslTransformer() => removeElementXslTransformer;
        public XslCompiledTransform getAddUBLElementTransformer() => addUBLElementTransformer;
        public XslCompiledTransform getAddQRElementTransformer() => addQRElementTransformer;
        public XslCompiledTransform getAddSignatureElementTransformer() => addSignatureElementTransformer;

        public string getUblXml() => ublXml;
        public string getQrXml() => qrXml;
        public string getSignatureXml() => signatureXml;
    }
}
