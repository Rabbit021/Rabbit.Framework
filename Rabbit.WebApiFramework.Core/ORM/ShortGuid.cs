using System;

namespace Rabbit.WebApiFramework.Core.ORM
{
    public class ShortGuid
    {
        public static string NewGuid()
        {
            var guid = Guid.NewGuid();
            return ToShortGuid(guid);
        }

        public static string ToShortGuid(Guid guid)
        {
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            encoded = encoded
                .Replace("/", "_")
                .Replace("+", "-");
            return encoded.Substring(0, 22);
        }

        public static Guid ToGuid(string uid)
        {
            var guid = Guid.Empty;
            if (!string.IsNullOrEmpty(uid) && uid.Trim().Length == 22)
            {
                try
                {
                    string encoded = string.Concat(uid.Trim().Replace("-", "+").Replace("_", "/"), "==");
                    byte[] base64 = Convert.FromBase64String(encoded);
                    guid = new Guid(base64);
                }
                catch
                {
                }
            }
            return guid;
        }
    }
}