using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
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
        private readonly string resourceBaseNamespace = "ISTD_OFFLINE_CSHARP.loader";

        public AppResources(ILogger<AppResources> log)
        {
            this.log = log;
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
                invoiceXslTransformer = loadXslt("invoice.xsl");
                removeElementXslTransformer = loadXslt("xslt.removeElements.xsl");
                touchTransformer(removeElementXslTransformer);
                addUBLElementTransformer = loadXslt("xslt.addUBLElement.xsl");
                touchTransformer(addUBLElementTransformer);
                addQRElementTransformer = loadXslt("xslt.addQRElement.xsl");
                touchTransformer(addQRElementTransformer);
                addSignatureElementTransformer = loadXslt("xslt.addSignatureElement.xsl");
                touchTransformer(addSignatureElementTransformer);
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to set up XSLT transformers");
                throw;
            }
        }

        private void touchTransformer(XslCompiledTransform transformer)
        {
            //leave empty
            //cant be implemented like the java version (XslCompiledTransform does not support output properties directly
        }

        private XslCompiledTransform loadXslt(string resourceName)
        {
            string fullResourceName = $"{resourceBaseNamespace}.{resourceName.Replace('/', '.').Replace('\\', '.')}";
            using Stream xsltStream = assembly.GetManifestResourceStream(fullResourceName);
            if (xsltStream == null)
                throw new FileNotFoundException($"Embedded resource '{fullResourceName}' not found.");

            var xslt = new XslCompiledTransform();
            using XmlReader reader = XmlReader.Create(xsltStream);
            xslt.Load(reader);
            return xslt;
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
