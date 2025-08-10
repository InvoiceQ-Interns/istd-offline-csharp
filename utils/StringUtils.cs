using System;
using System.Text;

namespace ISTD_OFFLINE_CSHARP.utils
{
   
    public class StringUtils
    {
      
        public static string ConvertPrivateKeyBytesToPem(byte[] privateKeyBytes)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("-----BEGIN ENCRYPTED PRIVATE KEY-----\n");
            builder.Append(InsertLineBreaks(Convert.ToBase64String(privateKeyBytes), 64));
            builder.Append("\n-----END ENCRYPTED PRIVATE KEY-----\n");
            return builder.ToString();
        }

       
        public static string ConvertCertificateBytesToPem(byte[] certBytes)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("-----BEGIN CERTIFICATE-----\n");
            builder.Append(InsertLineBreaks(Convert.ToBase64String(certBytes), 64));
            builder.Append("\n-----END CERTIFICATE-----\n");
            return builder.ToString();
        }

        
        public static string CleanCsrString(string rawCsr)
        {
            if (string.IsNullOrWhiteSpace(rawCsr))
            {
                return "";
            }

            return rawCsr
                .Replace("-----BEGIN CERTIFICATE REQUEST-----", "", StringComparison.OrdinalIgnoreCase)
                .Replace("-----END CERTIFICATE REQUEST-----", "", StringComparison.OrdinalIgnoreCase)
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Trim();
        }

       
        private static string InsertLineBreaks(string input, int lineLength)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < input.Length; i += lineLength)
            {
                result.Append(input.Substring(i, Math.Min(lineLength, input.Length - i)));
                if (i + lineLength < input.Length)
                {
                    result.Append("\n");
                }
            }
            return result.ToString();
        }
    }
}
