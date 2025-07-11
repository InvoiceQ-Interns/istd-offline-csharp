
using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.DTOs;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class DigitalSignatureHelper
    {
        private readonly ILogger<DigitalSignatureHelper> _logger;

        public DigitalSignatureHelper(ILogger<DigitalSignatureHelper> logger)
        {
            _logger = logger;
        }

        public DigitalSignature getDigitalSignature(ECParameters privateKeyParams, string invoiceHash)
        {


            byte[] xmlHashingBytes = Convert.FromBase64String(invoiceHash);
            byte[] digitalSignatureBytes = signECDSA(privateKeyParams, xmlHashingBytes);

            string digitalSignature = Convert.ToBase64String(digitalSignatureBytes);
            return new DigitalSignature(digitalSignature, xmlHashingBytes);


        }

        private byte[] signECDSA(ECParameters privateKey, byte[] messageHash)
        {
            try
            {
                using (ECDsa ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportParameters(privateKey);
                    return ecdsa.SignHash(messageHash);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while signing the XML document.");
                return null;
            }
        }
    }
}
