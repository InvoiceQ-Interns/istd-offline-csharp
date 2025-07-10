namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class EInvoiceResponse
    {
        private String invoiceHash;
        private String status;
        private String clearedInvoice;
        private List<InfoMessage> warnings;
        private List<InfoMessage> errors;

        public String getInvoiceHash()
        {
            return invoiceHash;
        }

        public void setInvoiceHash(String invoiceHash)
        {
            this.invoiceHash = invoiceHash;
        }

        public String getStatus()
        {
            return status;
        }

        public void setStatus(String status)
        {
            this.status = status;
        }

        public String getClearedInvoice()
        {
            return clearedInvoice;
        }

        public void setClearedInvoice(String clearedInvoice)
        {
            this.clearedInvoice = clearedInvoice;
        }

        public List<InfoMessage> getWarnings()
        {
            return warnings;
        }

        public void setWarnings(List<InfoMessage> warnings)
        {
            this.warnings = warnings;
        }

        public List<InfoMessage> getErrors()
        {
            return errors;
        }

        public void setErrors(List<InfoMessage> errors)
        {
            this.errors = errors;
        }
    }

}