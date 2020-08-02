using System;
using System.Text;

namespace Goober.Http.Extensions
{
    public static class UdpExtensions
    {
        public static string GetStringFromBytes(this byte[] buffer)
        {
            if (buffer == null)
                return null;

            int nullIdx = Array.IndexOf(buffer, (byte)0);
            nullIdx = nullIdx >= 0 ? nullIdx : buffer.Length;
            var msg = Encoding.UTF8.GetString(buffer, 0, nullIdx);

            return msg;
        }
    }
}
