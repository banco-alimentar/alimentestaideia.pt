using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CaptchaMVC.HtmlHelpers
{
    /// <summary>
    /// A class to encrypt text
    /// </summary>
    public static class Encryption
    {
        /// <summary>
        /// Encrypt Text
        /// </summary>
        /// <param name="inputText">The text to encrypt</param>
        /// <param name="password">Password encryption</param>
        /// <param name="salt">Salt</param>
        /// <returns></returns>
        public static string Encrypt(string inputText, string password, byte[] salt)
        {
            var inputBytes = Encoding.Unicode.GetBytes(inputText);

            var pdb = new PasswordDeriveBytes(password, salt);

            using (var ms = new MemoryStream())
            {
                var alg = Rijndael.Create();

                alg.Key = pdb.GetBytes(32);
                alg.IV = pdb.GetBytes(16);

                using (var cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputBytes, 0, inputBytes.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// Deciphering text
        /// </summary>
        /// <param name="inputText">The text for the decryption</param>
        /// <param name="password">Password encryption</param>
        /// <param name="salt">Salt</param>
        /// <returns></returns>
        public static string Decrypt(string inputText, string password, byte[] salt)
        {
            if (inputText == null) { return String.Empty; }
            var inputBytes = Convert.FromBase64String(inputText);

            var pdb = new PasswordDeriveBytes(password, salt);

            using (var ms = new MemoryStream())
            {
                var alg = Rijndael.Create();

                alg.Key = pdb.GetBytes(32);
                alg.IV = pdb.GetBytes(16);

                using (var cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputBytes, 0, inputBytes.Length);
                }

                return Encoding.Unicode.GetString(ms.ToArray());
            }

        }
    }
}
