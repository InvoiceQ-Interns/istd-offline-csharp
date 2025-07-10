public class EInvoiceSigningResults
{
    private String invoiceHash;
    private String signature;
    private String qrCode;
    private String signedXml;
    private String invoiceUUID;


    public EInvoiceSigningResults(String invoiceHash, String signature, String qrCode, String signedXml, String invoiceUUID)
    {
        this.invoiceHash = invoiceHash;
        this.signature = signature;
        this.qrCode = qrCode;
        this.signedXml = signedXml;
        this.invoiceUUID = invoiceUUID;
    }

    public String getInvoiceUUID()
    {
        return invoiceUUID;
    }

    public String getInvoiceHash()
    {
        return invoiceHash;
    }

    public void setInvoiceHash(String invoiceHash)
    {
        this.invoiceHash = invoiceHash;
    }

    public String getSignature()
    {
        return signature;
    }

    public void setSignature(String signature)
    {
        this.signature = signature;
    }

    public String getQrCode()
    {
        return qrCode;
    }

    public void setQrCode(String qrCode)
    {
        this.qrCode = qrCode;
    }

    public String getSignedXml()
    {
        return signedXml;
    }

    public void setSignedXml(String signedXml)
    {
        this.signedXml = signedXml;
    }
}