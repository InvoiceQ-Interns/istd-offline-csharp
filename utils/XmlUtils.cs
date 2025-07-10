using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.utils
{
    public class xmlUtil
    {
        private static ILogger logger;
        public static void setLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("XmlUtil");
        }
        
        public static string transform(XmlDocument document)
        {
            try
            {
                using var stringWriter = new StringWriter();
                using var xmlTextWriter = new XmlTextWriter(stringWriter);
                document.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "something went wrong while transforming xml document, error: {Message}", e.Message);
                return null;
            }
        }


        public static XmlDocument transform(string xmlString)
        {
            try
            {
                var factory = new XmlDocument
                {
                    PreserveWhitespace = false
                };
                factory.LoadXml(xmlString);
                return factory;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "something went wrong while transforming xml document, error: {Message}", e.Message);
                return null;
            }
        }

        public static XmlNodeList evaluateXpath(XmlDocument document, string xpathExpression)
        {
            try
            {
                var navigator = document.CreateNavigator();
                var nodes = navigator.Select(xpathExpression);
                return (XmlNodeList)((IHasXmlNode)nodes).GetNode().SelectNodes(xpathExpression);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "something went wrong while transforming xml document, error: {Message}", e.Message);
                return null;
            }
        }
    }
}
