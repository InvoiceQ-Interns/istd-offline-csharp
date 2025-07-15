
using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.DTOs;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class DigitalSignatureHelper
    {
        private readonly ILogger<DigitalSignatureHelper> log;

        public DigitalSignatureHelper(ILogger<DigitalSignatureHelper> log)
        {
            log = log;
        }

        public DigitalSignature getDigitalSignature( ECDsa privateKeyParams, string invoiceHash)
        {


            byte[] xmlHashingBytes = Convert.FromBase64String(invoiceHash);
            byte[] digitalSignatureBytes = signECDSA(privateKeyParams, xmlHashingBytes);

            string digitalSignature = Convert.ToBase64String(digitalSignatureBytes);
            return new DigitalSignature(digitalSignature, xmlHashingBytes);


        }

        private byte[] signECDSA( ECDsa privateKey, byte[] messageHash)
        {
            try
            {
                return privateKey.SignHash(messageHash);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Something went wrong while signing the XML document.");
                return null;
            }
        }
    }
}
