using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.security
{
    public class SecurityUtils
    {
        private static ILogger log;

        public static void setLogger(ILoggerFactory loggerFactory)
        {
            log = loggerFactory.CreateLogger("SecurityUtils");
        }

        private static readonly string algorithm = "AES";
        private static readonly byte[] keyBytes = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
        private static readonly byte[] ivBytes = Encoding.UTF8.GetBytes("abcdef9876543210");

        public static string encrypt(string plainText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception)
            {
                return plainText;
            }
        }

        public static string decrypt(string encryptedText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] cipherBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception)
            {
                return encryptedText;
            }
        }
    }
}
