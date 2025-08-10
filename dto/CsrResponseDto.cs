namespace ISTD_OFFLINE_CSHARP.DTOs;

public class CsrResponseDto
{
    private byte[] csrDer;
    private byte[] privateKeyBytes;

    public CsrResponseDto(byte[] csrDer, byte[] privateKeyBytes)
    {
        this.csrDer = csrDer;
        this.privateKeyBytes = privateKeyBytes;
    }

    public byte[] getCsrDer()
    {
        return csrDer;
    }

    public byte[] getPrivateKeyBytes()
    {
        return privateKeyBytes;

    }
}