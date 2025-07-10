namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class ComplianceInvoiceResponse
    {
        private ValidationResults validationResults;
        private List<InfoMessage> errorMessages;
        private List<InfoMessage> warningMessages;
        private String reportingStatus;
        private String clearanceStatus;

        public ValidationResults getValidationResults()
        {
            return validationResults;
        }

        public void setValidationResults(ValidationResults validationResults)
        {
            this.validationResults = validationResults;
        }

        public List<InfoMessage> getErrorMessages()
        {
            return errorMessages;
        }

        public void setErrorMessages(List<InfoMessage> errorMessages)
        {
            this.errorMessages = errorMessages;
        }

        public List<InfoMessage> getWarningMessages()
        {
            return warningMessages;
        }

        public void setWarningMessages(List<InfoMessage> warningMessages)
        {
            this.warningMessages = warningMessages;
        }

        public String getReportingStatus()
        {
            return reportingStatus;
        }

        public void setReportingStatus(String reportingStatus)
        {
            this.reportingStatus = reportingStatus;
        }

        public String getClearanceStatus()
        {
            return clearanceStatus;
        }

        public void setClearanceStatus(String clearanceStatus)
        {
            this.clearanceStatus = clearanceStatus;
        }

        public bool IsValid()
        {
            return (!string.IsNullOrWhiteSpace(reportingStatus) && string.Equals(reportingStatus, "REPORTED", StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrWhiteSpace(clearanceStatus) && string.Equals(clearanceStatus, "CLEARED", StringComparison.OrdinalIgnoreCase));
        }

    }
}