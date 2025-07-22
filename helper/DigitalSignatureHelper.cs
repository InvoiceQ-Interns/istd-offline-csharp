
using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using ISTD_OFFLINE_CSHARP.DTOs;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class DigitalSignatureHelper
    {
        private readonly ILogger log;

        public DigitalSignatureHelper()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<DigitalSignatureHelper>();
        }

        public DigitalSignature getDigitalSignature( ECPrivateKeyParameters privateKeyParams, string invoiceHash)
        {


            byte[] xmlHashingBytes = Convert.FromBase64String(invoiceHash);
            byte[] digitalSignatureBytes = signECDSA(privateKeyParams, xmlHashingBytes);

            string digitalSignature = Convert.ToBase64String(digitalSignatureBytes);
            return new DigitalSignature(digitalSignature, xmlHashingBytes);


        }

        private byte[] signECDSA( ECPrivateKeyParameters privateKey, byte[] messageHash)
        {
            var signer = new ECDsaSigner();
            signer.Init(true, privateKey);

            // Sign the hashed message, returns BigInteger[] { r, s }
            var signatureComponents = signer.GenerateSignature(messageHash);

            var r = signatureComponents[0];
            var s = signatureComponents[1];
            
            var sequence = new Org.BouncyCastle.Asn1.DerSequence(
                new Org.BouncyCastle.Asn1.DerInteger(r),
                new Org.BouncyCastle.Asn1.DerInteger(s)
            );

            return sequence.GetDerEncoded();
        }
    }
}
