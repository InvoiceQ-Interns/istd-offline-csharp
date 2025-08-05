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
            
            string transformedXml = applyJavaTransformationSequence(xmlDocument, appResources);
            
            // Apply C14N 1.1 canonicalization
            string canonicalizedXml = applyC14N11Canonicalization(transformedXml);
            
           
            byte[] hash = hashStringToBytes(canonicalizedXml);
            string base64Hash = Convert.ToBase64String(hash);
            
            return base64Hash;
        }

        private string applyJavaTransformationSequence(string xmlDocument, AppResources appResources)
        {
            
            xmlDocument = transformXml(xmlDocument, appResources.getRemoveElementXslTransformer());
            
            
            xmlDocument = transformXml(xmlDocument, appResources.getInvoiceXslTransformer());
            
            return xmlDocument;
        }

        private string transformXml(string xmlDocument, XslCompiledTransform transformer)
        {
            var outputStream = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = true,
                NewLineHandling = NewLineHandling.Replace,
                NewLineChars = "\n"
            };

            using (var xmlOutput = XmlWriter.Create(outputStream, xmlWriterSettings))
            using (var stringReader = new StringReader(xmlDocument))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                transformer.Transform(xmlReader, xmlOutput);
                xmlOutput.Flush();
            }

            return Encoding.UTF8.GetString(outputStream.ToArray());
        }

        private string applyC14N11Canonicalization(string xmlString)
        {
            var xmlDoc = new XmlDocument
            {
                PreserveWhitespace = true
            };
            
            xmlDoc.LoadXml(xmlString);

           
            var transform = new XmlDsigC14NTransform(false); 
            transform.LoadInput(xmlDoc);

            using (var stream = (Stream)transform.GetOutput(typeof(Stream)))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string result = reader.ReadToEnd();
                
                return result;
            }
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
