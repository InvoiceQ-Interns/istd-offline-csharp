using ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.resolvers;

public class InputResolver
{
    private static ILogger log;

    public static void configureLogger(ILogger<InputResolver> logInstance)
    {
        log = logInstance;
    }

    public static processor.ActionProcessor resolve(string action, ILoggerFactory loggerFactory)
{
    switch (action)
    {
        case "generate-csr-keys":
            return new CsrKeysProcessor(loggerFactory.CreateLogger<CsrKeysProcessor>());

        case "onboard":
            return new OnboardProcessor(loggerFactory.CreateLogger<OnboardProcessor>());

        case "validate":
            return new InvoiceValidationProcessor(loggerFactory.CreateLogger<InvoiceValidationProcessor>());

        case "sign":
            return new InvoiceSignProcessor(loggerFactory.CreateLogger<InvoiceSignProcessor>());

        case "generate-qr":
            return new QrGeneratorProcessor(loggerFactory.CreateLogger<QrGeneratorProcessor>());

        case "submit-clearance":
            return new InvoiceSubmitProcessor(loggerFactory.CreateLogger<InvoiceSubmitProcessor>());

        case "submit-report":
            return new ReportSubmitProcessor(loggerFactory.CreateLogger<ReportSubmitProcessor>());

        case "compliance-invoice":
            return new ComplianceSubmitProcessor(loggerFactory.CreateLogger<ComplianceSubmitProcessor>());

        case "decrypt":
            return new DecryptProcess(loggerFactory.CreateLogger<DecryptProcess>());

        default:
            var log = loggerFactory.CreateLogger("ActionResolver");
            log.LogError(
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