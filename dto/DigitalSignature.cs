namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class DigitalSignature
    {
        private String digitalSignature;
        private byte[] xmlHashing;

        public DigitalSignature(String digitalSignature, byte[] xmlHashing)
        {
            this.digitalSignature = digitalSignature;
            this.xmlHashing = xmlHashing;
        }

        public String getDigitalSignature()
        {
            return digitalSignature;
        }

        public byte[] getXmlHashing()
        {
            return xmlHashing;
        }
    }
}