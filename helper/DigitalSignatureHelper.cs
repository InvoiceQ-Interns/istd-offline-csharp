using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class DigitalSignatureHelper
    {
        private readonly ILogger log;

        public DigitalSignatureHelper()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<DigitalSignatureHelper>();
        }

        public DigitalSignature getDigitalSignature(RSA privateKey, string invoiceHash)
        {
            // Match Java exactly: decode the base64 after converting to UTF-8 bytes first
            byte[] invoiceHashBytes = Encoding.UTF8.GetBytes(invoiceHash);
            byte[] xmlHashingBytes = Convert.FromBase64String(Encoding.UTF8.GetString(invoiceHashBytes));
            
            byte[] digitalSignatureBytes = signWithPrivateKey(privateKey, xmlHashingBytes);

            string digitalSignature = Convert.ToBase64String(digitalSignatureBytes);
            return new DigitalSignature(digitalSignature, xmlHashingBytes);
        }

        private byte[] signWithPrivateKey(RSA privateKey, byte[] messageHash)
        {
            try
            {
                // Use SignData instead of SignHash to match Java's Signature.update() + sign() behavior
                // This performs raw signature on the data without additional hashing
                return privateKey.SignData(messageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to sign with RSA");
                throw;
            }
        }
    }
}
