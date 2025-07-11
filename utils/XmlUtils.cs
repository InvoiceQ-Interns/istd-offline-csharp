using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.utils
{
    public class XmlUtil
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
                var settings = new XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
                    Indent = false,
                    Encoding = new System.Text.UTF8Encoding(false)
                };

                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, settings);
                document.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
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
                var document = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                document.LoadXml(xmlString);
                return document;
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
                var xpath = document.CreateNavigator();
                var nodes = xpath.Select(xpathExpression);
                var result = document.CreateNode(XmlNodeType.Element, "dummy", null);
                while (nodes.MoveNext())
                {
                    if (nodes.Current is IHasXmlNode hasNode)
                    {
                        var node = hasNode.GetNode();
                        result.AppendChild(document.ImportNode(node, true));
                    }
                }
                return result.ChildNodes;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "something went wrong while transforming xml document, error: {Message}", e.Message);
                return null;
            }
        }
    }
}
