namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class CsrConfigDto
    {
        private String commonName;
        private String serialNumber;
        private String organizationIdentifier;
        private String organizationUnitName;
        private String organizationName;
        private String countryName;
        private String invoiceType;
        private String location;
        private String industry;

        public String getCommonName()
        {
            return commonName;
        }

        public void setCommonName(String commonName)
        {
            this.commonName = commonName;
        }

        public String getSerialNumber()
        {
            return serialNumber;
        }

        public void setSerialNumber(String serialNumber)
        {
            this.serialNumber = serialNumber;
        }

        public String getOrganizationIdentifier()
        {
            return organizationIdentifier;
        }

        public void setOrganizationIdentifier(String organizationIdentifier)
        {
            this.organizationIdentifier = organizationIdentifier;
        }

        public String getOrganizationUnitName()
        {
            return organizationUnitName;
        }

        public void setOrganizationUnitName(String organizationUnitName)
        {
            this.organizationUnitName = organizationUnitName;
        }

        public String getOrganizationName()
        {
            return organizationName;
        }

        public void setOrganizationName(String organizationName)
        {
            this.organizationName = organizationName;
        }

        public String getCountryName()
        {
            return countryName;
        }

        public void setCountryName(String countryName)
        {
            this.countryName = countryName;
        }

        public String getInvoiceType()
        {
            return invoiceType;
        }

        public void setInvoiceType(String invoiceType)
        {
            this.invoiceType = invoiceType;
        }

        public String getLocation()
        {
            return location;
        }

        public void setLocation(String location)
        {
            this.location = location;
        }

        public String getIndustry()
        {
            return industry;
        }

        public void setIndustry(String industry)
        {
            this.industry = industry;
        }
    }
}