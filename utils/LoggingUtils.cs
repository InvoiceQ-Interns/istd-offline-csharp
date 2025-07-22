using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.utils;

public class LoggingUtils
{
private static ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

public static ILoggerFactory getLoggerFactory()
{
    return loggerFactory;
}
}