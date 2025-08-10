using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.processor;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using ISTD_OFFLINE_CSHARP.Helper;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Utilities.IO.Pem;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl
{
    public class CsrKeysProcessor : processor.ActionProcessor
    {
        private readonly ILogger log;

        public CsrKeysProcessor()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<CsrKeysProcessor>();
        }

        private string outputDirectory = "";
        private string configFilePath = "";
        private CsrConfigDto csrConfigDto;
        private CsrResponseDto csrResponse;
        private string csrPem;
        private string csrDerBase64;
        private string encryptedPrivateKeyBase64;
        private string publicKeyPem;

        protected override bool loadArgs(string[] args)
        {
            if (args.Length != 5)
            {
                log.LogInformation("Usage: dotnet run generate-csr-keys <directory> <en-name> <serial-number> <key-password> <config-file>");
                return false;
            }

            outputDirectory = args[0];
            string enName = args[1];
            string serialNumber = args[2];
            string keyPassword = args[3];
            configFilePath = args[4];

            csrConfigDto = new CsrConfigDto();
            csrConfigDto.setEnName(enName);
            csrConfigDto.setSerialNumber(serialNumber);
            csrConfigDto.setKeyPassword(keyPassword);

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
                log.LogInformation("Config file path is required");
                return false;
            }

            string configFile = ReaderHelper.readFileAsString(configFilePath);
            if (string.IsNullOrWhiteSpace(configFile))
            {
                log.LogInformation($"Config file [{configFilePath}] is empty");
                return false;
            }

            CsrConfigDto configFromFile = JsonUtils.readJson<CsrConfigDto>(configFile);
            if (configFromFile == null)
            {
                log.LogInformation($"Config file [{configFilePath}] is invalid");
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

            return validateCsrConfig();
        }

        private bool validateCsrConfig()
        {
            if (string.IsNullOrWhiteSpace(csrConfigDto.getEnName()))
            {
                log.LogInformation("Please enter a valid Name.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(csrConfigDto.getSerialNumber()))
            {
                log.LogInformation("Please enter a valid Serial Number.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(csrConfigDto.getKeyPassword()))
            {
                log.LogInformation("Please enter a password for the private key.");
                return false;
            }

            if (csrConfigDto.getKeySize() < 1024)
            {
                log.LogInformation("Key size must be at least 1024 bits");
                return false;
            }

            return true;
        }

        protected override bool process()
        {
            try
            {
                string subjectDn = csrConfigDto.getSubjectDn();
                log.LogInformation($"Generated DN: {subjectDn}");
                log.LogInformation($"RSA key size: {csrConfigDto.getKeySize()}");

                if (!string.IsNullOrWhiteSpace(csrConfigDto.getTemplateOid()))
                {
                    log.LogInformation($"Certificate template OID: {csrConfigDto.getTemplateOid()} (v{csrConfigDto.getMajorVersion()}.{csrConfigDto.getMinorVersion()})");
                }
                
                csrResponse = CmsRequestHelper.createCsr(csrConfigDto);
                
                byte[] publicKeyBytes = extractPublicKeyFromPrivateKey(csrResponse.getPrivateKeyBytes(), csrConfigDto.getKeyPassword());

                string csrBase64 = Convert.ToBase64String(csrResponse.getCsrDer());
                string cleanedCsr = StringUtils.CleanCsrString(csrBase64);

                csrPem = convertToPem("CERTIFICATE REQUEST", csrResponse.getCsrDer());
                csrDerBase64 = cleanedCsr;
                encryptedPrivateKeyBase64 = Convert.ToBase64String(csrResponse.getPrivateKeyBytes());
                publicKeyPem = convertToPem("PUBLIC KEY", publicKeyBytes);

                log.LogInformation("Successfully generated CSR, encrypted private key, and public key");
                return true;
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to generate CSR");
                return false;
            }
        }

        private byte[] extractPublicKeyFromPrivateKey(byte[] encryptedPrivateKeyBytes, string password)
        {
            using (RSA rsa = RSA.Create())
            {
                // Import the encrypted private key
                rsa.ImportEncryptedPkcs8PrivateKey(password, encryptedPrivateKeyBytes, out _);
                
                // Export the public key
                return rsa.ExportSubjectPublicKeyInfo();
            }
        }

        protected override bool output()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string commonName = extractCommonNameFromDN(csrConfigDto.getSubjectDn());
            string baseFileName = $"{commonName}_{timestamp}";

            string csrFile = Path.Combine(outputDirectory, $"{baseFileName}.csr");
            string keyFile = Path.Combine(outputDirectory, $"{baseFileName}.key");
            string pubFile = Path.Combine(outputDirectory, $"{baseFileName}.pub");

            bool valid = WriterHelper.writeFile(csrFile, SecurityUtils.encrypt(csrDerBase64));
            valid = WriterHelper.writeFile(keyFile, SecurityUtils.encrypt(encryptedPrivateKeyBase64)) && valid;
            valid = WriterHelper.writeFile(pubFile, SecurityUtils.encrypt(publicKeyPem)) && valid;

            if (valid)
            {
                log.LogInformation($"Successfully saved:");
                log.LogInformation($"  CSR: {csrFile}");
                log.LogInformation($"  Private Key: {keyFile}");
                log.LogInformation($"  Public Key: {pubFile}");
            }

            return valid;
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
                        return System.Text.RegularExpressions.Regex.Replace(
                            trimmed.Substring(3).Trim(), 
                            "[^a-zA-Z0-9_-]", 
                            "_");
                    }
                }
                return "CSR";
            }
            catch (Exception)
            {
                return "CSR";
            }
        }

        private string convertToPem(string type, byte[] derBytes)
        {
            try
            {
                var pemObject = new PemObject(type, derBytes);
                using var stringWriter = new StringWriter();
                using var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
                pemWriter.WriteObject(pemObject);
                pemWriter.Writer.Flush();
                return stringWriter.ToString();
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to convert to PEM format");
                throw;
            }
        }
    }
}