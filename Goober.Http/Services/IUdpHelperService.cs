using Goober.Http.Abstractions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Goober.Http.Services
{
    public interface IUdpHelperService
    {
        IPEndPoint Bind(int minPort, int maxPort);
        void Receive(IPEndPoint localEndPoint, Func<IPEndPoint, byte[], IPEndPoint, Task<byte[]>> onReceiveFuncAsync, int? receiveTimeoutInMilliseconds = null);

        void Send(IPEndPoint localEndPoint, byte[] msgBuffer, IPEndPoint destinationEndPoint);

        void StartReceiving<TUdpResponseService>(IPEndPoint localEndPoint, int? receiveTimeoutInMilliseconds = null) where TUdpResponseService : IBaseUdpResponseService;

        bool StopReceiving(IPEndPoint localEndPoint, int timeoutInMilliseconds = 1000);

        void UnBind(IPEndPoint localEndPoint);

        bool IsEndPointBinded(IPEndPoint localEndPoint);

        bool IsReceiving(IPEndPoint localEndPoint);
    }
}