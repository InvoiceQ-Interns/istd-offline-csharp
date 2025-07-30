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

        private static readonly string ALGORITHM = "AES/CBC/PKCS7Padding";
        private static readonly byte[] KEY_BYTES = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
        private static readonly byte[] IV_BYTES = Encoding.UTF8.GetBytes("abcdef9876543210");

        public static string encrypt(string plainText)
        {
            /*try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = KEY_BYTES;
                    aes.IV = IV_BYTES;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
            catch (Exception e)
            {*/
                // log.warn("failed to encrypt data");
                return plainText;
            //}
        }

        public static string decrypt(string encryptedText)
        {
            /*try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = KEY_BYTES;
                    aes.IV = IV_BYTES;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                        byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                }
            }
            catch (Exception e)
            {*/
                // log.warn("failed to decrypt data");
                return encryptedText;
            //}
        }
    }
}
