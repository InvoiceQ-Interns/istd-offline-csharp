using System;
using System.Text;
using System.Security.Cryptography;
using ISTD_OFFLINE_CSHARP.helper;

namespace ISTD_OFFLINE_CSHARP.helper
{
    public class QRGeneratorHelper
    {
        public string generateQrCode(
            string sellerName,
            string vatRegistrationNumber,
            string timeStamp,
            string invoiceTotal,
            string vatTotal,
            string hashedXml,
            byte[] publicKey,
            string signature,
            byte[] certificateSignature)
        {
            var berTlvBuilder = new BerTlvBuilder();
            berTlvBuilder.addText(1, sellerName, Encoding.UTF8)
                         .addText(2, vatRegistrationNumber)
                         .addText(3, timeStamp)
                         .addText(4, invoiceTotal)
                         .addText(5, vatTotal)
                         .addText(6, hashedXml)
                         .addBytes(7, Encoding.UTF8.GetBytes(signature))
                         .addBytes(8, publicKey)
                         .addBytes(9, certificateSignature);

            byte[] bytes = berTlvBuilder.buildArray();
            return Convert.ToBase64String(bytes);
        }
    }
}
