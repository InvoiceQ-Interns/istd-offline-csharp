using Newtonsoft.Json;

namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class CsrConfigDto
    {

        private string enName;

        private string serialNumber;
        
        private string keyPassword;
        
        [JsonProperty("keySize")]
        private int keySize = 2048;
        
        [JsonProperty("templateOid")]
        private string templateOid;

        [JsonProperty("major")] 
        private int majorVersion;

        [JsonProperty("minor")] 
        private int minorVersion;

        public CsrConfigDto() { }

        public string getEnName()
        {
            return enName;
        }

        public void setEnName(string enName)
        {
            this.enName = enName;
        }

        public string getSerialNumber()
        {
            return serialNumber;
        }

        public void setSerialNumber(string serialNumber)
        {
            this.serialNumber = serialNumber;
        }

        public string getKeyPassword()
        {
            return keyPassword;
        }

        public void setKeyPassword(string keyPassword)
        {
            this.keyPassword = keyPassword;
        }

        public int getKeySize()
        {
            return keySize;
        }

        public void setKeySize(int keySize)
        {
            this.keySize = keySize;
        }

        public string getTemplateOid()
        {
            return templateOid;
        }

        public void setTemplateOid(string templateOid)
        {
            this.templateOid = templateOid;
        }

        public int getMajorVersion()
        {
            return majorVersion;
        }

        public void setMajorVersion(int majorVersion)
        {
            this.majorVersion = majorVersion;
        }

        public int getMinorVersion()
        {
            return minorVersion;
        }

        public void setMinorVersion(int minorVersion)
        {
            this.minorVersion = minorVersion;
        }

        public string getPassword()
        {
            return keyPassword;
        }

        public string getSubjectDn()
        {
            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(serialNumber))
            {
                return null;
            }

            return $"CN={enName.Trim()}, O=Government of Jordan, OU=eID, SerialNumber={serialNumber.Trim()}, C=JO";
        }
    }
}