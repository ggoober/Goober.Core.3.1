using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Goober.Http
{
    public static class UdpServerExtensions
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

    public class UdpServerOptionsModel
    {
        /// <summary>
        /// Option: receive buffer size
        /// </summary>
        public int? BufferSize { get; set; }

        /// <summary>
        /// Option: linger state
        /// </summary>
        public LingerOption LingerState { get; set; }

        public int ConnectTimeoutInMilliseconds { get; set; } = 15000;
    }

    /// <summary>
    /// UDP server is used to send or multicast datagrams to UDP endpoints
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public class UdpServer : IDisposable
    {
        #region protected

        /// <summary>
        /// Socket
        /// </summary>
        private Socket _socket;

        #endregion

        #region privates

        private ILogger _logger;

        private UdpServerOptionsModel _options;

        private SocketAsyncEventArgs receiveEventArg;

        private Func<IPEndPoint, byte[], Task<byte[]>> onReceivedFuncAsync { get; set; }

        #endregion

        #region properties

        /// <summary>
        /// Server Id
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// IP endpoint
        /// </summary>
        public EndPoint LocalEndpoint { get; private set; }

        public EndPoint RemoteEndPoint 
        { 
            get {
                if (_socket.Connected == false)
                    return null;

                return _socket.RemoteEndPoint;
            } 
        }

        private bool _isReceiving = false;

        private bool _isSending = false;

        /// <summary>
        /// Number of bytes received by the server
        /// </summary>
        private long _bytesReceived;
        public long BytesReceived { get { return _bytesReceived; } }

        /// <summary>
        /// Number of datagrams received by the server
        /// </summary>
        private long _datagramsReceived;
        public long DatagramsReceived { get { return _datagramsReceived; } }

        private long _bytesSent;
        public long BytesSend { get { return _bytesSent; } }

        private long _datagramsSent;
        public long DatagramsSent { get { return _datagramsSent; } }

        /// <summary>
        /// Is the server started?
        /// </summary>
        public bool IsStarted { get; private set; }

        #endregion

        #region ctor

        /// <summary>
        /// Initialize UDP server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public UdpServer(IPAddress address,
            int port, Func<IPEndPoint, byte[], Task<byte[]>> responseHandlerAsync,
            ILogger logger,
            UdpServerOptionsModel options = null)
            : this(new IPEndPoint(address, port), responseHandlerAsync, logger, options)
        {

        }

        /// <summary>
        /// Initialize UDP server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public UdpServer(string address, int port,
            Func<IPEndPoint, byte[], Task<byte[]>> responseHandlerAsync,
             ILogger logger,
            UdpServerOptionsModel options = null)
            : this(new IPEndPoint(IPAddress.Parse(address), port), responseHandlerAsync, logger, options)
        {

        }

        /// <summary>
        /// Initialize UDP server with a given IP endpoint
        /// </summary>
        /// <param name="receivingEndpoint">IP endpoint</param>
        public UdpServer(IPEndPoint receivingEndpoint,
            Func<IPEndPoint, byte[], Task<byte[]>> responseHandlerAsync,
            ILogger logger,
            UdpServerOptionsModel options = null)
        {
            Id = Guid.NewGuid();
            LocalEndpoint = receivingEndpoint;

            _options = options ?? new UdpServerOptionsModel();

            if (_options.LingerState == null)
            {
                _options.LingerState = new LingerOption(true, 10);
            }

            if (_options.BufferSize == null)
            {
                _options.BufferSize = 256;
            }

            if (_options.ConnectTimeoutInMilliseconds <= 0)
            {
                _options.ConnectTimeoutInMilliseconds = 15000;
            }

            _logger = logger;

            receiveEventArg = new SocketAsyncEventArgs();
            receiveEventArg.Completed += ProcessReceiveCompleted;
            receiveEventArg.RemoteEndPoint = new IPEndPoint((LocalEndpoint.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0);

            var bufferSize = _options.BufferSize.Value;
            var buffer = new byte[bufferSize];
            receiveEventArg.SetBuffer(buffer, 0, (int)buffer.Length);

            this.onReceivedFuncAsync = responseHandlerAsync;
        }

        #endregion

        #region Start|Stop methods

        /// <summary>
        /// Start the server (synchronous)
        /// </summary>
        /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
        public virtual bool Start()
        {
            if (IsStarted == true)
                throw new InvalidOperationException("already started");

            // Create a new server socket
            _socket = new Socket(LocalEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // Apply the option: reuse address
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind the server socket to the IP endpoint
            _socket.Bind(LocalEndpoint);

            LocalEndpoint = _socket.LocalEndPoint;

            _bytesReceived = 0;
            _datagramsReceived = 0;

            // Update the started flag
            IsStarted = true;

            return true;
        }

        /// <summary>
        /// Stop the server (synchronous)
        /// </summary>
        /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
        public virtual bool Stop()
        {
            if (IsStarted == false)
                return true;

            try
            {
                // Close the server socket
                _socket.Close();

                // Dispose the server socket
                _socket.Dispose();
            }
            catch (ObjectDisposedException) { }

            // Update the started flag
            IsStarted = false;

            return true;
        }

        /// <summary>
        /// Restart the server (synchronous)
        /// </summary>
        /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
        public virtual bool Restart()
        {
            if (Stop() == false)
                return false;

            return Start();
        }

        #endregion

        #region Recieve | Send data

        /// <summary>
        /// <summary>
        /// Receive datagram from the client (asynchronous)
        /// </summary>
        public void StartReceiving()
        {
            _isReceiving = true;
            var completedAsync = false;

            _logger.LogInformation($"Start receiving LocalEndPoint = {_socket.LocalEndPoint.ToString()}");

            try
            {
                completedAsync = _socket.ReceiveFromAsync(receiveEventArg);
            }
            catch
            {
                _isReceiving = false;
                throw;
            }

            if (completedAsync == false)
            {
                ProcessReceiveCompleted(this, receiveEventArg);
            }
        }

        public void SendResponse(byte[] msg)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            if (_isSending == true)
                throw new InvalidOperationException("Can't start second send");

            var e = new SocketAsyncEventArgs();
            e.SetBuffer(msg, 0, msg.Length);
            e.Completed += ProcessSendCompleted;

            _isSending = true;
            bool completedAsync = false;

            _logger.LogInformation($"Send response to RemoteEndPoint = {_socket.RemoteEndPoint}");

            try
            {
                completedAsync = _socket.SendAsync(e);
            }
            catch
            {
                _isSending = false;
                throw;
            }

            if (completedAsync == false)
            {
                ProcessSendCompleted(this, e);
            }
        }

        #endregion

        #region IO processing

        /// <summary>
        /// This method is invoked when an asynchronous receive from operation completes
        /// </summary>
        private void ProcessReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            _isReceiving = false;

            if (e.SocketError != SocketError.Success)
                throw new InvalidOperationException($"Receive error = {e.SocketError} e.RemoteEndPoint = {e.RemoteEndPoint}");

            long size = e.BytesTransferred;

            // Received some data from the client
            if (size > 0)
            {
                Interlocked.Increment(ref _datagramsReceived);
                Interlocked.Add(ref _bytesReceived, size);
            }

            _logger.LogInformation($"Receive completed from RemoteEndPoint = {e.RemoteEndPoint.ToString()}");

            var receiveTask = onReceivedFuncAsync((IPEndPoint)e.RemoteEndPoint, e.Buffer);
            receiveTask.Wait();
            if (receiveTask.Exception != null)
            {
                var baseException = receiveTask.Exception.GetBaseException();
                throw new InvalidOperationException($"On receive fault with message = {baseException.Message}, stackTrace = {baseException.StackTrace}");
            }

            var receiveResult = receiveTask.Result;
            if (receiveResult == null)
            {
                _logger.LogInformation($"Receive result is null");
                return;
            }

            if (_socket.Connected == false)
            {
                var connectTask = _socket.ConnectAsync(e.RemoteEndPoint);
                connectTask.Wait(_options.ConnectTimeoutInMilliseconds);

                _logger.LogInformation($"Connected not RemoteEndPoint = {e.RemoteEndPoint}");
            }
            else if (_socket.RemoteEndPoint != e.RemoteEndPoint)
            {
                throw new InvalidOperationException($"Can't connect to remoteEndPoint = {e.RemoteEndPoint}, already connect to = {_socket.RemoteEndPoint}");
            }

            SendResponse(receiveResult);
        }

        private void ProcessSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            _isSending = false;

            if (e.SocketError != SocketError.Success)
            {
                throw new InvalidOperationException($"Send error = {e.SocketError} e.RemoteEndPoint = {e.RemoteEndPoint}");
            }

            var size = e.BytesTransferred;

            if (size > 0)
            {
                Interlocked.Increment(ref _datagramsSent);
                Interlocked.Add(ref _bytesSent, size);
            }

            _logger.LogInformation($"Send completed");
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Disposed flag
        /// </summary>
        public bool IsDisposed { get; private set; }

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposingManagedResources)
        {
            // The idea here is that Dispose(Boolean) knows whether it is
            // being called to do explicit cleanup (the Boolean is true)
            // versus being called due to a garbage collection (the Boolean
            // is false). This distinction is useful because, when being
            // disposed explicitly, the Dispose(Boolean) method can safely
            // execute code using reference type fields that refer to other
            // objects knowing for sure that these other objects have not been
            // finalized or disposed of yet. When the Boolean is false,
            // the Dispose(Boolean) method should not execute code that
            // refer to reference type fields because those objects may
            // have already been finalized."

            if (!IsDisposed)
            {
                if (disposingManagedResources)
                {
                    // Dispose managed resources here...
                    Stop();
                }

                // Dispose unmanaged resources here...

                // Set large fields to null here...

                // Mark as disposed.
                IsDisposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~UdpServer()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        #endregion
    }
}