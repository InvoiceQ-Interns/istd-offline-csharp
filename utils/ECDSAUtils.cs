using System;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace ISTD_OFFLINE_CSHARP.utils;
public class ECDSAUtil
{
    public static AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var curve = SecNamedCurves.GetByName("secp256k1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        
        var keyGen = new ECKeyPairGenerator();
        var secureRandom = new SecureRandom();
        var keyGenParam = new ECKeyGenerationParameters(domainParams, secureRandom);
        keyGen.Init(keyGenParam);
        
        return keyGen.GenerateKeyPair();
    }
    public static ECPrivateKeyParameters getPrivateKey(string base64Key)
    {
        byte[] keyBytes = Convert.FromBase64String(base64Key);

        // Try parsing the EC PRIVATE KEY (SEC1)
        Asn1Sequence seq = (Asn1Sequence)Asn1Object.FromByteArray(keyBytes);
        DerOctetString privateKeyOctet = seq[1] as DerOctetString;

        if (privateKeyOctet == null)
            throw new ArgumentException("Failed to parse EC private key");

        BigInteger d = new BigInteger(1, privateKeyOctet.GetOctets());

        var curve = SecNamedCurves.GetByName("secp256k1");
        var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        return new ECPrivateKeyParameters(d, domainParams);
    }
}