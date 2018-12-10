using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ABCat.Shared
{
    public static class Crypt
    {
        [DebuggerNonUserCode]
        public static string Decrypt(this string str, string keyCrypt)
        {
            string result;
            try
            {
                var cs = InternalDecrypt(Convert.FromBase64String(str), keyCrypt);
                var sr = new StreamReader(cs);

                result = sr.ReadToEnd();

                cs.Close();
                cs.Dispose();

                sr.Close();
                sr.Dispose();
            }
            catch (CryptographicException)
            {
                return null;
            }

            return result;
        }

        public static string Encrypt(this string str, string keyCrypt)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(str), keyCrypt));
        }

        private static byte[] Encrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            var ct = sa.CreateEncryptor(new PasswordDeriveBytes(value, null).GetBytes(16), new byte[16]);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);

            cs.Write(key, 0, key.Length);
            cs.FlushFinalBlock();

            var result = ms.ToArray();

            ms.Close();
            ms.Dispose();

            cs.Close();
            cs.Dispose();

            ct.Dispose();

            return result;
        }

        private static CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            var ct = sa.CreateDecryptor(new PasswordDeriveBytes(value, null).GetBytes(16), new byte[16]);

            var ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }
    }
}