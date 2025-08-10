using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using Microsoft.Extensions.Logging;
using ISTD_OFFLINE_CSHARP.utils;

namespace ISTD_OFFLINE_CSHARP.utils
{
    public static class PrivateKeyUtil
    {
        private static readonly ILogger log = LoggingUtils.getLoggerFactory().CreateLogger(typeof(PrivateKeyUtil));

        public static RSA loadPrivateKey(string privateKeyContent, string password = "")
        {
            if (privateKeyContent.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----"))
            {
                return loadEncryptedPKCS8PrivateKey(privateKeyContent, password);
            }
            else if (privateKeyContent.Contains("-----BEGIN PRIVATE KEY-----"))
            {
                return loadUnencryptedPKCS8PrivateKey(privateKeyContent);
            }
            else if (privateKeyContent.Contains("-----BEGIN RSA PRIVATE KEY-----"))
            {
                return loadRSAPrivateKey(privateKeyContent);
            }
            else
            {
                return loadPrivateKeyFromBase64(privateKeyContent, password);
            }
        }

        private static RSA loadEncryptedPKCS8PrivateKey(string privateKeyContent, string password)
        {
            try
            {
                using var stringReader = new StringReader(privateKeyContent);
                using var pemReader = new PemReader(stringReader, new PasswordFinder(password));
                
                var keyObject = pemReader.ReadObject();
                
                if (keyObject is AsymmetricCipherKeyPair keyPair)
                {
                    return convertToRSA(keyPair.Private);
                }
                else if (keyObject is AsymmetricKeyParameter keyParam)
                {
                    return convertToRSA(keyParam);
                }
                else
                {
                    throw new Exception("Expected encrypted private key but found different format");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to load encrypted PKCS8 private key");
                throw new Exception("Failed to load encrypted PKCS8 private key: " + ex.Message, ex);
            }
        }

        private static RSA loadUnencryptedPKCS8PrivateKey(string privateKeyContent)
        {
            try
            {
                string cleanKey = privateKeyContent
                    .Replace("-----BEGIN PRIVATE KEY-----", "")
                    .Replace("-----END PRIVATE KEY-----", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace(" ", "")
                    .Replace("\t", "");

                byte[] keyBytes = Convert.FromBase64String(cleanKey);
                
                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                return rsa;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to load unencrypted PKCS8 private key");
                throw new Exception("Failed to load unencrypted PKCS8 private key: " + ex.Message, ex);
            }
        }

        private static RSA loadRSAPrivateKey(string privateKeyContent)
        {
            try
            {
                using var stringReader = new StringReader(privateKeyContent);
                using var pemReader = new PemReader(stringReader);
                
                var keyObject = pemReader.ReadObject();
                
                if (keyObject is AsymmetricCipherKeyPair keyPair)
                {
                    return convertToRSA(keyPair.Private);
                }
                else if (keyObject is AsymmetricKeyParameter keyParam)
                {
                    return convertToRSA(keyParam);
                }
                else
                {
                    throw new Exception("Unable to parse RSA private key format");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to load RSA private key");
                throw new Exception("Failed to load RSA private key: " + ex.Message, ex);
            }
        }

        private static RSA loadPrivateKeyFromBase64(string privateKeyBase64, string password)
        {
            try
            {
                byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
                log.LogInformation($"Loaded {privateKeyBytes.Length} bytes from base64");

                // Try to parse as PEM first
                string pemContent = Encoding.UTF8.GetString(privateKeyBytes);
                if (pemContent.Contains("-----BEGIN"))
                {
                    log.LogInformation("Detected PEM format, delegating to PEM parser");
                    return loadPrivateKey(pemContent, password);
                }

                // Try encrypted PKCS8 first
                try
                {
                    log.LogInformation("Attempting encrypted PKCS8 import");
                    RSA rsa = RSA.Create();
                    if (!string.IsNullOrEmpty(password))
                    {
                        rsa.ImportEncryptedPkcs8PrivateKey(password, privateKeyBytes, out _);
                        log.LogInformation("Successfully imported encrypted PKCS8 private key");
                    }
                    else
                    {
                        rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                        log.LogInformation("Successfully imported unencrypted PKCS8 private key");
                    }
                    return rsa;
                }
                catch (Exception encryptedException)
                {
                    log.LogWarning($"Encrypted PKCS8 failed: {encryptedException.Message}");
                    // Try unencrypted PKCS8
                    try
                    {
                        log.LogInformation("Attempting unencrypted PKCS8 import");
                        RSA rsa = RSA.Create();
                        rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                        log.LogInformation("Successfully imported PKCS8 private key");
                        return rsa;
                    }
                    catch (Exception pkcs8Exception)
                    {
                        log.LogWarning($"PKCS8 failed: {pkcs8Exception.Message}");
                        // Try RSA private key format
                        try
                        {
                            log.LogInformation("Attempting RSA private key import");
                            RSA rsa = RSA.Create();
                            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                            log.LogInformation("Successfully imported RSA private key");
                            return rsa;
                        }
                        catch (Exception rsaException)
                        {
                            log.LogError($"All import methods failed - RSA: {rsaException.Message}");
                            throw new Exception($"Unable to parse private key format. " +
                                              $"Encrypted PKCS#8 error: {encryptedException.Message}, " +
                                              $"PKCS#8 error: {pkcs8Exception.Message}, " +
                                              $"RSA error: {rsaException.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to load private key from base64");
                throw new Exception("Failed to load private key from base64: " + ex.Message, ex);
            }
        }

        private static RSA convertToRSA(AsymmetricKeyParameter privateKey)
        {
            try
            {
                if (privateKey is Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters rsaKey)
                {
                    var rsa = RSA.Create();
                    var parameters = new RSAParameters
                    {
                        Modulus = rsaKey.Modulus.ToByteArrayUnsigned(),
                        Exponent = rsaKey.PublicExponent.ToByteArrayUnsigned(),
                        D = rsaKey.Exponent.ToByteArrayUnsigned(), // Private exponent
                        P = rsaKey.P.ToByteArrayUnsigned(),
                        Q = rsaKey.Q.ToByteArrayUnsigned(),
                        DP = rsaKey.DP.ToByteArrayUnsigned(),
                        DQ = rsaKey.DQ.ToByteArrayUnsigned(),
                        InverseQ = rsaKey.QInv.ToByteArrayUnsigned()
                    };
                    rsa.ImportParameters(parameters);
                    return rsa;
                }
                else
                {
                    throw new Exception("Only RSA private keys are supported");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to convert BouncyCastle key to RSA");
                throw new Exception("Failed to convert BouncyCastle key to RSA: " + ex.Message, ex);
            }
        }

        private class PasswordFinder : IPasswordFinder
        {
            private readonly string password;

            public PasswordFinder(string password)
            {
                this.password = password ?? "";
            }

            public char[] GetPassword()
            {
                return password.ToCharArray();
            }
        }
    }
}
