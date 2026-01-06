using System.Security.Cryptography;
using System.Text;

namespace MyThuatShop.Api.Utils
{
    public class MyUtils
    {
        public static string keyGenerator(int length = 10)
        {
            var value = "absf@kklmihs!jgasj#123giwj123gnajgalsdj521lkfMLGAJ@!123&^#%adsfad1fjLKJFKLANGNAKFJKSKL";
            var sb = new StringBuilder();
            var rd = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(value[rd.Next(0, value.Length)]);
            }
            return sb.ToString();
        }

        public static string ToMd5Hash(string password, string? randomKey)
        {
            using (var md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(string.Concat(password, randomKey)));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}
