using System.Security.Cryptography;
using System.Text;
using ISTD_OFFLINE_CSHARP.client;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class OnboardProcessor : processor.ActionProcessor
{
    private string outputDirectory = "";
    private string configFilePath = "";
    private string csrEncoded = "";
    private FotaraClient fClient;
    private  ECDsa privateKey;
    private string deviceId;
    private string taxPayerNumber;
    private CsrConfigDto csrConfigDto;
    private string otp;
    private string complianceCertificateStr;
    private CertificateResponse complianceCsrResponse;
    private CertificateResponse prodCertificateResponse;
    private Queue<string> testQueue = new Queue<string>(10);
    private Dictionary<string, string> signedXmlMap = new Dictionary<string, string>();
    private readonly SigningHelper signingHelper;
    private readonly RequesterGeneratorHelper requesterGeneratorHelper;
    private readonly ILogger<processor.ActionProcessor> log;
    private readonly ILogger<CsrKeysProcessor> CSRLog;
    
    public OnboardProcessor(ILogger<processor.ActionProcessor> log) : base(log)
    {
        this.log = log;
    }

   
    
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 3)
        {
            log?.LogInformation("Usage: dotnet run onboard <otp> <output-directory> <config-path>");
            return false;
        }

        if (string.IsNullOrWhiteSpace(args[0]) || !System.Text.RegularExpressions.Regex.IsMatch(args[0], @"^\d{6}$"))
        {
            log?.LogInformation("Invalid otp");
            return false;
        }

        otp = args[0];
        outputDirectory = args[1];
        configFilePath = args[2];
        fClient= new FotaraClient(propertiesManager);
        return true;
    }

    protected override bool validateArgs()
    {
        return true;
    }

    protected override bool process()
    {
        var csrKeysProcessor = new CsrKeysProcessor(CSRLog);

        bool isValid = csrKeysProcessor.process(
            new[] { outputDirectory, configFilePath },
            propertiesManager
        );

        if (!isValid)
        {
            return false;
        }

        if (!loadPrivateKey())
        {
            log?.LogInformation("Failed to load private key");
            return false;
        }

        if (!loadCsrConfigs())
        {
            log?.LogInformation("Failed to load CSR configs");
            return false;
        }

        if (!complianceCsr())
        {
            log?.LogInformation("Failed to compliance CSR");
            return false;
        }

        if (!enrichTestQueue())
        {
            log?.LogInformation("Failed to create test XMLs");
            return false;
        }

        if (!complianceInvoices())
        {
            log?.LogInformation("Failed to compliance invoices");
            return false;
        }

        if (!getProdCertificate())
        {
            log?.LogInformation("Failed to get prod certificate");
            return false;
        }

        return true;
    }


    protected override bool output()
    {
        bool valid = true;

        foreach (var entry in signedXmlMap)
        {
            string filePath = Path.Combine(outputDirectory, entry.Key);
            valid = WriterHelper.writeFile(filePath, entry.Value) && valid;
        }

        string productCertificate = Path.Combine(outputDirectory, "production_csid.cert");
        string productionResponse = Path.Combine(outputDirectory, "production_response.json");

        string productionResponseJson = JsonUtils.toJson(prodCertificateResponse);
        valid = WriterHelper.writeFile(productionResponse, SecurityUtils.encrypt(productionResponseJson)) && valid;

        string decodedCert = Encoding.UTF8.GetString(Convert.FromBase64String(prodCertificateResponse.binarySecurityToken));
        valid = WriterHelper.writeFile(productCertificate, SecurityUtils.encrypt(decodedCert)) && valid;

        return valid;
    }

    
    private bool loadCsrConfigs()
    {
        try
        {
            csrEncoded = SecurityUtils.decrypt(ReaderHelper.readFileAsString(Path.Combine(outputDirectory, "csr.encoded")));
        
            string configFileContent = ReaderHelper.readFileAsString(configFilePath);
            csrConfigDto = JsonUtils.readJson<CsrConfigDto>(configFileContent);
        
            if (csrConfigDto == null)
            {
                log?.LogError("CSR Config DTO is null.");
                return false;
            }

            string[] serialNumberParts = csrConfigDto.getSerialNumber().Split('|');
            deviceId = serialNumberParts[2];
            taxPayerNumber = serialNumberParts[0];
        }
        catch (Exception e)
        {
            log?.LogError(e, "Failed to load CSR configs");
            return false;
        }

        return true;
    }

    
    private bool loadPrivateKey()
    {
        try
        {
            string privateKeyPEM = ReaderHelper.readFileAsString(Path.Combine(outputDirectory, "private.pem"));
            if (string.IsNullOrWhiteSpace(privateKeyPEM))
            {
                log?.LogError("Private key file is empty or missing.");
                return false;
            }

            privateKeyPEM = SecurityUtils.decrypt(privateKeyPEM);

            string key = privateKeyPEM
                .Replace("-----BEGIN EC PRIVATE KEY-----", "")
                .Replace("-----END EC PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "")
                .Trim();
            
            privateKey = ECDSAUtil.getPrivateKey(key); 
        }
        catch (Exception e)
        {
            log?.LogError(e, "Failed to load private key");
            return false;
        }

        return true;
    }

    
    
    private bool getProdCertificate()
    {
        prodCertificateResponse = fClient.getProdCertificate(complianceCsrResponse, complianceCsrResponse.getRequestID());

        return prodCertificateResponse != null &&
               string.Equals(prodCertificateResponse.getDispositionMessage(), "ISSUED", StringComparison.OrdinalIgnoreCase);
    }
    
    private bool complianceInvoices()
    {
        bool valid = true;
        int counter = 0;

        while (testQueue.Count > 0)
        {
            string xml = testQueue.Dequeue();
            EInvoiceSigningResults signingResults = signingHelper.signEInvoice(xml, privateKey, complianceCertificateStr);

            string jsonBody = requesterGeneratorHelper.generateEInvoiceRequest(
                signingResults.getInvoiceHash(),
                signingResults.getInvoiceUUID(),
                signingResults.getSignedXml());

            ComplianceInvoiceResponse complianceInvoiceResponse = fClient.complianceInvoice(complianceCsrResponse, jsonBody);

            if (complianceInvoiceResponse == null || complianceInvoiceResponse.IsValid() != true)
            {
                string encodedXml = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xml));
                log?.LogInformation($"Failed to compliance invoice [{encodedXml}] and error [{JsonUtils.toJson(complianceInvoiceResponse)}]");
                valid = false;
            }
            else
            {
                string id = $"einvoice_test_{taxPayerNumber}_{deviceId}_{counter++}.xml";
                signedXmlMap[id] = signingResults.getSignedXml();
            }
        }

        return valid;
    }

    
    private bool complianceCsr()
    {
        complianceCsrResponse = fClient.complianceCsr(otp, csrEncoded);
        if (complianceCsrResponse == null)
            return false;

        byte[] decodedBytes = Convert.FromBase64String(complianceCsrResponse.binarySecurityToken);
        complianceCertificateStr = System.Text.Encoding.UTF8.GetString(decodedBytes);

        return string.Equals(complianceCsrResponse.getDispositionMessage(), "ISSUED", StringComparison.OrdinalIgnoreCase);
    }
    
    private bool enrichTestQueue()
    {
        bool valid = false;
        try
        {
            string invoiceType = csrConfigDto.getInvoiceType();
            bool isB2B = invoiceType[0] == '1';
            bool isB2C = invoiceType[1] == '1';
            int counter = 0;

            if (isB2B)
            {
                testQueue.Enqueue(EnrichFile(ReaderHelper.readFileFromResource("samples/b2b_invoice.xml") ?? throw new Exception("Missing b2b_invoice"), counter++));
                testQueue.Enqueue(EnrichFile(ReaderHelper.readFileFromResource("samples/b2b_credit.xml") ?? throw new Exception("Missing b2b_credit"), counter++));
                valid = true;
            }
            if (isB2C)
            {
                testQueue.Enqueue(EnrichFile(ReaderHelper.readFileFromResource("samples/b2b_invoice.xml") ?? throw new Exception("Missing b2b_invoice"), counter++));
                testQueue.Enqueue(EnrichFile(ReaderHelper.readFileFromResource("samples/b2b_credit.xml") ?? throw new Exception("Missing b2b_credit"), counter++));
                valid = true;
            }
        }
        catch (Exception e)
        {
            log?.LogError(e, "Failed to enrich test queue");
            return false;
        }

        return valid;
    }

    
    
    private string EnrichFile(string file, int counter)
    {
        string id = $"{taxPayerNumber}_{deviceId}_{counter}";
        string orgId = $"{taxPayerNumber}_{deviceId}_{counter - 1}";
        string formattedDate = DateTime.Now.ToString("yyyy-MM-dd");

        string enrichedFile = file
            .Replace("${ID}", id)
            .Replace("${UUID}", GuidFromString(id).ToString())
            .Replace("${ISSUE_DATE}", formattedDate)
            .Replace("${ORG_ID}", orgId)
            .Replace("${ORG_UUID}", GuidFromString(orgId).ToString())
            .Replace("${VAT_NUMBER}", taxPayerNumber)
            .Replace("${TAXPAYER_NAME}", csrConfigDto.getCommonName())
            .Replace("${DEVICE_ID}", deviceId);

        return enrichedFile;
    }
    
    private Guid GuidFromString(string input)
    {
        // Useing SHA1 to mimic UUID.nameUUIDFromBytes
        using (var sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            byte[] guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, 16);

            // Set UUID version to 5 (name-based using SHA-1) to match behavior
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | (5 << 4));
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

            return new Guid(guidBytes);
        }
    }


}