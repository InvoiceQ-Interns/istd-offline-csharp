using Microsoft.Extensions.Logging;
using System;
namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;
using System;
public class ComplianceSubmitProcessor : processor.ActionProcessor
{
    public ComplianceSubmitProcessor(ILogger<processor.ActionProcessor> log) : base(log)
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