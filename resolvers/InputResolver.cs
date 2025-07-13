using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.resolvers;

public class InputResolver
{
    private static ILogger logger;

    public static void configureLogger(ILogger<InputResolver> logInstance)
    {
        logger = logInstance;
    }

    public static processor.ActionProcessor resolve(string action)
    {
        switch (action)
        {
            case "generate-csr-keys":
                return new CsrKeysProcessor();
            case "onboard":
                return new OnboardProcessor();
            case "validate":
                return new InvoiceValidationProcessor();
            case "sign":
                return new InvoiceSignProcessor();
            case "generate-qr":
                return new QrGeneratorProcessor();
            case "submit-clearance":
                return new InvoiceSubmitProcessor();
            case "submit-report":
                return new ReportSubmitProcessor();
            case "compliance-invoice":
                return new ComplianceSubmitProcessor();
            case "decrypt":
                return new DecryptProcess();
            default:
                logger?.LogError(
                    "Invalid action, allowed actions are:\n" +
                    "1-generate-csr-keys: to Generate CSR and Key Pairs\n" +
                    "2-onboard: to onboard a generated csr\n" +
                    "3-validate: to validate Invoice\n" +
                    "4-sign: to sign Invoice\n" +
                    "5-generate-qr: to generate QR code\n" +
                    "6-submit-clearance: to submit Invoice to Fotara\n" +
                    "7-submit-report: to submit Invoice to Fotara\n" +
                    "8-compliance-invoice: to submit Invoice to Fotara\n" +
                    "9-decrypt: to decrypt file\n"
                );
                return null;
        }
    }
}