using ISTD_OFFLINE_CSHARP.client;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class InvoiceSubmitProcessor : processor.ActionProcessor
{
    
    private readonly RequesterGeneratorHelper requesterGeneratorHelper;
    private string xmlPath = "";
    private string productionCertificateResponsePath = "";
    private string signedXml;
    private CertificateResponse productionCertificateResponse;
    private FotaraClient client;
    private EInvoiceResponse eInvoiceResponse;
    private string outputPath;
    private readonly ILogger log;
    
    public InvoiceSubmitProcessor()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<InvoiceSubmitProcessor>();
    }
    
    
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            log?.LogInformation("Usage: dotnet run submit <signed-xml-path> <production-certificate-response-path> <output-path>");
            return false;
        }

        xmlPath = args[0];
        productionCertificateResponsePath = args[1];
        outputPath = args[2];
        client = new FotaraClient(propertiesManager);

        return true;
    }

    protected override bool validateArgs()
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            log?.LogInformation("Invalid output path");
            return false;
        }

        signedXml = ReaderHelper.readFileAsString(xmlPath);
        if (string.IsNullOrWhiteSpace(signedXml))
        {
            log?.LogInformation($"Invalid signed xml [{xmlPath}]");
        }

        string productionCertificateResponseStr = ReaderHelper.readFileAsString(productionCertificateResponsePath);
        if (string.IsNullOrWhiteSpace(productionCertificateResponseStr))
        {
            log?.LogInformation($"Invalid production certificate response [{productionCertificateResponsePath}]");
        }

        productionCertificateResponseStr = SecurityUtils.decrypt(productionCertificateResponseStr);
        productionCertificateResponse = JsonUtils.readJson<CertificateResponse>(productionCertificateResponseStr);

        return productionCertificateResponse != null
               && !string.IsNullOrWhiteSpace(productionCertificateResponse.getSecret())
               && !string.IsNullOrWhiteSpace(productionCertificateResponse.getBinarySecurityToken());
    }

    protected override bool process()
    {
        string jsonBody = requesterGeneratorHelper.generateEInvoiceRequest(signedXml);
        eInvoiceResponse = client.submitInvoice(productionCertificateResponse, jsonBody);

        return eInvoiceResponse != null &&
               (string.Equals(eInvoiceResponse.getStatus(), "CLEARED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(eInvoiceResponse.getStatus(), "REPORTED", StringComparison.OrdinalIgnoreCase));
    }

    protected override bool output()
    {
        string responseJson = JsonUtils.toJson(eInvoiceResponse);
        log?.LogInformation($"Response [{responseJson}]");
        WriterHelper.writeFile(outputPath, responseJson);
        return true;
    }
}