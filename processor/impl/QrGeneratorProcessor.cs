using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class QrGeneratorProcessor : processor.ActionProcessor
{

    private readonly SigningHelper signingHelper;
    private ILogger log;
    private String xmlPath = "";
    private String privateKeyPath = "";
    private String certificatePath = "";
    private  ECDsa privateKey;
    private String xmlFile;
    private String certificateStr;
    private EInvoiceSigningResults signingResults;
    
    public QrGeneratorProcessor(ILogger<processor.ActionProcessor> log) : base(log)
    {
        this.log = log;
    }
    
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: dotnet run invoice-sign <xml-path> <private-key-path> <certificate-path>");
            return false;
        }
        xmlPath = args[0];
        privateKeyPath = args[1];
        certificatePath = args[2];
        return true;
    }


    protected override bool validateArgs()
    {
        if (!readXmlFile()) return false;
        if (!readPrivateKey()) return false;
        return readCertificate();
    }


    protected override bool process()
    {
        signingResults = signingHelper.signEInvoice(xmlFile, privateKey, certificateStr);
        return signingResults != null && !string.IsNullOrWhiteSpace(signingResults.getQrCode());
    }


    protected override bool output()
    {
        log?.LogInformation(string.Format("invoice Hash [{0}]\n invoice QR Code: [{1}]\n", 
            signingResults.getInvoiceHash(), 
            signingResults.getQrCode()));

        return true;
    }

    
    private bool readCertificate()
    {
        certificateStr = ReaderHelper.readFileAsString(certificatePath);
        certificateStr = SecurityUtils.decrypt(certificateStr);
        return !string.IsNullOrWhiteSpace(certificateStr);
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
                .Replace(Environment.NewLine, "")
                .Trim();

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