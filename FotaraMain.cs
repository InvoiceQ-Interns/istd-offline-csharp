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
        private readonly ILogger<FotaraMain> log;

        public FotaraMain(ILogger<FotaraMain> log)
        {
            this.log = log;
        }

        public static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var log = loggerFactory.CreateLogger<FotaraMain>();
            var fotaraMain = new FotaraMain(log);
            fotaraMain.execute(args, loggerFactory);
        }

        private void execute(string[] args, ILoggerFactory loggerFactory)
        {
            if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                log.LogError("Usage: dotnet run <action> <args>");
                return;
            }

            string action = args[0];
            
            PropertiesManager propertiesManager = propertiesFactory.getPropertiesManager();

           if (propertiesManager == null)
           {
               log.LogError("Failed to load properties manager.");
               return;
           }

            string[] parameters = args.Skip(1).ToArray();

            ActionProcessor processor = InputResolver.resolve(action, loggerFactory);
            if (processor == null)
            {
                log.LogError("Invalid Action");
                return;
            }

            bool result = processor.process(parameters, propertiesManager);
            if (!result)
            {
                log.LogError("Action processing failed.");
            }
        }
    }
}