
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ISTD_OFFLINE_CSHARP.io
{
    public class ReaderHelper
    {
        public static bool isDirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        public static string readFileAsString(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "failed to read file");
                return null;
            }
        }
        public static string readFileFromResource(string resourcePath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string fullName = $"istd_offline_csharp.{normalize(resourcePath)}";
                using Stream stream = assembly.GetManifestResourceStream(fullName);
                if (stream == null) throw new FileNotFoundException($"Resource {fullName} not found");

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "failed to read resource");
                return null;
            }
        }
        private static ILogger logger;

        public static void setLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("ReaderHelper");
        }




        private static string normalize(string name)
        {
            return name.Replace("/", ".").Replace("\\", ".");
        }
    }
}
