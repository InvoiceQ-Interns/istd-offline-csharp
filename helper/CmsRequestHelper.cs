using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Formats.Asn1;
using System.Text;
using ISTD_OFFLINE_CSHARP.DTOs;

namespace ISTD_OFFLINE_CSHARP.Helper
{
    public static class CmsRequestHelper
    {
        public static CsrResponseDto createCsr(CsrConfigDto config)
        {
            if (string.IsNullOrWhiteSpace(config.getKeyPassword()))
            {
                throw new ArgumentException("Password must not be null or empty.");
            }

            using RSA rsa = RSA.Create(config.getKeySize());

            var certReq = new CertificateRequest(
                config.getSubjectDn(),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            addSubjectKeyIdentifier(certReq, rsa);

            if (!string.IsNullOrEmpty(config.getTemplateOid()))
            {
                addCertificateTemplateExtension(certReq, config.getTemplateOid(),
                    config.getMajorVersion(), config.getMinorVersion());
            }

            byte[] pkcs10Der = certReq.CreateSigningRequest();

            byte[] privateKeyBytes = exportEncryptedPkcs8PrivateKey(rsa, config.getKeyPassword());

            return new CsrResponseDto(pkcs10Der, privateKeyBytes);
        }

        private static void addSubjectKeyIdentifier(CertificateRequest certReq, RSA rsa)
        {
            byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            byte[] skiBytes = SHA1.HashData(publicKeyBytes);

            AsnWriter skiWriter = new AsnWriter(AsnEncodingRules.DER);
            skiWriter.WriteOctetString(skiBytes);
            var skiExtension = new X509Extension("2.5.29.14", skiWriter.Encode(), false);
            certReq.CertificateExtensions.Add(skiExtension);
        }

        private static void addCertificateTemplateExtension(CertificateRequest certReq, string oid,
            int majorVersion, int minorVersion)
        {
            byte[] templateExtension = buildCertificateTemplateExtension(oid, majorVersion, minorVersion);
            certReq.CertificateExtensions.Add(new X509Extension("1.3.6.1.4.1.311.21.7", templateExtension, false));
        }

        private static byte[] buildCertificateTemplateExtension(string oid, int majorVersion, int minorVersion)
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteObjectIdentifier(oid);
            writer.WriteInteger(majorVersion);
            writer.WriteInteger(minorVersion);
            writer.PopSequence();
            return writer.Encode();
        }

        private static byte[] exportEncryptedPkcs8PrivateKey(RSA rsa, string password)
        {
            try
            {
                return rsa.ExportEncryptedPkcs8PrivateKey(
                    password,
                    new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000));
            }
            catch (Exception e)
            {
                throw new Exception("Failed to encrypt private key: " + e.Message, e);
            }
        }
    }
}
