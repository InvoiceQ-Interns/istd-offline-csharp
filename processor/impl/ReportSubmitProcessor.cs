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
    public ReportSubmitProcessor() 
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<ReportSubmitProcessor>();
    }
    
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            log.LogInformation("Usage: dotnet run submit <signed-xml-path> <production-certificate-response-path> <output-path>");
            return false;
        }
        xmlPath = args[0];
        productionCertificateResponsePath = args[1];
        outputPath = args[2];
        fClient = new FotaraClient(propertiesManager);
        return true;
    }

    protected override bool validateArgs()
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            log.LogInformation("Invalid output path");
            return false;
        }

        signedXml = ReaderHelper.readFileAsString(xmlPath);
        if (string.IsNullOrWhiteSpace(signedXml))
        {
            log.LogInformation($"Invalid signed xml [{xmlPath}]");
        }

        string productionCertificateResponseStr = ReaderHelper.readFileAsString(productionCertificateResponsePath);
        if (string.IsNullOrWhiteSpace(productionCertificateResponseStr))
        {
            log.LogInformation($"Invalid production certificate response [{productionCertificateResponsePath}]");
        }

        productionCertificateResponseStr = SecurityUtils.decrypt(productionCertificateResponseStr);
        productionCertificateResponse = JsonUtils.readJson<CertificateResponse>(productionCertificateResponseStr);

        return productionCertificateResponse != null &&
               !string.IsNullOrWhiteSpace(productionCertificateResponse.getSecret()) &&
               !string.IsNullOrWhiteSpace(productionCertificateResponse.getBinarySecurityToken());
    }


    protected override bool process()
    {
        string jsonBody = requesterGeneratorHelper.generateEInvoiceRequest(signedXml);
        eInvoiceResponse = fClient.reportInvoice(productionCertificateResponse, jsonBody);
        return eInvoiceResponse != null && 
               (string.Equals(eInvoiceResponse.getStatus(), "CLEARED", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(eInvoiceResponse.getStatus(), "REPORTED", StringComparison.OrdinalIgnoreCase));
    }


    protected override bool output()
    {
        log?.LogInformation($"Response [{JsonUtils.toJson(eInvoiceResponse)}]");
        WriterHelper.writeFile(outputPath, JsonUtils.toJson(eInvoiceResponse));
        return true;
    }

}