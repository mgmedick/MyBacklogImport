using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace GameStatsAppImport.Common.Extensions
{
    public static class StringExtensions
    {
        public static string GetHMACSHA256Hash(this string plaintext, string salt)
        {
            string result = string.Empty;
            var enc = Encoding.Default;
            byte[]
            baText2BeHashed = enc.GetBytes(plaintext),
            baSalt = enc.GetBytes(salt);
            System.Security.Cryptography.HMACSHA256 hasher = new HMACSHA256(baSalt);
            byte[] baHashedText = hasher.ComputeHash(baText2BeHashed);
            result = string.Join(string.Empty, baHashedText.ToList().Select(b => b.ToString("x2")).ToArray());
            return result;
        }
    }
}

