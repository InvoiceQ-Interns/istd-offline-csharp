using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class CsrKeysProcessor : processor.ActionProcessor
{
    public CsrKeysProcessor(ILogger<processor.ActionProcessor> log) : base(log)
    {
       
    }
    
    protected override bool loadArgs(string[] args)
    {
        Console.WriteLine("im here");
        return true;
    }

    protected override bool validateArgs()
    {Console.WriteLine("im here");
        return true;throw new NotImplementedException();
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