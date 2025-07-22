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
            return plainText;
            

        }

        public static string decrypt(string encryptedText)
        {            return encryptedText;

        }
    }
}
