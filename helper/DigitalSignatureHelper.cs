using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

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
            byte[] xmlHashingBytes = Convert.FromBase64String(invoiceHash);
            byte[] digitalSignatureBytes = signRSA(privateKey, xmlHashingBytes);

            string digitalSignature = Convert.ToBase64String(digitalSignatureBytes);
            return new DigitalSignature(digitalSignature, xmlHashingBytes);
        }

        private byte[] signRSA(RSA privateKey, byte[] messageHash)
        {
            try
            {
                return privateKey.SignHash(messageHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to sign with RSA");
                throw;
            }
        }
    }
}
