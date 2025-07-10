namespace ISTD_OFFLINE_CSHARP.DTOs
{
    public class ValidationResults
    {
        private InfoMessage infoMessages;
        public InfoMessage getInfoMessage()
        {
            return infoMessages;
        }

        public void setInfoMessage(InfoMessage infoMessage)
        {
            this.infoMessages = infoMessage;
        }
    }
}