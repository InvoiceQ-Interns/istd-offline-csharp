using System;
using Microsoft.Extensions.Logging;
using ISTD_OFFLINE_CSHARP.loader;
using ISTD_OFFLINE_CSHARP.properties;
using ISTD_OFFLINE_CSHARP.utils;

namespace ISTD_OFFLINE_CSHARP.processor
{
    public abstract class ActionProcessor
    {
        protected readonly ILogger log;
        protected PropertiesManager propertiesManager;

        protected ActionProcessor()
        {
            this.log = LoggingUtils.getLoggerFactory().CreateLogger<ActionProcessor>();
        }

        protected abstract bool loadArgs(string[] args);
        protected abstract bool validateArgs();
        protected abstract bool process();
        protected abstract bool output();

        public bool process(string[] args, PropertiesManager propertiesManager)
        {
            this.propertiesManager = propertiesManager;

            if (!loadArgs(args))
            {
                log.LogError("Failed to load arguments");
                return false;
            }

            if (!validateArgs())
            {
                log.LogError("Invalid arguments");
                return false;
            }

            if (!process())
            {
                log.LogError("Failed to process");
                return false;
            }

            if (!output())
            {
                log.LogError("Failed to output");
                return false;
            }

            return true;
        }
    }
}