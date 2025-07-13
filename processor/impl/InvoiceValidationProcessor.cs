using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class InvoiceValidationProcessor : processor.ActionProcessor
{
    public InvoiceValidationProcessor(ILogger<processor.ActionProcessor> log) : base(log)
    {
        
    }
    
    protected override bool loadArgs(string[] args)
    {
        Console.WriteLine("im here");
        return true;
    }

    protected override bool validateArgs()
    {
        Console.WriteLine("im here");
        return true;
    }

    protected override bool process()
    {
        Console.WriteLine("im here");
        return true;
    }

    protected override bool output()
    {
        Console.WriteLine("im here");
        return true;
    }
}