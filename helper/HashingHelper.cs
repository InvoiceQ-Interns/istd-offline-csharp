using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Security.Cryptography.Xml;
using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using ISTD_OFFLINE_CSHARP.loader;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class HashingHelper
    {

        private readonly ILogger log;

        public HashingHelper()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<HashingHelper>();
        }


        public string getInvoiceHash(string xmlDocument, AppResources appResources)
        {
            var transformer = getTransformer(appResources);
            var byteArrayOutputStream = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = true
            };

            using (var xmlOutput = XmlWriter.Create(byteArrayOutputStream, xmlWriterSettings))
            using (var stringReader = new StringReader(xmlDocument))
            using (var streamSource = XmlReader.Create(stringReader))
            {
                transformer.Transform(streamSource, xmlOutput);
                xmlOutput.Flush();
            }

            string canonicalizedXml = canonicalizeXml(byteArrayOutputStream.ToArray());
            byte[] hash = hashStringToBytes(canonicalizedXml);
            return Convert.ToBase64String(hash);
        }

        private string canonicalizeXml(byte[] xmlBytes)
        {
            var xmlDoc = new XmlDocument
            {
                PreserveWhitespace = true
            };

            using (var stream = new MemoryStream(xmlBytes))
            {
                xmlDoc.Load(stream);
            }

            var transform = new XmlDsigC14NTransform();

            transform.LoadInput(xmlDoc);

            using (var s = (Stream)transform.GetOutput(typeof(Stream)))
            using (var reader = new StreamReader(s, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }



        private XslCompiledTransform getTransformer(AppResources appResources)
        {
            return appResources.getInvoiceXslTransformer();
        }

        private XmlWriterSettings getXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = true
            };
        }


        private byte[] hashStringToBytes(string input)
        {
            try
            {
                using (var digest = SHA256.Create())
                {
                    return digest.ComputeHash(Encoding.UTF8.GetBytes(input));
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Something went wrong while hashing XML document");
                return null;
            }
        }

    }



}
