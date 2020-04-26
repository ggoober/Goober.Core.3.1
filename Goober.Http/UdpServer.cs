using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Goober.Http
{
    public class UdpServerOptionsModel
    {
        /// <summary>
        /// Option: reuse address
        /// </summary>
        /// <remarks>
        /// This option will enable/disable SO_REUSEADDR if the OS support this feature
        /// </remarks>
        public bool ReuseAddress { get; set; }

        /// <summary>
        /// Option: enables a socket to be bound for exclusive access
        /// </summary>
        /// <remarks>
        /// This option will enable/disable SO_EXCLUSIVEADDRUSE if the OS support this feature
        /// </remarks>
        public bool ExclusiveAddressUse { get; set; }

        /// <summary>
        /// Option: receive buffer size
        /// </summary>
        public int? ReceiveBufferSize { get; set; }

        /// <summary>
        /// Option: linger state
        /// </summary>
        public LingerOption LingerState { get; set; }
    }

    /// <summary>
    /// UDP server is used to send or multicast datagrams to UDP endpoints
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public class UdpServer : IDisposable
    {
        class ReceiveStringUserToken
        {
            public Func<SocketError, IPEndPoint, string, Task> OnReceivedStringFuncAsync { get; set; }
        }

        class SendStringUserToken
        {
            public Func<SocketError, Task> OnSendStringFuncAsync { get; set; }
        }

        #region protected

        /// <summary>
        /// Socket
        /// </summary>
        private Socket _socket;

        #endregion

        #region privates

        private UdpServerOptionsModel _options;

        #endregion

        #region properties

        /// <summary>
        /// Server Id
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// IP endpoint
        /// </summary>
        public IPEndPoint ListenerEndpoint { get; }

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
        public UdpServer(IPAddress address, int port, UdpServerOptionsModel options = null)
            : this(new IPEndPoint(address, port), options)
        {

        }

        /// <summary>
        /// Initialize UDP server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public UdpServer(string address, int port, UdpServerOptionsModel options = null)
            : this(new IPEndPoint(IPAddress.Parse(address), port), options)
        {

        }

        /// <summary>
        /// Initialize UDP server with a given IP endpoint
        /// </summary>
        /// <param name="receivingEndpoint">IP endpoint</param>
        public UdpServer(IPEndPoint receivingEndpoint, UdpServerOptionsModel options = null)
        {
            Id = Guid.NewGuid();
            ListenerEndpoint = receivingEndpoint;
            _options = options ?? new UdpServerOptionsModel
            {
                ExclusiveAddressUse = false,
                ReuseAddress = false
            };

            if (_options.LingerState == null)
            {
                _options.LingerState = new LingerOption(true, 10);
            }

            if (_options.ReceiveBufferSize == null)
            {
                _options.ReceiveBufferSize = 256;
            }
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
            _socket = new Socket(ListenerEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // Apply the option: reuse address
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _options.ReuseAddress);

            //// Apply the option: exclusive address use
            //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, _options.ExclusiveAddressUse);

            // Bind the server socket to the IP endpoint
            _socket.Bind(ListenerEndpoint);

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
        public virtual void ReceiveString(Func<SocketError, IPEndPoint, string, Task> onReceivedStringFuncAsync, 
            IPEndPoint receiveEndpoint = null, 
            int? size = null)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            if (_isReceiving == true)
                throw new InvalidOperationException("Can't start second receiving");

            // Setup event args
            var receiveEventArg = new SocketAsyncEventArgs();
            receiveEventArg.UserToken = new ReceiveStringUserToken { OnReceivedStringFuncAsync = onReceivedStringFuncAsync };
            receiveEventArg.Completed += ProcessReceiveStringCompleted;
            receiveEventArg.RemoteEndPoint = receiveEndpoint ?? new IPEndPoint((ListenerEndpoint.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0);

            var bufferSize = size ?? _options.ReceiveBufferSize.Value;
            var buffer = new byte[bufferSize];
            receiveEventArg.SetBuffer(buffer, 0, (int)buffer.Length);

            _isReceiving = true;
            bool completedAsync = false;

            try
            {
                completedAsync = _socket.ReceiveFromAsync(receiveEventArg);
            }
            catch (Exception exc)
            {
                _isReceiving = false;
                throw;
            }

            if (completedAsync == false)
            {
                ProcessReceiveStringCompleted(this, receiveEventArg);
            }
        }

        public virtual void SendString(Func<SocketError, Task> onSendStringFuncAsync,
            string msg,
            IPEndPoint destinationEndPoint)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            if (_isSending == true)
                throw new InvalidOperationException("Can't start second send");

            _socket.Connect(destinationEndPoint);

            byte[] buffer = Encoding.UTF8.GetBytes(msg);

            var e = new SocketAsyncEventArgs();
            e.UserToken = new SendStringUserToken { OnSendStringFuncAsync = onSendStringFuncAsync };
            e.SetBuffer(buffer, 0, msg.Length);
            e.Completed += ProcessSendStringCompleted;
            
            _isSending = true;
            bool completedAsync = false;

            try
            {
                completedAsync = _socket.SendAsync(e);
            }
            catch (Exception exc)
            {
                _isSending = false;
                throw;
            }

            if (completedAsync == false)
            {
                ProcessSendStringCompleted(this, e);
            }
        }

        #endregion

        #region IO processing

        /// <summary>
        /// This method is invoked when an asynchronous receive from operation completes
        /// </summary>
        private void ProcessReceiveStringCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            _isReceiving = false;

            var receiveStringUserToken = e.UserToken as ReceiveStringUserToken;
            if (receiveStringUserToken == null)
                throw new InvalidOperationException($"{nameof(receiveStringUserToken)} is null");

            long size = e.BytesTransferred;

            // Received some data from the client
            if (size > 0)
            {
                Interlocked.Increment(ref _datagramsReceived);
                Interlocked.Add(ref _bytesReceived, size);
            }

            int nullIdx = Array.IndexOf(e.Buffer, (byte)0);
            nullIdx = nullIdx >= 0 ? nullIdx : e.Buffer.Length;
            var msg = Encoding.UTF8.GetString(e.Buffer, 0, nullIdx);
            
            var task = receiveStringUserToken.OnReceivedStringFuncAsync(e.SocketError, (IPEndPoint) e.RemoteEndPoint, msg);
            task.Wait();
        }

        private void ProcessSendStringCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsStarted == false)
                throw new InvalidOperationException("Server not started");

            _isSending = false;

            var sendStringUserToken = e.UserToken as SendStringUserToken;
            if (sendStringUserToken == null)
                throw new InvalidOperationException($"{nameof(sendStringUserToken)} is null");

            var size = e.BytesTransferred;

            if (size > 0)
            {
                Interlocked.Increment(ref _datagramsSent);
                Interlocked.Add(ref _bytesSent, size);
            }

            var task = sendStringUserToken.OnSendStringFuncAsync(e.SocketError);
            task.Wait();
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