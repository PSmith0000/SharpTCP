using System.Security.Cryptography;

namespace SharpTCP.Encryption;

public sealed class AesEncryptionProvider : IEncryptionProvider
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionProvider(byte[] key, byte[] iv)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        _key = key;
        _iv = iv;
    }

    public static (byte[] Key, byte[] IV) GenerateKeyPair()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        return (aes.Key, aes.IV);
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        var input = plaintext.ToArray();
        return encryptor.TransformFinalBlock(input, 0, input.Length);
    }

    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        var input = ciphertext.ToArray();
        return decryptor.TransformFinalBlock(input, 0, input.Length);
    }
}