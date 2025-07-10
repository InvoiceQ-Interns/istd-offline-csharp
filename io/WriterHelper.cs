
using System.Text;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.io
{
    public class WriterHelper
    {
        private static ILogger logger;

        public static void setLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("WriterHelper");
        }

        public static bool writeFile(string filePath, string content)
        {
            try
            {
                File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(content));
            }
            catch (Exception e)
            {
                logger?.LogError(e, "failed to write file on path");
                return false;
            }
            return true;
        }
    }
}