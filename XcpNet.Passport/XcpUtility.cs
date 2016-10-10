using System;
using System.Collections.Generic;

namespace XcpNet.Passport
{
    internal static class XcpUtility
    {
        public const string SupplierCookieName = "XCPNET.PASSPORT";

        public static byte[] GetBytes(KeyValuePair<bool, bool> pair)
        {
            return new byte[] { pair.Key ? byte.MaxValue : byte.MinValue, pair.Value ? byte.MaxValue : byte.MinValue };
        }
        public static KeyValuePair<bool, bool> GetValue(byte[] value)
        {
            if (value != null && value.Length == 2)
                return new KeyValuePair<bool, bool>(value[0] == byte.MaxValue, value[1] == byte.MaxValue);
            return new KeyValuePair<bool, bool>(false, false);
        }
    }
}
