using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.processor;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;

using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Microsoft;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X500;

using Org.BouncyCastle.Asn1.X500.Style;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Utilities.IO.Pem;

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.X509;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class CsrKeysProcessor : processor.ActionProcessor
{
    private readonly ILogger log;

    public CsrKeysProcessor()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<CsrKeysProcessor>();
    }
    
    private string outputDirectory = "";
    private string configFilePath = "";
    private static AsymmetricKeyParameter  publicKey;
    private AsymmetricKeyParameter  privateKey;
    private CsrConfigDto csrConfigDto;
    private string csrEncoded;
    private string privateKeyPEM;
    private string publicKeyPEM;
    private string csrPem;

     
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 2)
        {
            log.LogInformation("Usage: dotnet run generate-csr-keys <directory> <config-file>");
            return false;
        }

        outputDirectory = args[0];
        configFilePath = args[1];
        return true;
    }
    

    protected override bool validateArgs()
{
    if (!ReaderHelper.isDirectoryExists(outputDirectory))
    {
        log.LogInformation($"Output directory [{outputDirectory}] does not exist");
        return false;
    }

    if (string.IsNullOrWhiteSpace(configFilePath))
    {
        log.LogInformation($"Config file [{configFilePath}] does not exist");
        return false;
    }

    string configFile = ReaderHelper.readFileAsString(configFilePath);
    if (string.IsNullOrWhiteSpace(configFile))
    {
        log.LogInformation($"Config file [{configFilePath}] is empty");
        return false;
    }

    csrConfigDto = JsonUtils.readJson<CsrConfigDto>(configFile);
    if (csrConfigDto == null)
    {
        log.LogInformation($"Config file [{configFilePath}] is invalid");
        return false;
    }

    bool isValid = true;

    if (string.IsNullOrWhiteSpace(csrConfigDto.getCommonName()))
    {
        log.LogInformation($"Common name is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getSerialNumber()))
    {
        log.LogInformation($"Serial number is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getOrganizationIdentifier()))
    {
        log.LogInformation($"Organization identifier is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getOrganizationUnitName()))
    {
        log.LogInformation($"Organization unit name is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getOrganizationName()))
    {
        log.LogInformation($"Organization name is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getCountryName()))
    {
        log.LogInformation($"Country name is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getInvoiceType()))
    {
        log.LogInformation($"Invoice type is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getLocation()))
    {
        log.LogInformation($"Location is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getIndustry()))
    {
        log.LogInformation($"Industry is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (string.IsNullOrWhiteSpace(csrConfigDto.getEmailAddress()))
    {
        log.LogInformation($"Email is missing in config file [{configFilePath}]");
        isValid = false;
    }

    if (isValid && csrConfigDto.getSerialNumber().Split('|').Length != 3)
    {
        log.LogInformation($"Serial number [{csrConfigDto.getSerialNumber()}] is invalid, format [TAX_NUMBER|SEQ_NUMBER|DEVICE_ID]");
        isValid = false;
    }

    if (isValid && (csrConfigDto.getInvoiceType().Length != 4 || !System.Text.RegularExpressions.Regex.IsMatch(csrConfigDto.getInvoiceType(), "^[01]+$")))
    {
        log.LogInformation($"Invoice type [{csrConfigDto.getInvoiceType()}] is invalid, format [4-digit-number (0/1)]");
        isValid = false;
    }

    return isValid;
}
    
    protected override bool process()
    {
        if (!generateKeyPairs() || publicKey == null || privateKey == null)
        {
            log.LogError("Failed to generate CSR keys");
            return false;
        }

        if (!buildCsr() || string.IsNullOrWhiteSpace(csrEncoded))
        {
            log.LogError("Failed to build CSR");
            return false;
        }

        using (var sw = new StringWriter())
        {
            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(sw);
            pemWriter.WriteObject(privateKey);
            pemWriter.Writer.Flush();
            privateKeyPEM = sw.ToString();
        }
        
        using (var sw = new StringWriter())
        {
            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(sw);
            pemWriter.WriteObject(publicKey);
            pemWriter.Writer.Flush();
            publicKeyPEM = sw.ToString();
        }
        return true;
    }
   
    protected override bool output()
    {
        log?.LogInformation($"CSR [{SecurityUtils.decrypt(csrEncoded)}]");

        string privateKeyFile = Path.Combine(outputDirectory, "private.pem");
        string publicKeyFile = Path.Combine(outputDirectory, "public.pem");
        string csrFile = Path.Combine(outputDirectory, "csr.pem");
        string csrEncodedFile = Path.Combine(outputDirectory, "csr.encoded");

        bool valid = WriterHelper.writeFile(privateKeyFile, SecurityUtils.encrypt(privateKeyPEM));
        valid = WriterHelper.writeFile(publicKeyFile, SecurityUtils.encrypt(publicKeyPEM)) && valid;
        valid = WriterHelper.writeFile(csrFile, SecurityUtils.encrypt(csrPem)) && valid;
        valid = WriterHelper.writeFile(csrEncodedFile, SecurityUtils.encrypt(csrEncoded)) && valid;

        return valid;
    }

    
    
    private bool buildCsr()
    {
        try
        {
            string certificateTemplateName = propertiesManager.getProperty("fotara.certificate.template");

            X509Name subject = buildX500SubjectBlock();
            X509Name x500OtherAttributes = buildX500AttributesBlock();

            var generalNames = new GeneralNames(new GeneralName(GeneralName.DirectoryName, x500OtherAttributes));

            // Build extensions properly
            X509Extension certTemplateX509Ext = new X509Extension(
                false,
                new DerOctetString(new DerPrintableString(certificateTemplateName))

            );

            X509Extension subjectAltNameX509Ext = new X509Extension(
                false,
                new DerOctetString(generalNames)
            );

            var extensionsDict = new Dictionary<DerObjectIdentifier, X509Extension>
            {
                { MicrosoftObjectIdentifiers.MicrosoftCertTemplateV1, certTemplateX509Ext },
                { X509Extensions.SubjectAlternativeName, subjectAltNameX509Ext }
            };

            X509Extensions x509Extensions = new X509Extensions(extensionsDict);

            AttributePkcs extensionRequestAttribute = new AttributePkcs(
                PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                new DerSet(x509Extensions)
            );

            var pkcs10Builder = new Pkcs10CertificationRequest(
                "SHA256WITHECDSA",
                subject,
                publicKey,
                new DerSet(extensionRequestAttribute),
                privateKey
            );

            csrPem = transform("CERTIFICATE REQUEST", pkcs10Builder.GetDerEncoded());
            if (csrPem == null) return false;

            csrEncoded = Convert.ToBase64String(pkcs10Builder.GetDerEncoded());

        }
        catch (Exception e)
        {
            log?.LogError(e, "Failed to build CSR");
            return false;
        }

        return true;
    }
    
    private string transform(string type, byte[] certificateRequest)
    {
        try
        {
            var pemObject = new Org.BouncyCastle.Utilities.IO.Pem.PemObject(type, certificateRequest);
            using var stringWriter = new StringWriter();
            using var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
            pemWriter.WriteObject(pemObject);
            pemWriter.Writer.Flush();
            return stringWriter.ToString();
        }
        catch (Exception e)
        {
            log.LogError(e, "Something went wrong while transforming to PEM");
            return null;
        }
    }
    
    private bool generateKeyPairs()
    {
        try
        {
            var keyPair = ECDSAUtils.getKeyPair();
            publicKey = keyPair.Public;
            privateKey = keyPair.Private;
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to generate CSR keys");
            return false;
        }

        return true;
    }

    private X509Name buildX500SubjectBlock()
    {
        var subjectAttributes = new List<DerObjectIdentifier>
        {
            X509Name.EmailAddress // Email Address
        };

        var subjectValues = new List<string>
        {
            csrConfigDto.getEmailAddress()
        };

        return new X509Name(subjectAttributes, subjectValues);
    }


    
    private X509Name buildX500AttributesBlock()
    {
        var oids = new List<DerObjectIdentifier>
                 {
            //bouncyCastle in C# doesn't store these by default so we added them manually 
            // Serial Number
            new DerObjectIdentifier("2.5.4.4"),
            // UID (organization identifier)
            new DerObjectIdentifier("0.9.2342.19200300.100.1.1"),
            // Title (invoice type)
            new DerObjectIdentifier("2.5.4.12"),
            // Registered Address
            new DerObjectIdentifier("2.5.4.26"),
            // Business Category
            new DerObjectIdentifier("2.5.4.15")
        };

        var values = new List<string>
        {
            csrConfigDto.getSerialNumber(),
            csrConfigDto.getOrganizationIdentifier(),
            csrConfigDto.getInvoiceType(),
            csrConfigDto.getLocation(),
            csrConfigDto.getIndustry()
        };

        return new X509Name(oids, values);
    }
    
}