using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Goober.Core.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<(bool IsReadToTheEnd, StringBuilder StringResult)> ReadStreamWithMaxSizeRetrictionAsync(this Stream stream,
            Encoding encoding,
            long maxSize,
            int bufferSize = 1024)
        {
            if (stream.CanRead == false)
            {
                throw new InvalidOperationException("stream is not ready to ready");
            }

            var totalBytesRead = 0;

            var sbResult = new StringBuilder();

            byte[] buffer = new byte[bufferSize];
            var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

            while (bytesRead > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSize)
                {
                    return (false, sbResult);
                }

                sbResult.Append(encoding.GetString(bytes: buffer, index: 0, count: bytesRead));

                bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);
            }

            return (true, sbResult);
        }
    }
}
