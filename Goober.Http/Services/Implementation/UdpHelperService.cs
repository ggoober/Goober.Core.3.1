using Goober.Http.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Goober.Http.Services.Implementation
{
    class UdpHelperService : IUdpHelperService
    {
        class UdpClientModel
        {
            public Task<UdpReceiveResult> ReceiveTask { get; set; }

            public UdpClient UdpClient { get; set; }

            public CancellationTokenSource StoppingTokenSource { get; set; }
        }

        private readonly ConcurrentDictionary<IPEndPoint, UdpClientModel> _udpModels = new ConcurrentDictionary<IPEndPoint, UdpClientModel>();

        private readonly ILogger<UdpHelperService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public UdpHelperService(ILogger<UdpHelperService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _serviceScopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        #region IUdpHelperService

        public IPEndPoint Bind(int minPort, int maxPort)
        {
            var udpClient = CreateUdpClient(minPort, maxPort);

            (IPEndPoint LocalEndPoint, UdpClientModel UdpModel) createResult;
            try
            {
                createResult = AddUdpClientToDictionary(udpClient);
            }
            catch (Exception)
            {
                udpClient.Close();
                throw;
            }

            return createResult.LocalEndPoint;
        }

        public void StartReceiving<TUdpResponseService>(IPEndPoint localEndPoint, int? receiveTimeoutInMilliseconds = null) where TUdpResponseService : IBaseUdpResponseService
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            var cancellationToken = udpModel.StoppingTokenSource.Token;
            Action<Task> repeatAction = null;
            repeatAction = _ignored1 =>
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetRequiredService<TUdpResponseService>() as IBaseUdpResponseService;
                        if (service == null)
                            throw new InvalidOperationException($"Can't resolve service {typeof(IBaseUdpResponseService).Name}");

                        ProcessRecieve(udpModel, service.OnReceivedAsync, receiveTimeoutInMilliseconds);
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogError(message: $"Error while receiving {udpModel.UdpClient.Client.LocalEndPoint as IPEndPoint}", exception: exc);
                }

                Task.Delay(0, cancellationToken)
                    .ContinueWith(_ignored2 => repeatAction(_ignored2), cancellationToken);
            };

            Task.Delay(0, cancellationToken)
                .ContinueWith(continuationAction: repeatAction, cancellationToken: cancellationToken);
        }

        public void Receive(IPEndPoint localEndPoint, Func<IPEndPoint, byte[], IPEndPoint, Task<byte[]>> onReceiveFuncAsync, int? receiveTimeoutInMilliseconds = null)
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            ProcessRecieve(udpModel, onReceiveFuncAsync, receiveTimeoutInMilliseconds);
        }

        public void Send(IPEndPoint localEndPoint, byte[] msgBuffer, IPEndPoint destinationEndPoint)
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            udpModel.UdpClient.Send(msgBuffer, msgBuffer.Length, destinationEndPoint);
        }

        public void UnBind(IPEndPoint localEndPoint)
        {
            if (_udpModels.ContainsKey(localEndPoint) == false)
                throw new InvalidOperationException($"TerminateReceiver: can't find udpModel by endPoint = {localEndPoint}");

            var isRemoved = _udpModels.TryRemove(localEndPoint, out UdpClientModel udpModel);
            if (isRemoved == false)
            {
                throw new InvalidOperationException($"TerminateReceiver: can't remove udpModel by endPoint = {localEndPoint}");
            }

            StopReceiving(localEndPoint);

            udpModel.UdpClient.Close();
        }

        public bool StopReceiving(IPEndPoint localEndPoint, int timeoutInMilliseconds = 1000)
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            var ret = StopReceivingInternal(udpModel, timeoutInMilliseconds);

            return ret;
        }

        public bool IsEndPointBinded(IPEndPoint localEndPoint)
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            return udpModel != null;
        }

        public bool IsReceiving(IPEndPoint localEndPoint)
        {
            var udpModel = GetUdpClientModel(localEndPoint);

            if (udpModel == null)
                return false;

            if (udpModel.ReceiveTask == null)
                return false;

            var status = udpModel.ReceiveTask.Status;
            if (status != TaskStatus.Canceled
                && status != TaskStatus.Faulted
                && status != TaskStatus.RanToCompletion)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region private methods

        private UdpClientModel GetUdpClientModel(IPEndPoint localEndPoint)
        {
            if (_udpModels.ContainsKey(localEndPoint) == false)
                throw new InvalidOperationException($"Can't find udpModel by endPoint = {localEndPoint}");

            var udpModel = _udpModels[localEndPoint];
            return udpModel;
        }

        private bool StopReceivingInternal(UdpClientModel udpModel, int timeoutInMilliseconds = 300)
        {
            if (udpModel.ReceiveTask == null)
            {
                return false;
            }

            var status = udpModel.ReceiveTask.Status;
            if (status != TaskStatus.Canceled
                && status != TaskStatus.Faulted
                && status != TaskStatus.RanToCompletion)
            {
                udpModel.StoppingTokenSource.Cancel();
                udpModel.StoppingTokenSource = new CancellationTokenSource();

                var taskDelay = Task.Delay(timeoutInMilliseconds);
                taskDelay.Wait();
                return true;
            }

            return false;
        }

        private (IPEndPoint LocalEndPoint, UdpClientModel UdpModel) AddUdpClientToDictionary(UdpClient udpClient)
        {
            var localEndPoint = udpClient.Client.LocalEndPoint as IPEndPoint;
            if (localEndPoint == null)
                throw new InvalidOperationException($"Can't convert {udpClient.Client.LocalEndPoint} to IPEndPoint");

            if (_udpModels.ContainsKey(localEndPoint) == true)
                throw new InvalidOperationException($"UdpClient with port = {udpClient.Client.LocalEndPoint} already exists");

            var udpModel = new UdpClientModel
            {
                StoppingTokenSource = new CancellationTokenSource(),
                UdpClient = udpClient
            };

            var isAdded = _udpModels.TryAdd(localEndPoint, udpModel);
            if (isAdded == false)
            {
                throw new InvalidOperationException($"Can't add udpClient to dictionary udpModels");
            }

            return (localEndPoint, udpModel);
        }

        private UdpClient CreateUdpClient(int minPort, int maxPort)
        {
            UdpClient ret;

            for (int currentPort = minPort; currentPort <= maxPort; currentPort++)
            {
                try
                {
                    ret = new UdpClient(currentPort);
                    return ret;
                }
                catch (SocketException)
                {
                    _logger.LogInformation($"UdpPort {currentPort} is busy");
                }
            }

            throw new InvalidOperationException($"Can't allocate udp port from {minPort} to {maxPort}");
        }

        private void ProcessRecieve(UdpClientModel udpModel,
            Func<IPEndPoint, byte[], IPEndPoint, Task<byte[]>> onReceiveFuncAsunc,
            int? receiveTimeoutInMilliseconds)
        {
            StopReceivingInternal(udpModel);

            udpModel.ReceiveTask = udpModel.UdpClient.ReceiveAsync();
            udpModel.UdpClient.BeginReceive()
            if (udpModel.StoppingTokenSource.IsCancellationRequested == true)
            {
                udpModel.StoppingTokenSource = new CancellationTokenSource();
            }
            try
            {
                if (receiveTimeoutInMilliseconds > 0)
                {
                    udpModel.ReceiveTask.Wait(receiveTimeoutInMilliseconds.Value, udpModel.StoppingTokenSource.Token);
                }
                else
                {
                    udpModel.ReceiveTask.Wait(udpModel.StoppingTokenSource.Token);
                }
            }
            // *** If cancellation is requested, an OperationCanceledException results.
            catch (OperationCanceledException)
            {
                udpModel.ReceiveTask = null;
                return;
            }

            if (udpModel.ReceiveTask.IsCanceled == true)
            {
                udpModel.ReceiveTask = null;
                return;
            }

            if (udpModel.ReceiveTask.Exception != null)
            {
                var exception = udpModel.ReceiveTask.Exception;
                udpModel.ReceiveTask = null;
                throw exception;
            }

            var receiveResult = udpModel.ReceiveTask.Result;
            udpModel.ReceiveTask = null;

            var executeTask = onReceiveFuncAsunc(udpModel.UdpClient.Client.LocalEndPoint as IPEndPoint,
                receiveResult.Buffer,
                receiveResult.RemoteEndPoint);

            if (executeTask != null)
            {
                executeTask.Wait();
            }
            
            var onReceivedResult = executeTask.Result;

            if (onReceivedResult != null)
            {
                var sendTask = udpModel.UdpClient.SendAsync(onReceivedResult, onReceivedResult.Length, receiveResult.RemoteEndPoint);
                sendTask.Wait();
            }
        }

        #endregion
    }
}
