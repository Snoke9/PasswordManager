using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Windows;


namespace PasswordManager.Services
{
    public static class Encryption
    {
        private static readonly string key = ConfigurationManager.AppSettings["KEY"]!.PadRight(32);

        public static string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    byte[] combinedData = new byte[aes.IV.Length + encrypted.Length];
                    Array.Copy(aes.IV, 0, combinedData, 0, aes.IV.Length);
                    Array.Copy(encrypted, 0, combinedData, aes.IV.Length, encrypted.Length);

                    return Convert.ToBase64String(combinedData);
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] combinedData = Convert.FromBase64String(encryptedText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);

                byte[] iv = new byte[16];
                byte[] cipherText = new byte[combinedData.Length - iv.Length];

                Array.Copy(combinedData, 0, iv, 0, iv.Length);
                Array.Copy(combinedData, iv.Length, cipherText, 0, cipherText.Length);

                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    try
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                    catch (CryptographicException ex)
                    {
                        MessageBox.Show($"Произошла ошибка дешифровки: {ex.Message}");
                        return null;
                    }
                }
            }
        }
    }
}
