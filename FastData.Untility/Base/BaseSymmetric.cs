using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace FastUntility.Base
{

public static class BaseSymmetric
    {
        private static string key = "Guz(%&hj7x89H$yuBI012345maT5&fvHUFCy76*h%(HilJ$lhj!y6&(*jkP~!@#$";
        private static string p_strKey = "Weizz_2015";

        private static byte[] GetLegalKey(Aes aes)
        {
            string sTemp = key;
            aes.GenerateKey();
            byte[] bytTemp = aes.Key;
            int KeyLength = bytTemp.Length;
            if (sTemp.Length > KeyLength)
                sTemp = sTemp.Substring(0, KeyLength);
            else if (sTemp.Length < KeyLength)
                sTemp = sTemp.PadRight(KeyLength, ' ');
            return ASCIIEncoding.ASCII.GetBytes(sTemp);
        }

        private static byte[] GetLegalIV(Aes aes)
        {
            string sTemp = "E4ghj*Ghg7!rNIfb&95GUY86GfghUb#er57HBh(u%g6HJ($jhWk7&!~!@#$%^&*(";
            aes.GenerateIV();
            byte[] bytTemp = aes.IV;
            int IVLength = bytTemp.Length;
            if (sTemp.Length > IVLength)
                sTemp = sTemp.Substring(0, IVLength);
            else if (sTemp.Length < IVLength)
                sTemp = sTemp.PadRight(IVLength, ' ');
            return ASCIIEncoding.ASCII.GetBytes(sTemp);
        }

        /// <summary>
        /// 加密方法
        /// </summary>
        public static string Encrypto(string Source)
        {
            byte[] bytIn = UTF8Encoding.UTF8.GetBytes(Source);
            using (var aes = Aes.Create())
            {
                aes.Key = GetLegalKey(aes);
                aes.IV = GetLegalIV(aes);
                using (ICryptoTransform encrypto = aes.CreateEncryptor())
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Write))
                    {
                        cs.Write(bytIn, 0, bytIn.Length);
                        cs.FlushFinalBlock();
                        byte[] bytOut = ms.ToArray();
                        return Convert.ToBase64String(bytOut);
                    }
                }
            }
        }

        /// <summary>
        /// 解密方法
        /// </summary>
        public static string Decrypto(string Source, string refValue = "")
        {
            try
            {
                byte[] bytIn = Convert.FromBase64String(Source);
                using (var aes = Aes.Create())
                using (MemoryStream ms = new MemoryStream(bytIn, 0, bytIn.Length))
                {
                    aes.Key = GetLegalKey(aes);
                    aes.IV = GetLegalIV(aes);
                    using (ICryptoTransform encrypto = aes.CreateDecryptor())
                    using (CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return refValue;
            }
        }

        [Obsolete("DES 算法已不安全，请使用 AES (Encrypto/Decrypto)")]
        public static string EncodeGB2312(string Source)
        {
#pragma warning disable SYSLIB0021
            using (DESCryptoServiceProvider provider = new DESCryptoServiceProvider())
#pragma warning restore SYSLIB0021
            {
                provider.Key = Encoding.ASCII.GetBytes(p_strKey.Substring(0, 8));
                provider.IV = Encoding.ASCII.GetBytes(p_strKey.Substring(0, 8));
                byte[] bytes = Encoding.GetEncoding("GB2312").GetBytes(Source);
                using (MemoryStream stream = new MemoryStream())
                {
                    using (CryptoStream stream2 = new CryptoStream(stream, provider.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        stream2.Write(bytes, 0, bytes.Length);
                        stream2.FlushFinalBlock();
                        StringBuilder builder = new StringBuilder();
                        foreach (byte num in stream.ToArray())
                        {
                            builder.AppendFormat("{0:X2}", num);
                        }
                        return builder.ToString();
                    }
                }
            }
        }

        [Obsolete("DES 算法已不安全，请使用 AES (Encrypto/Decrypto)")]
        public static string DecodeGB2312(string Source, string refValue = "")
        {
            try
            {
#pragma warning disable SYSLIB0021
                using (DESCryptoServiceProvider provider = new DESCryptoServiceProvider())
#pragma warning restore SYSLIB0021
                {
                    provider.Key = Encoding.ASCII.GetBytes(p_strKey.Substring(0, 8));
                    provider.IV = Encoding.ASCII.GetBytes(p_strKey.Substring(0, 8));
                    byte[] buffer = new byte[Source.Length / 2];
                    for (int i = 0; i < (Source.Length / 2); i++)
                    {
                        int num2 = Convert.ToInt32(Source.Substring(i * 2, 2), 0x10);
                        buffer[i] = (byte)num2;
                    }
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (CryptoStream stream2 = new CryptoStream(stream, provider.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            stream2.Write(buffer, 0, buffer.Length);
                            stream2.FlushFinalBlock();
                            return Encoding.GetEncoding("GB2312").GetString(stream.ToArray());
                        }
                    }
                }
            }
            catch
            {
                return refValue;
            }
}

        #region MD5加密
        /// <summary>
        /// 标签：2015.7.13，魏中针
        /// 说明：MD5加密
        /// </summary>
        /// <param name="code">md5加密位数，16位或32位</param>
        /// <param name="Source">要加密的字符串</param>
        /// <returns></returns>
        public static string md5(int code, string Source)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(Source);
                var hashBytes = md5.ComputeHash(inputBytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                var hash = sb.ToString();
                if (code == 16)
                {
                    return hash.Substring(8, 16);
                }
                return hash;
            }
        }
        #endregion

        #region  Generate 根据值获取经过MD5加密的数据
        /// <summary>
        /// Generate 根据值获取经过MD5加密的数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Generate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var md5 = MD5.Create();
            var s = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            for (int i = 0; i < s.Length; i++)
                sb.Append(s[i].ToString("x2"));

            return sb.ToString();
        }
        #endregion
    }
}
