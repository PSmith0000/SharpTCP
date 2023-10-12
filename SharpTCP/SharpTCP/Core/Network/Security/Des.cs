using System.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharpTCP.Core.Network.Security {
    public class Des
    {

        public static string GenerateKey()
        {
            var desCrypto = (TripleDESCryptoServiceProvider)TripleDES.Create();
            return Encoding.ASCII.GetString(desCrypto.Key);
        }

        public static MemoryStream Encrypt(MemoryStream ms, string key, byte[] IV)
        {
            // Convert the plaintext to bytes
            byte[] plaintextBytes = ms.ToArray();

            // Create a MemoryStream to hold the encrypted data
            using (MemoryStream encryptedStream = new MemoryStream())
            {
                // Create a TripleDES instance with a random key and IV
                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    // Generate a random key and IV
                    tripleDES.IV = IV;

                    tripleDES.Key = Encoding.Default.GetBytes(key);

                    // Create an encryptor with the key and IV
                    using (ICryptoTransform encryptor = tripleDES.CreateEncryptor())
                    {
                        // Create a CryptoStream to write the encrypted data to the MemoryStream
                        using (CryptoStream cryptoStream = new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write))
                        {
                            // Write the plaintext data to the CryptoStream
                            cryptoStream.Write(plaintextBytes, 0, plaintextBytes.Length);
                            cryptoStream.FlushFinalBlock();
                        }
                    }
                }

                // Get the encrypted data from the MemoryStream
                byte[] encryptedBytes = encryptedStream.ToArray();

                // Print the encrypted data (convert to Base64 for easy display)
                string encryptedText = Convert.ToBase64String(encryptedBytes);

                return encryptedStream;
            }
        }

        public static MemoryStream Decrypt(MemoryStream encryptedBytes, string Key, byte[] IV)
        {
            // Create a MemoryStream to hold the decrypted data
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                // Create a TripleDES instance with the same key and IV
                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider())
                {
                    // Create a decryptor with the key and IV
                    using (ICryptoTransform decryptor = tripleDES.CreateDecryptor(Encoding.Default.GetBytes(Key), IV))
                    {
                        // Create a CryptoStream to read the encrypted data from the input byte array
                        using (CryptoStream cryptoStream = new CryptoStream((encryptedBytes), decryptor, CryptoStreamMode.Read))
                        {
                            // Read the decrypted data from the CryptoStream and write it to the MemoryStream
                            cryptoStream.CopyTo(decryptedStream);
                        }
                    }
                }
                // Convert the decrypted bytes to a string
                string decryptedText = Encoding.UTF8.GetString(decryptedStream.ToArray());
                return decryptedStream;
            }
        }

    }

}