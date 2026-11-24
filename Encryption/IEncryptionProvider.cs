namespace SharpTCP.Encryption;

public interface IEncryptionProvider
{
    byte[] Encrypt(ReadOnlySpan<byte> plaintext);

    byte[] Decrypt(ReadOnlySpan<byte> ciphertext);
}