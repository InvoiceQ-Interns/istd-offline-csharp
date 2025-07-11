
using System.Reflection;
using System.Text;
using System.Xml;

namespace ISTD_OFFLINE_CSHARP.loader
{
    public class AppResourceLoader
    {
        private readonly Assembly assembly;
        private readonly string baseNamespace = "ISTD_OFFLINE_CSHARP.loader";

        public AppResourceLoader()
        {
            assembly = Assembly.GetExecutingAssembly();
        }

        public XmlReader getStreamResource(string fileName)
        {
            string fullName = $"{baseNamespace}.{normalize(fileName)}";
            Stream stream = assembly.GetManifestResourceStream(fullName);

            if (stream == null)
            {
                stream = readStreamResourceUsingLoader(fileName);
            }

            return XmlReader.Create(stream);
        }



        private StreamReader readInputStreamReaderUsingLoader(string fileName)
        {
            string fullName = $"{baseNamespace}.{normalize(fileName)}";
            Stream stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{fullName}' not found.");
            return new StreamReader(stream, Encoding.UTF8);
        }
        public StreamReader getInputStreamReader(string fileName)
        {
            try
            {
                string fullName = $"{baseNamespace}.{normalize(fileName)}";
                Stream stream = assembly.GetManifestResourceStream(fullName);
                if (stream == null) throw new Exception();
                return new StreamReader(stream, Encoding.UTF8);
            }
            catch
            {
                return readInputStreamReaderUsingLoader(fileName);
            }
        }

        private string normalize(string name)
        {
            return name.Replace("/", ".").Replace("\\", ".");
        }
        private Stream readStreamResourceUsingLoader(string fileName)
        {
            string fullName = $"{baseNamespace}.{normalize(fileName)}";
            Stream stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{fullName}' not found.");
            return stream;
        }
    }
}
