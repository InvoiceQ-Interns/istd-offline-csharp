using Microsoft.Extensions.Logging;
using System;
using ISTD_OFFLINE_CSHARP.client;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using System;
public class ComplianceSubmitProcessor : processor.ActionProcessor
{
    private readonly ILogger log;
    private readonly RequesterGeneratorHelper requesterGeneratorHelper;
    private string xmlPath = "";
    private string complianceCertificatePath = "";
    private string signedXml;
    private CertificateResponse productionCertificateResponse;
    private FotaraClient client;
    private ComplianceInvoiceResponse eInvoiceResponse;
    private string outputPath;

    public ComplianceSubmitProcessor()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<ComplianceSubmitProcessor>();
        requesterGeneratorHelper = new RequesterGeneratorHelper();
    }

    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            log?.LogInformation("Usage: dotnet run submit <signed-xml-path> <compliance-certificate-path> <output-path>");
            return false;
        }
        
        xmlPath = args[0];
        complianceCertificatePath = args[1];
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
            return false;
        }

        string productionCertificateResponseStr = ReaderHelper.readFileAsString(complianceCertificatePath);
        if (string.IsNullOrWhiteSpace(productionCertificateResponseStr))
        {
            log?.LogInformation($"Invalid production certificate response [{complianceCertificatePath}]");
            return false;
        }

        productionCertificateResponseStr = SecurityUtils.decrypt(productionCertificateResponseStr);
        productionCertificateResponse = new CertificateResponse
        {
            binarySecurityToken = productionCertificateResponseStr,
            secret = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(productionCertificateResponseStr))
        };

        return !string.IsNullOrWhiteSpace(productionCertificateResponseStr);
    }

    protected override bool process()
    {
        string jsonBody = requesterGeneratorHelper.generateEInvoiceRequest(signedXml);
        eInvoiceResponse = client.complianceInvoice(productionCertificateResponse, jsonBody);
        return eInvoiceResponse != null;
    }

    protected override bool output()
    {
        log?.LogInformation($"Response [{JsonUtils.toJson(eInvoiceResponse)}]");
        WriterHelper.writeFile(outputPath, JsonUtils.toJson(eInvoiceResponse));
        return true;
    }

}