using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.loader;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class SigningHelper
    {
        private readonly ILogger logger;
        private readonly HashingHelper hashingHelper;
        private readonly DigitalSignatureHelper digitalSignatureHelper;
        private readonly QRGeneratorHelper qrGeneratorHelper;
        private readonly AppResources appResources;
        private readonly string dateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss";

        
        public EInvoiceSigningResults SignEInvoice(string xmlDocument, ECParameters privateKey, string certificateAsString)
        {
            try
            {
                string invoiceHash = hashingHelper.getInvoiceHash(xmlDocument, appResources);

                // Remove PEM headers/footers and newlines
                certificateAsString = certificateAsString
                    .Replace("-----BEGIN CERTIFICATE-----", "")
                    .Replace("-----END CERTIFICATE-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

                byte[] certificateBytes = Convert.FromBase64String(certificateAsString);
                var cert = new X509Certificate2(certificateBytes);

                DigitalSignature digitalSignature = digitalSignatureHelper.getDigitalSignature(privateKey, invoiceHash);

                xmlDocument = transformXML(xmlDocument);

                XmlDocument document = getXmlDocument(xmlDocument);

                var nameSpacesMap = getNameSpacesMap();

                string certificateHashing = EncodeBase64(
                    Encoding.UTF8.GetBytes(
                        BytesToHex(HashStringToBytes(Encoding.UTF8.GetBytes(certificateAsString)))));

                string signedPropertiesHashing = PopulateSignedSignatureProperties(document, nameSpacesMap,
                    certificateHashing, GetCurrentTimestamp(), cert.Issuer, cert.SerialNumber);

                populateUBLExtensions(document, nameSpacesMap, digitalSignature.getDigitalSignature(),
                    signedPropertiesHashing, EncodeBase64(digitalSignature.getXmlHashing()), certificateAsString);

                string qrCode = PopulateQrCode(document, nameSpacesMap, cert, digitalSignature.getDigitalSignature(), invoiceHash);

                string uuid = readUUID(xmlDocument);

                return new EInvoiceSigningResults(invoiceHash, digitalSignature.getDigitalSignature(), qrCode, document.OuterXml, uuid);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Something went wrong while signing the invoice");
            }
            return null;
        }
        
        
        
        public SigningHelper(ILogger<SigningHelper> logger,
                             HashingHelper hashingHelper,
                             DigitalSignatureHelper digitalSignatureHelper,
                             QRGeneratorHelper qrGeneratorHelper,
                             AppResources appResources)
        {
            this.logger = logger;
            this.hashingHelper = hashingHelper;
            this.digitalSignatureHelper = digitalSignatureHelper;
            this.qrGeneratorHelper = qrGeneratorHelper;
            this.appResources = appResources;
        }
        private Dictionary<string, string> getNameSpacesMap()
        {
            return new Dictionary<string, string>
            {
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
        private string transformXml(string xmlDocument, System.Xml.Xsl.XslCompiledTransform transformer)
        {
            using var stringReader = new StringReader(xmlDocument);
            using var xmlReader = System.Xml.XmlReader.Create(stringReader);
            using var stringWriter = new StringWriter();
            using var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, transformer.OutputSettings);

            transformer.Transform(xmlReader, xmlWriter);

            return stringWriter.ToString();
        }
        private string transformXML(string xmlDocument)
        {
            xmlDocument = transformXml(xmlDocument, appResources.getRemoveElementXslTransformer());
            xmlDocument = transformXml(xmlDocument, appResources.getAddUBLElementTransformer());
            xmlDocument = xmlDocument.Replace("UBL-TO-BE-REPLACED", appResources.getUblXml());
            xmlDocument = transformXml(xmlDocument, appResources.getAddQRElementTransformer());
            xmlDocument = xmlDocument.Replace("QR-TO-BE-REPLACED", appResources.getQrXml());
            xmlDocument = transformXml(xmlDocument, appResources.getAddSignatureElementTransformer());
            xmlDocument = xmlDocument.Replace("SIGN-TO-BE-REPLACED", appResources.getSignatureXml());

            return xmlDocument;
        }

        private XmlDocument getXmlDocument(string xmlString)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,  // Equivalent to disallow-doctype-decl
                XmlResolver = null                       // Disables general and parameter external entities
            };

            using var stringReader = new StringReader(xmlString);
            using var xmlReader = XmlReader.Create(stringReader, settings);

            var xmlDoc = new XmlDocument
            {
                PreserveWhitespace = false
            };
            xmlDoc.Load(xmlReader);

            return xmlDoc;
        }

        private string getNodeXmlValue(XmlDocument document, Dictionary<string, string> namespaces)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            foreach (var ns in namespaces)
            {
                namespaceManager.AddNamespace(ns.Key, ns.Value);
            }

            string xpath = "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties";
            XmlNode node = document.SelectSingleNode(xpath, namespaceManager);

            return node != null ? node.OuterXml : null;
        }

        private string BytesToHex(byte[] hash)
        {
            StringBuilder hexString = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                hexString.Append(b.ToString("x2"));
            }
            return hexString.ToString();
        }

        private string EncodeBase64(byte[] dataToBeEncoded)
        {
            return Convert.ToBase64String(dataToBeEncoded);
        }

        private byte[] HashStringToBytes(byte[] toBeHashed)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(toBeHashed);
        }



        private string PopulateSignedSignatureProperties(
            XmlDocument document,
            Dictionary<string, string> nameSpacesMap,
            string publicKeyHashing,
            string signatureTimestamp,
            string x509IssuerName,
            string serialNumber)
        {
            populateXmlAttributeValue(document, nameSpacesMap, "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:CertDigest/ds:DigestValue", publicKeyHashing);

            populateXmlAttributeValue(document, nameSpacesMap, "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningTime", signatureTimestamp);

            populateXmlAttributeValue(document, nameSpacesMap, "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:IssuerSerial/ds:X509IssuerName", x509IssuerName);

            populateXmlAttributeValue(document, nameSpacesMap, "/Invoice/ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent/sig:UBLDocumentSignatures/sac:SignatureInformation/ds:Signature/ds:Object/xades:QualifyingProperties/xades:SignedProperties/xades:SignedSignatureProperties/xades:SigningCertificate/xades:Cert/xades:IssuerSerial/ds:X509SerialNumber", serialNumber);

            string signedSignatureElement = getNodeXmlValue(document, nameSpacesMap);
            if (signedSignatureElement == null) return null;

            byte[] hashed = HashStringToBytes(Encoding.UTF8.GetBytes(signedSignatureElement));
            string hex = BytesToHex(hashed);
            return EncodeBase64(Encoding.UTF8.GetBytes(hex));
        }

        private static void populateXmlAttributeValue(XmlDocument document, Dictionary<string, string> nameSpaces, string xpathExpression, string newValue)
        {
            XmlNamespaceManager nsManager = new XmlNamespaceManager(document.NameTable);
            foreach (var ns in nameSpaces)
            {
                nsManager.AddNamespace(ns.Key, ns.Value);
            }

            XmlNodeList nodes = document.SelectNodes(xpathExpression, nsManager);
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    node.InnerText = newValue;
                }
            }
        }
        
        private void populateUBLExtensions(
            XmlDocument document,
            Dictionary<string, string> nameSpacesMap,
            string digitalSignature,
            string signedPropertiesHashing,
            string xmlHashing,
            string certificate)
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

        private string GetCurrentTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss");
        }
        
        private string PopulateQrCode(XmlDocument document, Dictionary<string, string> namespacesMap, X509Certificate2 certificate, string signature, string hashedXml)
{
    string sellerName = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyLegalEntity/cbc:RegistrationName");
    string vatRegistrationNumber = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyTaxScheme/cbc:CompanyID");
    string invoiceTotal = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount");
    string vatTotal = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cac:TaxTotal/cbc:TaxAmount");
    string issueDate = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cbc:IssueDate");
    string issueTime = getNodeXmlTextValue(document, namespacesMap, "/Invoice/cbc:IssueTime");

    string timestamp;

    if (issueTime.EndsWith("Z"))
    {
        issueTime = issueTime.Replace("Z", "");
        var utc = DateTime.ParseExact(issueDate + "T" + issueTime, "yyyy-MM-dd'T'HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal);
        var ksaTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time"));
        timestamp = ksaTime.ToString("yyyy-MM-dd'T'HH:mm:ss");
    }
    else
    {
        var local = DateTime.Parse(issueDate + "T" + issueTime);
        timestamp = local.ToString("yyyy-MM-dd'T'HH:mm:ss");
    }

    string qrCode = qrGeneratorHelper.generateQrCode(
        sellerName,
        vatRegistrationNumber,
        timestamp,
        invoiceTotal,
        vatTotal,
        hashedXml,
        certificate.GetPublicKey(),
        signature,
        certificate.GetRawCertData()
    );

    populateXmlAttributeValue(document, namespacesMap,
        "/Invoice/cac:AdditionalDocumentReference[cbc:ID='QR']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject",
        qrCode);

    return qrCode;
}

        private string getNodeXmlTextValue(XmlDocument document, Dictionary<string, string> nameSpaces, string attributeXpath)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);

            foreach (var kvp in nameSpaces)
            {
                namespaceManager.AddNamespace(kvp.Key, kvp.Value);
            }

            XmlNode node = document.SelectSingleNode(attributeXpath, namespaceManager);
            return node?.InnerText;
        }


        public string readUUID(string xmlDocument)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlDocument);

            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

            var uuidNode = xmlDoc.SelectSingleNode("/Invoice/cbc:UUID", namespaceManager);
            return uuidNode?.InnerText;
        }
    }
}
