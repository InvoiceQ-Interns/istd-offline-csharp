using System;
using System.Security.Cryptography;
using System.Text;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.utils
{
    public static class PrivateKeyUtil
    {
        private static readonly ILogger log = LoggingUtils.getLoggerFactory().CreateLogger(typeof(PrivateKeyUtil));

        public static RSA loadPrivateKey(string privateKeyContent, string password)
        {
            try
            {
                if (privateKeyContent.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----"))
                {
                    return loadEncryptedPKCS8PrivateKey(privateKeyContent, password);
                }
                else if (privateKeyContent.Contains("-----BEGIN PRIVATE KEY-----"))
                {
                    return loadUnencryptedPKCS8PrivateKey(privateKeyContent);
                }
                else
                {
                    return loadPrivateKeyFromBase64(privateKeyContent, password);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to load private key");
                throw new Exception("Unable to load private key: " + e.Message, e);
            }
        }

        private static RSA loadEncryptedPKCS8PrivateKey(string privateKeyContent, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required for encrypted private key");
            }

            string cleanKey = privateKeyContent
                .Replace("-----BEGIN ENCRYPTED PRIVATE KEY-----", "")
                .Replace("-----END ENCRYPTED PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");

            byte[] keyBytes = Convert.FromBase64String(cleanKey);

            RSA rsa = RSA.Create();
            rsa.ImportEncryptedPkcs8PrivateKey(password, keyBytes, out _);
            return rsa;
        }

        private static RSA loadUnencryptedPKCS8PrivateKey(string privateKeyContent)
        {
            string cleanKey = privateKeyContent
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");

            byte[] keyBytes = Convert.FromBase64String(cleanKey);

            RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            return rsa;
        }

        private static RSA loadPrivateKeyFromBase64(string privateKeyBase64, string password)
        {
            byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

            // Try to parse as PEM first
            string pemContent = Encoding.UTF8.GetString(privateKeyBytes);
            if (pemContent.Contains("-----BEGIN"))
            {
                return loadPrivateKey(pemContent, password);
            }

            // Try as encrypted PKCS#8
            if (!string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    RSA rsa = RSA.Create();
                    rsa.ImportEncryptedPkcs8PrivateKey(password, privateKeyBytes, out _);
                    return rsa;
                }
                catch (Exception)
                {
                    // Fall through to try unencrypted
                }
            }

            // Try as unencrypted PKCS#8
            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                return rsa;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to parse private key format: " + e.Message, e);
            }
        }
    }
}