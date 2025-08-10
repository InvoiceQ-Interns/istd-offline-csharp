using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.Helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class InvoiceSignProcessor : processor.ActionProcessor
{
    private readonly ILogger log;
    private readonly SigningHelper signingHelper;
    private string xmlPath = "";
    private string privateKeyPath = "";
    private string certificatePath = "";
    private string outputFile = "";
    private string keyPassword = "";
    private RSA privateKey;
    private string xmlFile = "";
    private string certificateStr = "";
    private EInvoiceSigningResults signingResults;

    public InvoiceSignProcessor()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<InvoiceSignProcessor>();
        this.signingHelper = new SigningHelper();
    }

    protected override bool loadArgs(string[] args)
    {
        if (args.Length < 4 || args.Length > 5)
        {
            log?.LogInformation("Usage: dotnet run invoice-sign <xml-path> <private-key-path> <certificate-path> <output-file> [key-password]");
            return false;
        }
        xmlPath = args[0];
        privateKeyPath = args[1];
        certificatePath = args[2];
        outputFile = args[3];
        keyPassword = args.Length == 5 ? args[4] : "";
        return true;
    }
    
    protected override bool validateArgs()
    {
        if (string.IsNullOrWhiteSpace(outputFile))
        {
            log?.LogInformation("Invalid output path");
            return false;
        }

        if (!readXmlFile()) return false;
        if (!readPrivateKey()) return false;
        return readCertificate();
    }

    protected override bool process()
    {
        if (signingHelper == null)
        {
            log?.LogError("SigningHelper is null. Initialization missing.");
            return false;
        }
        
        signingResults = signingHelper.signEInvoice(xmlFile, privateKey, certificateStr);
        return signingResults != null && !string.IsNullOrWhiteSpace(signingResults.getSignedXml());
    }
    
    protected override bool output()
    {
        log?.LogInformation($@"
        invoice UUID [{signingResults.getInvoiceUUID()}]
        invoice Hash [{signingResults.getInvoiceHash()}]
        invoice QR Code: [{signingResults.getQrCode()}]
        ");

        return WriterHelper.writeFile(outputFile, signingResults.getSignedXml());
    }
    
    private bool readCertificate()
    {
        certificateStr = ReaderHelper.readFileAsString(certificatePath);
        if (string.IsNullOrWhiteSpace(certificateStr))
        {
            log?.LogInformation($"Certificate file [{certificatePath}] is empty");
            return false;
        }
        certificateStr = SecurityUtils.decrypt(certificateStr);
        return true;
    }
    
    private bool readPrivateKey()
    {
        string privateKeyFile = ReaderHelper.readFileAsString(privateKeyPath);
        if (string.IsNullOrWhiteSpace(privateKeyFile))
        {
            log?.LogInformation($"Private key file [{privateKeyPath}] is empty");
            return false;
        }
        try
        {
            privateKeyFile = SecurityUtils.decrypt(privateKeyFile);
            
            // Use the new PrivateKeyUtil to load the private key
            privateKey = PrivateKeyUtil.loadPrivateKey(privateKeyFile, keyPassword);
        }
        catch (Exception e)
        {
            log?.LogError(e, $"Failed to read private key [{privateKeyPath}]");
            return false;
        }
        return true;
    }
   
    private bool readXmlFile()
    {
        xmlFile = ReaderHelper.readFileAsString(xmlPath);
        if (string.IsNullOrWhiteSpace(xmlFile))
        {
            log?.LogInformation($"XML file [{xmlPath}] is empty");
            return false;
        }
        return true;
    }


}