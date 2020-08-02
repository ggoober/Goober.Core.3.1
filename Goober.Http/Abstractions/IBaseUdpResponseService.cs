using System.Net;
using System.Threading.Tasks;

namespace Goober.Http.Abstractions
{
    public interface IBaseUdpResponseService
    {
        Task<byte[]> OnReceivedAsync(IPEndPoint localEndPoint, byte[] msgBuffer, IPEndPoint senderEndPoint);
    }
}
