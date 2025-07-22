using ISTD_OFFLINE_CSHARP.io;
using ISTD_OFFLINE_CSHARP.security;
using ISTD_OFFLINE_CSHARP.utils;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.ActionProcessor.impl;

public class DecryptProcess : processor.ActionProcessor
{
    private readonly ILogger log;
    public DecryptProcess()
    {
        this.log = LoggingUtils.getLoggerFactory().CreateLogger<DecryptProcess>();
    }
    
    private string encryptedFilePath;
    private string encryptedFile = "";
    private string decryptedFile = "";

    protected override bool loadArgs(string[] args)
    {
        if (args.Length != 1)
        {
            log?.LogInformation("Usage: dotnet run decrypt <encrypted-file-path>");
            return false;
        }
        encryptedFilePath = args[0];
        return true;
    }

    protected override bool validateArgs()
    {
        encryptedFile = ReaderHelper.readFileAsString(encryptedFilePath);
        return !string.IsNullOrWhiteSpace(encryptedFile);
    }

    protected override bool process()
    {
        decryptedFile = SecurityUtils.decrypt(encryptedFile);
        return !string.IsNullOrWhiteSpace(decryptedFile);
    }

    protected override bool output()
    {
        log?.LogInformation("Decrypted file: " + decryptedFile);
        return true;
    }
}