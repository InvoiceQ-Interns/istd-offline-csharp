using Newtonsoft.Json;
using System;
using System.IO;

namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class CsrConfigDto
    {
        [JsonProperty("CommonName")]
        private string? enName;

        [JsonProperty("organization")]
        private string? organizationIdentifier;

        [JsonProperty("organizationUnitName")]
        private string? organizationUnitName;

        [JsonProperty("SerialNumber")]
        private string? serialNumber;

        [JsonProperty("Country (ISO2)")]
        private string? country;
        
        [JsonProperty("keySize")]
        private int keySize = 2048;
        
        [JsonProperty("templateOid")]
        private string? templateOid;

        [JsonProperty("major")] 
        private int majorVersion;

        [JsonProperty("minor")] 
        private int minorVersion;

        public string? getOrganizationIdentifier()
        {
            return organizationIdentifier;
        }

        public void setOrganizationIdentifier(string? organizationIdentifier)
        {
            this.organizationIdentifier = organizationIdentifier;
        }

        public string? getOrganizationUnitName()
        {
            return organizationUnitName;
        }

        public void setOrganizationUnitName(string? organizationUnitName)
        {
            this.organizationUnitName = organizationUnitName;
        }

        public string? getCountry()
        {
            return country;
        }

        public void setCountry(string? country)
        {
            this.country = country;
        }

        public string? getEnName()
        {
            return enName;
        }

        public void setEnName(string? enName)
        {
            this.enName = enName;
        }

        public string? getSerialNumber()
        {
            return serialNumber;
        }

        public void setSerialNumber(string? serialNumber)
        {
            this.serialNumber = serialNumber;
        }
        
        public int getKeySize()
        {
            return keySize;
        }

        public void setKeySize(int keySize)
        {
            this.keySize = keySize;
        }

        public string? getTemplateOid()
        {
            return templateOid;
        }

        public void setTemplateOid(string? templateOid)
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

        public string? getSubjectDn()
        {
            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(serialNumber))
            {
                return null;
            }

            // If we have all the new fields, use the full DN format
            if (!string.IsNullOrWhiteSpace(organizationIdentifier) &&
                !string.IsNullOrWhiteSpace(organizationUnitName) &&
                !string.IsNullOrWhiteSpace(country))
            {
                return $"CN={enName.Trim()}, O={organizationIdentifier.Trim()}, OU={organizationUnitName.Trim()}, SerialNumber={serialNumber.Trim()}, C={country.Trim()}";
            }
            
            // Fall back to the original format for backward compatibility
            return $"CN={enName.Trim()}, O=Government of Jordan, OU=eID, SerialNumber={serialNumber.Trim()}, C=JO";
        }

        public void loadStandardConfigFromResources()
        {
            try
            {
                var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "resources", "CSRconfig.json");
                if (!File.Exists(resourcesPath))
                {
                    // Try alternative path for deployed application
                    resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "CSRconfig.json");
                }

                if (File.Exists(resourcesPath))
                {
                    string content = File.ReadAllText(resourcesPath);
                    var standardConfig = JsonConvert.DeserializeObject<CsrConfigDto>(content);
                    
                    if (standardConfig != null)
                    {
                        if (standardConfig.getKeySize() > 0)
                        {
                            this.keySize = standardConfig.getKeySize();
                        }
                        if (!string.IsNullOrWhiteSpace(standardConfig.getTemplateOid()))
                        {
                            this.templateOid = standardConfig.getTemplateOid();
                        }
                        if (standardConfig.getMajorVersion() > 0)
                        {
                            this.majorVersion = standardConfig.getMajorVersion();
                        }
                        if (standardConfig.getMinorVersion() >= 0)
                        {
                            this.minorVersion = standardConfig.getMinorVersion();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Log error but continue - validation will catch missing required values
                Console.WriteLine($"Warning: Could not load standard config from resources: {e.Message}");
            }
        }
    }
}