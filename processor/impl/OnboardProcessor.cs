using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ISTD_OFFLINE_CSHARP.client;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.helper;
using ISTD_OFFLINE_CSHARP.Helper;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.processor;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl
{
    public class OnboardProcessor : processor.ActionProcessor
    {
        private static readonly string DateFormat = "yyyy-MM-dd";

        private string outputDirectory = "";
        private string configFilePath = "";
        private string csrEncoded = "";
        private RSA privateKey;
        private string deviceId;
        private string taxPayerNumber;
        private CsrConfigDto csrConfigDto;
        private string otp;
        private string complianceCertificateStr;
        private CertificateResponse complianceCsrResponse;
        private CertificateResponse prodCertificateResponse;
        private readonly Queue<string> testQueue = new Queue<string>();
        private readonly Dictionary<string, string> signedXmlMap = new Dictionary<string, string>();
        private readonly SigningHelper signingHelper = new SigningHelper();
        private readonly RequesterGeneratorHelper requesterGeneratorHelper = new RequesterGeneratorHelper();
        private FotaraClient client;

        protected override bool loadArgs(string[] args)
        {
            if (args.Length != 6)
            {
                log?.LogInformation("Usage: dotnet run onboard <otp> <output-directory> <en-name> <serial-number> <key-password> <config-file>");
                return false;
            }

            if (string.IsNullOrWhiteSpace(args[0]) || !Regex.IsMatch(args[0], @"^\d{6}$"))
            {
                log?.LogInformation("Invalid otp - must be 6 digits");
                return false;
            }

            otp = args[0];
            outputDirectory = args[1];
            string enName = args[2];
            string serialNumber = args[3];
            string keyPassword = args[4];
            configFilePath = args[5];

            csrConfigDto = new CsrConfigDto();
            csrConfigDto.setEnName(enName);
            csrConfigDto.setSerialNumber(serialNumber);
            csrConfigDto.setKeyPassword(keyPassword);

            client = new FotaraClient(propertiesManager);
            return true;
        }

        protected override bool validateArgs()
        {
            if (!ReaderHelper.isDirectoryExists(outputDirectory))
            {
                log?.LogInformation($"Output directory [{outputDirectory}] does not exist");
                return false;
            }

            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                log?.LogInformation("Config file path is required");
                return false;
            }

            string configFile = ReaderHelper.readFileAsString(configFilePath);
            if (string.IsNullOrWhiteSpace(configFile))
            {
                log?.LogInformation($"Config file [{configFilePath}] is empty");
                return false;
            }

            CsrConfigDto configFromFile = JsonUtils.readJson<CsrConfigDto>(configFile);
            if (configFromFile == null)
            {
                log?.LogInformation($"Config file [{configFilePath}] is invalid");
                return false;
            }

            if (configFromFile.getKeySize() > 0)
            {
                csrConfigDto.setKeySize(configFromFile.getKeySize());
            }
            if (!string.IsNullOrWhiteSpace(configFromFile.getTemplateOid()))
            {
                csrConfigDto.setTemplateOid(configFromFile.getTemplateOid());
            }
            if (configFromFile.getMajorVersion() > 0)
            {
                csrConfigDto.setMajorVersion(configFromFile.getMajorVersion());
            }
            if (configFromFile.getMinorVersion() >= 0)
            {
                csrConfigDto.setMinorVersion(configFromFile.getMinorVersion());
            }

            return true;
        }

        protected override bool process()
        {
            CsrKeysProcessor csrKeysProcessor = new CsrKeysProcessor();
            string[] csrArgs = { outputDirectory, csrConfigDto.getEnName(), csrConfigDto.getSerialNumber(),
                               csrConfigDto.getKeyPassword(), configFilePath };
            bool isValid = csrKeysProcessor.process(csrArgs, propertiesManager);
            if (!isValid)
            {
                log?.LogError("Failed to generate CSR and keys");
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
                log?.LogInformation("Failed to compliance csr");
                return false;
            }
            if (!enrichTestQueue())
            {
                log?.LogInformation("Failed to create test xmls");
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
            foreach (string key in signedXmlMap.Keys)
            {
                valid = WriterHelper.writeFile(outputDirectory + "/" + key, signedXmlMap[key]) && valid;
            }
            string productCertificate = outputDirectory + "/production_csid.cert";
            string productionResponse = outputDirectory + "/production_response.json";
            valid = WriterHelper.writeFile(productionResponse, SecurityUtils.encrypt(JsonUtils.toJson(prodCertificateResponse))) && valid;
            valid = WriterHelper.writeFile(productCertificate,
                    SecurityUtils.encrypt(Encoding.UTF8.GetString(Convert.FromBase64String(prodCertificateResponse.getBinarySecurityToken()))))
                    && valid;
            return valid;
        }

        private bool loadCsrConfigs()
        {
            try
            {
                string timestamp = findLatestTimestamp();
                if (timestamp == null)
                {
                    log?.LogError("No CSR files found in output directory");
                    return false;
                }

                string commonName = extractCommonNameFromDN(csrConfigDto.getSubjectDn());
                string baseFileName = $"{commonName}_{timestamp}";
                string csrFile = outputDirectory + "/" + baseFileName + ".csr";

                csrEncoded = SecurityUtils.decrypt(ReaderHelper.readFileAsString(csrFile));

                string[] serialNumberParts = csrConfigDto.getSerialNumber().Split('|');
                if (serialNumberParts.Length >= 3)
                {
                    deviceId = serialNumberParts[2];
                    taxPayerNumber = serialNumberParts[0];
                }
                else
                {
                    deviceId = "1";
                    taxPayerNumber = csrConfigDto.getSerialNumber();
                }
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
                string timestamp = findLatestTimestamp();
                if (timestamp == null)
                {
                    log?.LogError("No private key files found in output directory");
                    return false;
                }

                string commonName = extractCommonNameFromDN(csrConfigDto.getSubjectDn());
                string baseFileName = $"{commonName}_{timestamp}";
                string keyFile = outputDirectory + "/" + baseFileName + ".key";

                string privateKeyBase64 = SecurityUtils.decrypt(ReaderHelper.readFileAsString(keyFile));
                byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

                // Try to load as encrypted PKCS#8 first, then fallback to plain PKCS#8
                try
                {
                    privateKey = RSA.Create();
                    privateKey.ImportEncryptedPkcs8PrivateKey(csrConfigDto.getKeyPassword(), privateKeyBytes, out _);
                }
                catch
                {
                    // Fallback to plain PKCS#8
                    privateKey = RSA.Create();
                    privateKey.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                }
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
            prodCertificateResponse = client.getProdCertificate(complianceCsrResponse, complianceCsrResponse.getRequestID());
            return prodCertificateResponse != null && string.Equals(prodCertificateResponse.getDispositionMessage(), "ISSUED", StringComparison.OrdinalIgnoreCase);
        }

        private bool complianceInvoices()
        {
            bool valid = true;
            int counter = 0;
            while (testQueue.Count > 0)
            {
                string xml = testQueue.Dequeue();
                EInvoiceSigningResults signingResults = signingHelper.signEInvoice(xml, privateKey, complianceCertificateStr);

                string jsonBody = requesterGeneratorHelper.generateEInvoiceRequest(signingResults.getInvoiceHash(), signingResults.getInvoiceUUID(), signingResults.getSignedXml());
                ComplianceInvoiceResponse complianceInvoiceResponse = client.complianceInvoice(complianceCsrResponse, jsonBody);
                if (complianceInvoiceResponse == null || !complianceInvoiceResponse.isValid())
                {
                    log?.LogInformation($"Failed to compliance invoice [{Convert.ToBase64String(Encoding.UTF8.GetBytes(xml))}] and error [{JsonUtils.toJson(complianceInvoiceResponse)}]");
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
            complianceCsrResponse = client.complianceCsr(otp, csrEncoded);
            complianceCertificateStr = Encoding.UTF8.GetString(Convert.FromBase64String(complianceCsrResponse.getBinarySecurityToken()));
            return complianceCsrResponse != null && string.Equals(complianceCsrResponse.getDispositionMessage(), "ISSUED", StringComparison.OrdinalIgnoreCase);
        }

        private bool enrichTestQueue()
        {
            bool valid = false;
            try
            {
                int counter = 0;
                testQueue.Enqueue(enrichFile(ReaderHelper.readFileFromResource("samples/b2b_invoice.xml") ?? throw new Exception("Missing b2b_invoice"), counter++));
                testQueue.Enqueue(enrichFile(ReaderHelper.readFileFromResource("samples/b2b_credit.xml") ?? throw new Exception("Missing b2b_credit"), counter++));
                valid = true;
            }
            catch (Exception e)
            {
                log?.LogError(e, "Failed to enrich test queue");
                return false;
            }
            return valid;
        }

        private string enrichFile(string file, int counter)
        {
            string id = $"{taxPayerNumber}_{deviceId}_{counter}";
            string orgId = $"{taxPayerNumber}_{deviceId}_{counter - 1}";
            string formattedDate = DateTime.Now.ToString(DateFormat);
            
            string enrichedFile = file.Replace("${ID}", id);
            enrichedFile = enrichedFile.Replace("${UUID}", CreateGuid(id).ToString());
            enrichedFile = enrichedFile.Replace("${ISSUE_DATE}", formattedDate);
            enrichedFile = enrichedFile.Replace("${ORG_ID}", orgId);
            enrichedFile = enrichedFile.Replace("${ORG_UUID}", CreateGuid(orgId).ToString());
            enrichedFile = enrichedFile.Replace("${VAT_NUMBER}", taxPayerNumber);
            enrichedFile = enrichedFile.Replace("${TAXPAYER_NAME}", csrConfigDto.getEnName());
            enrichedFile = enrichedFile.Replace("${DEVICE_ID}", deviceId);
            return enrichedFile;
        }

        private Guid CreateGuid(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return new Guid(hash);
            }
        }

        private string findLatestTimestamp()
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(outputDirectory);
                FileInfo[] files = dir.GetFiles("*.csr").Concat(dir.GetFiles("*.key")).ToArray();
                if (files.Length == 0)
                {
                    return null;
                }

                string latestTimestamp = null;
                foreach (FileInfo file in files)
                {
                    string fileName = file.Name;
                    string[] parts = fileName.Split('_');
                    if (parts.Length >= 2)
                    {
                        string timestamp = parts[parts.Length - 1].Replace(".csr", "").Replace(".key", "");
                        if (latestTimestamp == null || string.Compare(timestamp, latestTimestamp) > 0)
                        {
                            latestTimestamp = timestamp;
                        }
                    }
                }
                return latestTimestamp;
            }
            catch (Exception e)
            {
                log?.LogError(e, "Failed to find latest timestamp");
                return null;
            }
        }

        private string extractCommonNameFromDN(string subjectDn)
        {
            try
            {
                string[] parts = subjectDn.Split(',');
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (trimmed.ToUpper().StartsWith("CN="))
                    {
                        return Regex.Replace(trimmed.Substring(3).Trim(), @"[^a-zA-Z0-9_-]", "_");
                    }
                }
                return "CSR";
            }
            catch (Exception)
            {
                return "CSR";
            }
        }
    }
}