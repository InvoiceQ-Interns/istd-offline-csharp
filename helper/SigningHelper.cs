using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.loader;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;

namespace ISTD_OFFLINE_CSHARP.Helper
{
    public class SigningHelper
    {
        private readonly ILogger<SigningHelper> log;
        private readonly string dateTimeFormatPattern = "yyyy-MM-ddTHH:mm:ss";
        private readonly HashingHelper hashingHelper;
        private readonly DigitalSignatureHelper digitalSignatureHelper;
        private readonly QRGeneratorHelper qrGeneratorHelper;
        private readonly AppResources appResources;

        public SigningHelper()
        {
            log = LoggingUtils.getLoggerFactory().CreateLogger<SigningHelper>();
            hashingHelper = new HashingHelper();
            digitalSignatureHelper = new DigitalSignatureHelper();
            qrGeneratorHelper = new QRGeneratorHelper();
            appResources = new AppResources();
        }

        public EInvoiceSigningResults? signEInvoice(string xmlDocument, RSA privateKey,
            string certificateAsString)
        {
            try
            {
                string invoiceHash = hashingHelper.getInvoiceHash(xmlDocument, appResources);


                certificateAsString = certificateAsString
                    .Replace("-----BEGIN CERTIFICATE-----", "")
                    .Replace("-----END CERTIFICATE-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

                byte[] certificateBytes = Convert.FromBase64String(certificateAsString);
                string certificateCopy = certificateAsString;


                X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(certificateBytes);


                DigitalSignature digitalSignature = digitalSignatureHelper.getDigitalSignature(privateKey, invoiceHash);
                Console.WriteLine(digitalSignature.getDigitalSignature());


                xmlDocument = transformXml(xmlDocument);


                XmlDocument document = getXmlDocument(xmlDocument);
                var nameSpacesMap = getNameSpacesMap();


                string certificateHashing = encodeBase64(
                    Encoding.UTF8.GetBytes(bytesToHex(hashStringToBytes(Encoding.UTF8.GetBytes(certificateAsString)))));

               
                string signedPropertiesHashing = populateSignedSignatureProperties(
                    document, nameSpacesMap, certificateHashing, getCurrentTimestamp(),
                    certificate.Issuer, certificate.SerialNumber);

                
                populateUblExtensions(document, nameSpacesMap, digitalSignature.getDigitalSignature(),
                    signedPropertiesHashing, encodeBase64(digitalSignature.getXmlHashing()),
                    certificateCopy);

                
                string qrCode = populateQrCode(document, nameSpacesMap, certificate,
                    digitalSignature.getDigitalSignature(), invoiceHash);

                
                string uuid = readUuid(xmlDocument);

                return new EInvoiceSigningResults(invoiceHash, digitalSignature.getDigitalSignature(),
                    qrCode, document.OuterXml, uuid);
            }
            catch (Exception e)
            {
                log.LogError(e, "Something went wrong while signing the invoice");
                return null;
            }
        }

        private static Dictionary<string, string> getNameSpacesMap()
        {
            return new Dictionary<string, string>
            {
                { "", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" }, // Default namespace
                { "cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" },
                { "cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2" },
                { "ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2" },
                { "sig", "urn:oasis:names:specification:ubl:schema:xsd:CommonSignatureComponents-2" },
                { "sac", "urn:oasis:names:specification:ubl:schema:xsd:SignatureAggregateComponents-2" },
                { "sbc", "urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2" },
                { "ds", "http://www.w3.org/2000/09/xmldsig#" },
                { "xades", "http://uri.etsi.org/01903/v1.3.2#" }
            };
        }

        private string transformXml(string xmlDocument)
        {
            try
            {
                log.LogDebug("Starting XML transformation pipeline");

                
                xmlDocument = transformXmlStep(xmlDocument, appResources.getRemoveElementXslTransformer(),
                    "removeElements");

                
                xmlDocument = transformXmlStep(xmlDocument, appResources.getAddUBLElementTransformer(),
                    "addUBLElement");
                xmlDocument = xmlDocument.Replace("UBL-TO-BE-REPLACED", appResources.getUblXml());

                
                xmlDocument = transformXmlStep(xmlDocument, appResources.getAddQRElementTransformer(), "addQRElement");
                xmlDocument = xmlDocument.Replace("QR-TO-BE-REPLACED", appResources.getQrXml());

                
                xmlDocument = transformXmlStep(xmlDocument, appResources.getAddSignatureElementTransformer(),
                    "addSignatureElement");
                xmlDocument = xmlDocument.Replace("SIGN-TO-BE-REPLACED", appResources.getSignatureXml());

                log.LogDebug("XML transformation pipeline completed successfully");
                return xmlDocument;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed during XML transformation pipeline");
                throw new InvalidOperationException("XML transformation pipeline failed", ex);
            }
        }

        private string transformXmlStep(string xmlDocument, XslCompiledTransform transformer,
            string stepName = "unknown")
        {
            try
            {
                
                xmlDocument = cleanXmlDocument(xmlDocument);

                using var inputStream = new StringReader(xmlDocument);
                using var outputStream = new StringWriter();

                
                var readerSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    NameTable = new NameTable(),
                    CheckCharacters = false 
                };

                using var xmlReader = XmlReader.Create(inputStream, readerSettings);

                
                var xsltArgs = new XsltArgumentList();

                transformer.Transform(xmlReader, xsltArgs, outputStream);

                return outputStream.ToString();
            }
            catch (XmlException ex)
            {
                log.LogError(ex,
                    "XML transformation failed in step '{StepName}' at line {LineNumber}, position {LinePosition}: {Message}",
                    stepName, ex.LineNumber, ex.LinePosition, ex.Message);
                throw new InvalidOperationException(
                    $"XML transformation failed in step '{stepName}' at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Transformation failed in step '{StepName}': {Message}", stepName, ex.Message);
                throw new InvalidOperationException($"Transformation failed in step '{stepName}': {ex.Message}", ex);
            }
        }

        private static string cleanXmlDocument(string xmlDocument)
        {
            if (string.IsNullOrWhiteSpace(xmlDocument))
                return xmlDocument;

            
            xmlDocument = xmlDocument.Trim('\uFEFF', '\u200B', '\0');

            
            xmlDocument = xmlDocument.TrimStart();

            
            if (!xmlDocument.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                // Add XML declaration if missing
                xmlDocument = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xmlDocument;
            }

            
            xmlDocument = fixNamespaceDeclarations(xmlDocument);

            return xmlDocument;
        }

        private static string fixNamespaceDeclarations(string xmlDocument)
        {
            
            xmlDocument = System.Text.RegularExpressions.Regex.Replace(
                xmlDocument,
                @"\s+xmlns\s*=\s*[""'][\s]*[""']",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

          
            xmlDocument = System.Text.RegularExpressions.Regex.Replace(
                xmlDocument,
                @"\s+xmlns:([^=\s]+)\s*=\s*[""'][\s]*[""']",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return xmlDocument;
        }

        private static XmlDocument getXmlDocument(string xmlDocument)
        {
            try
            {
                var doc = new XmlDocument();
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    CheckCharacters = false,
                    IgnoreWhitespace = false
                };

                using var stringReader = new StringReader(xmlDocument);
                using var xmlReader = XmlReader.Create(stringReader, settings);
                doc.Load(xmlReader);
                return doc;
            }
            catch (XmlException ex)
            {
                throw new XmlException($"Failed to parse XML document: {ex.Message}", ex);
            }
        }

        private static string? getNodeXmlValue(XmlDocument document, Dictionary<string, string> nameSpaces)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);

            
            foreach (var ns in nameSpaces)
            {
                if (string.IsNullOrEmpty(ns.Key))
                {
                    namespaceManager.AddNamespace("def", ns.Value); // Use 'def' for default namespace
                }
                else
                {
                    namespaceManager.AddNamespace(ns.Key, ns.Value);
                }
            }

            const string xpath =
                "/def:Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties";
            XmlNode? node = document.SelectSingleNode(xpath, namespaceManager);
            return node?.OuterXml;
        }

        private static string bytesToHex(byte[] hash)
        {
            StringBuilder hexString = new StringBuilder(2 * hash.Length);
            foreach (byte b in hash)
            {
                string hex = (b & 0xFF).ToString("x2");
                hexString.Append(hex);
            }

            return hexString.ToString();
        }

        private static string encodeBase64(byte[] stringToBeEncoded)
        {
            return Convert.ToBase64String(stringToBeEncoded);
        }

        private static byte[] hashStringToBytes(byte[] toBeHashed)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(toBeHashed);
        }

        private string populateSignedSignatureProperties(XmlDocument document,
            Dictionary<string, string> nameSpacesMap,
            string publicKeyHashing, string signatureTimestamp, string x509IssuerName, string serialNumber)
        {
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:CertDigest/ds:DigestValue",
                publicKeyHashing);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningTime",
                signatureTimestamp);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:IssuerSerial/ds:X509IssuerName",
                x509IssuerName);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:IssuerSerial/ds:X509SerialNumber",
                serialNumber);

            string? signedSignatureElement = getNodeXmlValue(document, nameSpacesMap);

            if (!string.IsNullOrEmpty(signedSignatureElement))
            {
                return encodeBase64(
                    Encoding.UTF8.GetBytes(
                        bytesToHex(hashStringToBytes(Encoding.UTF8.GetBytes(signedSignatureElement)))));
            }

            return string.Empty;
        }

        private static void populateXmlAttributeValue(XmlDocument document,
           Dictionary<string, string> nameSpaces,
            string attributeXpath, string newValue)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);

           
            foreach (var ns in nameSpaces)
            {
                if (string.IsNullOrEmpty(ns.Key))
                {
                    namespaceManager.AddNamespace("def", ns.Value); // Use 'def' for default namespace
                }
                else
                {
                    namespaceManager.AddNamespace(ns.Key, ns.Value);
                }
            }

            // Replace /Invoice with /def:Invoice in xpath if it starts with /Invoice
            if (attributeXpath.StartsWith("/Invoice/"))
            {
                attributeXpath = attributeXpath.Replace("/Invoice/", "/def:Invoice/");
            }

            XmlNodeList? nodes = document.SelectNodes(attributeXpath, namespaceManager);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node is XmlElement element)
                    {
                        element.InnerText = newValue;
                    }
                }
            }
        }

        private void populateUblExtensions(XmlDocument document,
            Dictionary<string, string> nameSpacesMap,
            string digitalSignature, string signedPropertiesHashing, string xmlHashing, string certificate)
        {
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:SignatureValue",
                digitalSignature);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:KeyInfo/ds:X509Data/ds:X509Certificate",
                certificate);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:SignedInfo/ds:Reference[@URI='#xadesSignedProperties']/ds:DigestValue",
                signedPropertiesHashing);
            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:SignedInfo/ds:Reference[@Id='invoiceSignedData']/ds:DigestValue",
                xmlHashing);
        }

        private string getCurrentTimestamp()
        {
            return DateTime.Now.ToString(dateTimeFormatPattern, CultureInfo.InvariantCulture);
        }

        private string populateQrCode(XmlDocument document,
            Dictionary<string, string> nameSpacesMap,
            X509Certificate2 certificate, string signature, string hashedXml)
        {
            string? sellerName = getNodeXmlTextValue(document, nameSpacesMap,
                "/Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyLegalEntity/cbc:RegistrationName");
            string? vatRegistrationNumber = getNodeXmlTextValue(document, nameSpacesMap,
                "/Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID");
            string? invoiceTotal = getNodeXmlTextValue(document, nameSpacesMap,
                "/Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount");
            string? vatTotal = getNodeXmlTextValue(document, nameSpacesMap,
                "/Invoice/cac:TaxTotal/cbc:TaxAmount");
            string? issueDate = getNodeXmlTextValue(document, nameSpacesMap, "/Invoice/cbc:IssueDate");
            string? issueTime = getNodeXmlTextValue(document, nameSpacesMap, "/Invoice/cbc:IssueTime");

            if (string.IsNullOrEmpty(issueTime))
            {
                issueTime = "00:00:00";
                log.LogWarning("IssueTime element missing from invoice, using default time: {IssueTime}", issueTime);
            }

            string timeStamp = processDateTime(issueDate ?? "", issueTime);

            string qrCode = qrGeneratorHelper.generateQrCode(
                sellerName ?? "",
                vatRegistrationNumber ?? "",
                timeStamp,
                invoiceTotal ?? "",
                vatTotal ?? "",
                hashedXml,
                certificate.GetPublicKey(),
                signature,
                certificate.GetRawCertData());

            populateXmlAttributeValue(document, nameSpacesMap,
                "/Invoice/cac:AdditionalDocumentReference[cbc:ID='QR']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject",
                qrCode);

            return qrCode;
        }

        private string processDateTime(string issueDate, string issueTime)
        {
            if (issueTime.EndsWith('Z'))
            {
                issueTime = issueTime.Replace("Z", "");
                string dateTimeString = $"{issueDate}T{issueTime}";

                if (DateTime.TryParseExact(dateTimeString, "yyyy-MM-ddTHH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime utcDateTime))
                {
                    // Convert from UTC to GMT+3
                    DateTime ksaTime = utcDateTime.AddHours(3);
                    return ksaTime.ToString(dateTimeFormatPattern, CultureInfo.InvariantCulture);
                }
            }

            string stringDateTime = $"{issueDate}T{issueTime}";
            if (DateTime.TryParseExact(stringDateTime, "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime.ToString(dateTimeFormatPattern, CultureInfo.InvariantCulture);
            }

            return stringDateTime;
        }

        private string? getNodeXmlTextValue(XmlDocument document,
            Dictionary<string, string> nameSpaces, string attributeXpath)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);

            
            foreach (var ns in nameSpaces)
            {
                if (string.IsNullOrEmpty(ns.Key))
                {
                    namespaceManager.AddNamespace("def", ns.Value); // Use 'def' for default namespace
                }
                else
                {
                    namespaceManager.AddNamespace(ns.Key, ns.Value);
                }
            }

            // Replace /Invoice with /def:Invoice in xpath if it starts with /Invoice
            if (attributeXpath.StartsWith("/Invoice/"))
            {
                attributeXpath = attributeXpath.Replace("/Invoice/", "/def:Invoice/");
            }

            XmlNode? node = document.SelectSingleNode(attributeXpath, namespaceManager);
            if (node == null)
            {
                log.LogDebug("XML node not found for path: {AttributeXpath}", attributeXpath);
                return null;
            }

            return node.InnerText;
        }

        public string readUuid(string xmlDocument)
        {
            try
            {
                XmlDocument document = getXmlDocument(xmlDocument);
                var nameSpaces = getNameSpacesMap();

                
                string? uuid = getNodeXmlTextValue(document, nameSpaces, "/Invoice/cbc:UUID");

                if (string.IsNullOrEmpty(uuid))
                {
                    log.LogWarning("UUID not found in invoice document");
                    return string.Empty;
                }

                return uuid;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to read UUID from XML document");
                throw;
            }
        }
    }
}