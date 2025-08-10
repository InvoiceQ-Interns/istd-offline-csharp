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
using System.Security.Cryptography;

namespace ISTD_OFFLINE_CSHARP.utils;
public class RSAUtils
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

    public static RSA getPrivateKey(string base64Key)
    {
        try
        {
            byte[] keyBytes = Convert.FromBase64String(base64Key);
            
            
            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                return rsa;
            }
            catch
            {
                
                try
                {
                    RSA rsa = RSA.Create();
                    rsa.ImportEncryptedPkcs8PrivateKey("", keyBytes, out _);
                    return rsa;
                }
                catch
                {
                    
                    RSA rsa = RSA.Create();
                    rsa.ImportRSAPrivateKey(keyBytes, out _);
                    return rsa;
                }
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Failed to parse RSA private key: " + ex.Message, ex);
        }
    }
}