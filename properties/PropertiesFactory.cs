using System;
using Microsoft.Extensions.Logging;

using ISTD_OFFLINE_CSHARP.properties.impl;
namespace ISTD_OFFLINE_CSHARP.properties
{
    public class propertiesFactory
    {
        private static ILogger log;

        public static void setLogger(ILoggerFactory loggerFactory)
        {
            log = loggerFactory.CreateLogger("PropertiesFactory");
        }

        public static PropertiesManager getPropertiesManager()
        {
            string env = Environment.GetEnvironmentVariable("env");
            if (string.IsNullOrWhiteSpace(env))
            {
                log?.LogError("env param is missing, please provide env param, -Denv=${env} with allowed values [dev,sim,prod]");
                return null;
            }

            switch (env.Trim().ToLower())
            {
                case "dev":
                    return DevelopmentProperties.getInstance();
                case "sim":
                    return SimulationProperties.getInstance();
                case "prod":
                    return ProdProperties.getInstance();
                default:
                    log?.LogError("Invalid env param [{env}], allowed [dev,sim,prod]", env);
                    return null;
            }
        }
    }
}
