namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class CertificateResponse
    {
        public long requestID;
        public string dispositionMessage;
        public string binarySecurityToken;
        public string secret;
        public List<ResponseMessage> errors;

        public long getRequestID()
        {
            return requestID;
        }

        public void setRequestID(long requestID)
        {
            this.requestID = requestID;
        }

        public String getDispositionMessage()
        {
            return dispositionMessage;
        }

        public void setDispositionMessage(String dispositionMessage)
        {
            this.dispositionMessage = dispositionMessage;
        }

        public String getBinarySecurityToken()
        {
            return binarySecurityToken;
        }

        public void setBinarySecurityToken(String binarySecurityToken)
        {
            this.binarySecurityToken = binarySecurityToken;
        }

        public String getSecret()
        {
            return secret;
        }

        public void setSecret(String secret)
        {
            this.secret = secret;
        }

        public List<ResponseMessage> getErrors()
        {
            return errors;
        }

        public void setErrors(List<ResponseMessage> errors)
        {
            this.errors = errors;
        }


    }
}
