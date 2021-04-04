using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Goober.Core.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<string> ReadWithMaxSizeLimitsAsync(this Stream stream, Encoding encoding, int bufferSize = 1024, int maxSize = 1024 * 5)
        {
            if (stream.CanRead == false)
            {
                throw new InvalidOperationException("stream is not ready to read");
            }

            var totalBytesRead = 0;

            var ret = new StringBuilder();

            byte[] buffer = new byte[bufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);
            
            while (bytesRead > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSize)
                {
                    return ret.ToString();
                }

                ret.Append(encoding.GetString(bytes: buffer, index: 0, count: bytesRead));

                bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);
                
            }

            return ret.ToString();
        }
    }
}
