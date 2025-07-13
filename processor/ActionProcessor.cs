using System;
using Microsoft.Extensions.Logging;
using ISTD_OFFLINE_CSHARP.loader;
using ISTD_OFFLINE_CSHARP.properties;

namespace ISTD_OFFLINE_CSHARP.processor
{
    public abstract class ActionProcessor
    {
        protected readonly ILogger logger;
        protected PropertiesManager propertiesManager;

        protected ActionProcessor(ILogger<ActionProcessor> logger)
        {
            this.logger = logger;
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
                logger.LogError("Failed to load arguments");
                return false;
            }

            if (!validateArgs())
            {
                logger.LogError("Invalid arguments");
                return false;
            }

            if (!process())
            {
                logger.LogError("Failed to process");
                return false;
            }

            if (!output())
            {
                logger.LogError("Failed to output");
                return false;
            }

            return true;
        }
    }
}