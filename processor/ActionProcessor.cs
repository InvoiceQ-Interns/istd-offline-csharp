using System;
using Microsoft.Extensions.Logging;
using ISTD_OFFLINE_CSHARP.loader;
using ISTD_OFFLINE_CSHARP.properties;

namespace ISTD_OFFLINE_CSHARP.processor
{
    public abstract class ActionProcessor
    {
        protected readonly ILogger Log;
        protected PropertiesManager propertiesManager;

        protected ActionProcessor(ILogger<ActionProcessor> log)
        {
            this.Log = log;
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
                Log.LogError("Failed to load arguments");
                return false;
            }

            if (!validateArgs())
            {
                Log.LogError("Invalid arguments");
                return false;
            }

            if (!process())
            {
                Log.LogError("Failed to process");
                return false;
            }

            if (!output())
            {
                Log.LogError("Failed to output");
                return false;
            }

            return true;
        }
    }
}