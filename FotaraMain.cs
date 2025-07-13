using System;
using System.Linq;
using ISTD_OFFLINE_CSHARP.processor;
using ISTD_OFFLINE_CSHARP.properties;
using ISTD_OFFLINE_CSHARP.resolvers;
using Microsoft.Extensions.Logging;


namespace fotara
{
    public class FotaraMain
    {
        private readonly ILogger<FotaraMain> logger;

        public FotaraMain(ILogger<FotaraMain> logger)
        {
            this.logger = logger;
        }

        public static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger<FotaraMain>();
            var fotaraMain = new FotaraMain(logger);
            fotaraMain.execute(args, loggerFactory);
        }

        private void execute(string[] args, ILoggerFactory loggerFactory)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                logger.LogError("Usage: dotnet run <action> <args>");
                return;
            }

            string action = args[0];
            
            PropertiesManager propertiesManager = propertiesFactory.getPropertiesManager();

           // if (propertiesManager == null)
           // {
           //logger.LogError("Failed to load properties manager.");
           //     return;
           // }

            string[] parameters = args.Skip(1).ToArray();

            ActionProcessor processor = InputResolver.resolve(action, loggerFactory);
            if (processor == null)
            {
                logger.LogError("Invalid Action");
                return;
            }

            bool result = processor.process(parameters, propertiesManager);
            if (!result)
            {
                logger.LogError("Action processing failed.");
            }
        }
    }
}