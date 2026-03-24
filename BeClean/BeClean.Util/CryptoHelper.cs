using System.Security.Cryptography;
using System.Text;

namespace BeClean.Util
{
    public static class CryptoHelper
    {
        private static string _encryptionKey = string.Empty;

        /// <summary>
        /// Set the environment variable name that holds the AES encryption key
        /// </summary>
        /// <param name="value"></param>
        public static void SetEncryptionKeyEnvVar(string value)
        {
            _encryptionKey = Environment.GetEnvironmentVariable(value) 
                ?? throw new Exception($"Environment variable {value} not set in current context");
        }

        public static void SetEncryptionKey(string key)
        {
            _encryptionKey = key;
        }

        public static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);

                // Convert to hex string
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        private static byte[] GetHashedKey()
        {
            if (string.IsNullOrWhiteSpace(_encryptionKey))
                throw new InvalidOperationException("No encryption key was specified");

            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
            }
        }

        /// <summary>
        /// Encrypt the string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EncodeString(this string value)
        {
            byte[] key = GetHashedKey();

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, iv.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (var sw = new StreamWriter(cs))
                            {
                                sw.Write(value);
                            }
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Decrypt the string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DecodeString(this string value)
        {
            byte[] key = GetHashedKey();
            byte[] cipherTextBytes = Convert.FromBase64String(value);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherTextBytes, 0, iv, 0, iv.Length);

                byte[] actualCipherTextBytes = new byte[cipherTextBytes.Length - iv.Length];
                Array.Copy(cipherTextBytes, iv.Length, actualCipherTextBytes, 0, actualCipherTextBytes.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                {
                    using (var ms = new MemoryStream(actualCipherTextBytes))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}
