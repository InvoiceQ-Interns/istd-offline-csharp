
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Utilities;
using System.Security.Cryptography;

namespace ISTD_OFFLINE_CSHARP.utils
{
    public class ECDSAUtil
    {
        public static AsymmetricCipherKeyPair getKeyPair()
        {
            var ecParams = SecNamedCurves.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);

            var generator = new ECKeyPairGenerator();
            var secureRandom = new SecureRandom();
            var keyGenParams = new ECKeyGenerationParameters(domainParams, secureRandom);

            generator.Init(keyGenParams);
            return generator.GenerateKeyPair();
        }

        public static ECPrivateKeyParameters getPrivateKey(string base64Pkcs8)
        {
            byte[] pkcs8Bytes = Convert.FromBase64String(base64Pkcs8);
            AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(pkcs8Bytes);
            return (ECPrivateKeyParameters)privateKey;
        }
    }
}
