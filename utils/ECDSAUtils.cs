using System;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using System.IO;

namespace ISTD_OFFLINE_CSHARP.utils;
public class ECDSAUtils
{
    public static AsymmetricCipherKeyPair getKeyPair()
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
        try
        {
            byte[] keyBytes = Convert.FromBase64String(base64Key);
            
            // First try PKCS#8 format
            try
            {
                AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(keyBytes);
                
                if (privateKey is ECPrivateKeyParameters ecPrivateKey)
                {
                    return ecPrivateKey;
                }
                
                throw new ArgumentException("Decoded key is not an EC private key");
            }
            catch (Exception)
            {
                // If PKCS#8 fails, try SEC1 format (raw EC private key)
                return parseAsECPrivateKey(keyBytes);
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Failed to parse EC private key: " + ex.Message, ex);
        }
    }
    
    private static ECPrivateKeyParameters parseAsECPrivateKey(byte[] keyBytes)
    {
        try
        {
            
            Asn1Sequence seq = (Asn1Sequence)Asn1Object.FromByteArray(keyBytes);
            
            if (seq.Count < 2)
                throw new ArgumentException("Invalid EC private key sequence");
            
            DerOctetString privateKeyOctet = seq[1] as DerOctetString;
            
            if (privateKeyOctet == null)
                throw new ArgumentException("Invalid EC private key format - missing private key octet string");
            
            BigInteger d = new BigInteger(1, privateKeyOctet.GetOctets());
            
            // Use secp256k1 curve (adjust if needed)
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            
            return new ECPrivateKeyParameters(d, domainParams);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Failed to parse as SEC1 EC private key: " + ex.Message, ex);
        }
    }
}