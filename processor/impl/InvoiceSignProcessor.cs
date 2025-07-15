using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class InvoiceSignProcessor : processor.ActionProcessor
{

    private readonly ILogger log;
    private readonly SigningHelper signingHelper;
    private string xmlPath = "";
    private string privateKeyPath = "";
    private string certificatePath = "";
    private string outputFile = "";
    private  ECDsa privateKey;
    private string xmlFile = "";
    private string certificateStr = "";
    private EInvoiceSigningResults signingResults;

    public InvoiceSignProcessor(ILogger<processor.ActionProcessor> log) : base(log)
    {
        this.log = log;
    }

    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 4)
        {
            log?.LogInformation("Usage: dotnet run invoice-sign <xml-path> <private-key-path> <certificate-path> <output-file>");
            return false;
        }
        xmlPath = args[0];
        privateKeyPath = args[1];
        certificatePath = args[2];
        outputFile = args[3];
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
            string key = privateKeyFile
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "");

            privateKey = ECDSAUtil.getPrivateKey(key);
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