using ISTD_OFFLINE_CSHARP.client;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class ReportSubmitProcessor : processor.ActionProcessor
{
    private readonly RequesterGeneratorHelper requesterGeneratorHelper;
    private String xmlPath = "";
    private String productionCertificateResponsePath = "";
    private String signedXml;
    private CertificateResponse productionCertificateResponse;
    private FotaraClient fClient;
    private EInvoiceResponse eInvoiceResponse;
    private String outputPath;
    private readonly ILogger log;
    private String clientId;
    private String secretKey;
    private byte[] invoiceBytes;
    private String encodedXml;
    public ReportSubmitProcessor() 
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<ReportSubmitProcessor>();
    }

    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            log?.LogInformation("Usage: dotnet run submit <client-id> <secret-key> <signed-xml-path>");
            return false;
        }

        clientId = args[0];
        secretKey = args[1];
        xmlPath = args[2];
        fClient = new FotaraClient(propertiesManager);

        return true;
    }

    protected override bool validateArgs()
    {
        if (string.IsNullOrWhiteSpace(clientId)){
            log?.LogInformation("Invalid client ID");
            return false;
        }
        if (string.IsNullOrWhiteSpace(secretKey)) {
            log?.LogInformation("Invalid secret key");
            return false; 
        }

        signedXml = ReaderHelper.readFileAsString(xmlPath);
        if (string.IsNullOrWhiteSpace(signedXml))
        {
            log?.LogInformation($"Invalid signed xml [{xmlPath}]");
        }

        return true;
    }


    protected override bool process()
    {
        invoiceBytes = System.Text.Encoding.UTF8.GetBytes(signedXml);

        encodedXml = Convert.ToBase64String(invoiceBytes);
        fClient.reportInvoice(encodedXml, clientId, secretKey);
        return true;
    }


    protected override bool output()
    {
        log?.LogInformation($"Response [{JsonUtils.toJson(eInvoiceResponse)}]");
      
        return true;
    }

}