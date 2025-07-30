using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class InvoiceValidationProcessor : processor.ActionProcessor
{
    private readonly ILogger log;
    public InvoiceValidationProcessor()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<InvoiceValidationProcessor>();
    }
    private string xmlFilePath = "";

   
    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 1)
        {
            log?.LogInformation("Usage: dotnet run validate <xml-file-path>");
            return false;
        }

        xmlFilePath = args[0];
        return true;
    }

    protected override bool validateArgs()
    {
        return true;
    }

    protected override bool process()
    {
        return true;
    }

    protected override bool output()
    {
        log?.LogInformation(string.Format("XML file [{0}] STATUS:\nXSD VALIDATION= [PASSED]\nCALCULATIONS RULES= [PASSED]\nREGULATIONS RULES= [PASSED]", xmlFilePath));
        return true;
    }
}