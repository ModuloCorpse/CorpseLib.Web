using CorpseLib.Network;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CorpseLib.Web.Http;
using CorpseLib.Web.WebSocket;
using CorpseLib.Serialize;

namespace CorpseLib.Web
{
    public class HttpProtocol : AProtocol
    {
        private X509Certificate? m_Certificate = null;
        private readonly FragmentedFrameBuilder m_Builder = new();
        private readonly Dictionary<string, string> m_Extensions;
        private readonly string m_SecWebSocketKey;
        private readonly string m_ExpectedSecWebSocketKey;
        private Http.Path m_WebSocketPath = new();
        private readonly int m_FragmentSize;
        private bool m_IsWebsocket = false;

        public Http.Path WebSocketPath => m_WebSocketPath;
        public bool IsWebsocket => m_IsWebsocket;

        public HttpProtocol(Dictionary<string, string> extensions, int fragmentSize)
        {
            m_Extensions = extensions;
            m_SecWebSocketKey = Guid.NewGuid().ToString().Replace("-", string.Empty);
            m_ExpectedSecWebSocketKey = Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(m_SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            m_FragmentSize = fragmentSize;
        }

        protected void SetExtension(string key, string value) => m_Extensions[key] = value;

        /// <summary>
        /// Set an SSL certificate on the server
        /// </summary>
        /// <remarks>
        /// Calling this function will make this http/ws server a https/wss server
        /// </remarks>
        /// <param name="certificate">SSL certificate to use on the server</param>
        public void SetCertificate(X509Certificate certificate) => m_Certificate = certificate;

        /// <summary>
        /// Load an SSL certificate file on the server
        /// </summary>
        /// <remarks>
        /// Calling this function will make this http/ws server a https/wss server
        /// </remarks>
        /// <param name="path">Path of the SSL certificate file to use on the server</param>
        public void LoadCertificate(string path) => m_Certificate = X509Certificate.CreateFromCertFile(path);

        public HttpProtocol(Dictionary<string, string> extensions) : this(extensions, -1) { }
        public HttpProtocol(int fragmentSize) : this([], fragmentSize) { }
        public HttpProtocol() : this([], -1) { }

        private List<Frame> FragmentFrame(bool fin, int opCode, byte[] message)
        {
            List<Frame> frames = [];
            int nbFragment = (m_FragmentSize > 0 && (opCode == 1 || opCode == 2)) ? message.Length / m_FragmentSize : 0;
            if (nbFragment == 0)
            {
                frames.Add(new(fin, opCode, message));
                return frames;
            }
            for (int i = 0; i <= nbFragment; i++)
            {
                int fragmentOpCode = (i == 0) ? opCode : 0;
                byte[] frameBuffer = (i == nbFragment) ? message : message[..m_FragmentSize];
                message = message[frameBuffer.Length..];
                frames.Add(new((i == nbFragment), fragmentOpCode, frameBuffer));
            }
            return frames;
        }

        private OperationResult<object> ReadHTTP(BytesReader reader)
        {
            OperationResult<AMessage> message = reader.SafeRead<AMessage>();
            if (message)
            {
                if (message.Result != null)
                {
                    if (IsServerSide() && message.Result is Request request)
                        return new(request);
                    else if (!IsServerSide() && message.Result is Response response)
                        return new(response);
                }
                return new(null);
            }
            return new(message.Error, message.Description);
        }

        protected override OperationResult<object> Read(BytesReader reader) => m_IsWebsocket ? reader.SafeRead<Frame>().Cast<object>() : ReadHTTP(reader);

        private void TreatWebsocket(object packet)
        {
            if (packet is Frame readFrame)
            {
                Frame? frame = m_Builder.BuildFrame(readFrame);
                if (frame != null)
                {
                    OnWSFrameReceived(frame);
                    switch (frame.GetOpCode())
                    {
                        case 1: OnWSMessage(Encoding.UTF8.GetString(frame.GetContent())); break; //1 - text message
                        case 8: OnWSClose(frame.GetStatusCode(), Encoding.UTF8.GetString(frame.GetContent())); break; //8 - close message
                        case 9: //9 - ping message
                            frame.SetOpCode(10); //10 - pong message
                            ForceSend(frame);
                            break;
                    }
                }
            }
        }

        private void TreatHTTP(object packet)
        {
            if (IsServerSide() && packet is Request request)
            {
                if (request.HaveHeaderField("Sec-WebSocket-Key"))
                {
                    Response handshakeResponse = new(101, "Switching Protocols");
                    handshakeResponse["Server"] = "Web Overlay HTTP Server";
                    handshakeResponse["Content-Type"] = "text/html";
                    handshakeResponse["Connection"] = "Upgrade";
                    handshakeResponse["Upgrade"] = "websocket";
                    handshakeResponse["Sec-WebSocket-Accept"] = Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes((string)request["Sec-WebSocket-Key"] + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                    ForceSend(handshakeResponse);
                    m_IsWebsocket = true;
                    m_WebSocketPath = request.Path;
                    OnWSOpen(request);
                }
                else
                    OnHTTPRequest(request);
            }
            else if (!IsServerSide() && packet is Response response)
            {
                if (response.HaveHeaderField("Sec-WebSocket-Accept") &&
                    (string)response["Sec-WebSocket-Accept"] == m_ExpectedSecWebSocketKey)
                {
                    m_IsWebsocket = true;
                    OnWSOpen(response);
                }
                else
                    OnHTTPResponse(response);
            }
        }

        protected override void Treat(object packet)
        {
            if (m_IsWebsocket)
                TreatWebsocket(packet);
            else
                TreatHTTP(packet);
        }

        private void SendFrame(BytesWriter writer, Frame frame)
        {
            if (!IsServerSide()) //We mask the frame if it's send by a client
                frame.SetMask(new Random().Next());
            OnWSFrameSent(frame);
            writer.Write(frame);
        }

        protected override void Write(BytesWriter writer, object obj)
        {
            if (m_IsWebsocket)
            {
                if (obj is Frame frameBuffer)
                    SendFrame(writer, frameBuffer);
                else
                {
                    List<Frame> frames = FragmentFrame(true, 1, Encoding.UTF8.GetBytes(obj.ToString() ?? string.Empty));
                    foreach (Frame frame in frames)
                        SendFrame(writer, frame);
                }
            }
            else
            {
                if (obj is AMessage message)
                    writer.Write(message);
            }
        }

        private void HandleSSLConnection()
        {
            URI url = GetURL();
            if (IsServerSide())
            {
                if (m_Certificate != null)
                {
                    SslStream sslStream = new(GetStream(), leaveInnerStreamOpen: false);
                    sslStream.AuthenticateAsServer(m_Certificate, clientCertificateRequired: false, checkCertificateRevocation: true);
                    SetStream(sslStream);
                }
            }
            else if (url.Scheme == "wss" || url.Scheme == "https")
            {
                SslStream sslStream = new(GetStream(), leaveInnerStreamOpen: false);
                var options = new SslClientAuthenticationOptions()
                {
                    TargetHost = url.Host,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => errors == SslPolicyErrors.None,
                };

                sslStream.AuthenticateAsClientAsync(options, CancellationToken.None).Wait();
                SetStream(sslStream);
            }
        }

        protected override void OnClientConnected() => HandleSSLConnection();

        protected override void OnClientReconnected() => HandleSSLConnection();

        protected override void OnClientReset()
        {
            m_IsWebsocket = false;
            m_Builder.Clear();
        }

        protected override void OnClientDisconnected() {}

        protected void SendWebSocketHandshake()
        {
            URI url = GetURL();
            Request handshakeRequest = new(Request.MethodType.GET, (!string.IsNullOrWhiteSpace(url.FullPath)) ? url.Path : "/");
            handshakeRequest["Upgrade"] = "websocket";
            handshakeRequest["Connection"] = "Upgrade";
            handshakeRequest["Sec-WebSocket-Version"] = "13";
            handshakeRequest["Sec-WebSocket-Key"] = m_SecWebSocketKey;
            handshakeRequest["Host"] = url.Host;
            foreach (KeyValuePair<string, string> extension in m_Extensions)
                handshakeRequest[extension.Key] = extension.Value;
            ForceSend(handshakeRequest);
            if (IsSynchronous())
                ReadFromStream();
        }

        protected virtual void OnHTTPRequest(Request request) { }
        protected virtual void OnHTTPResponse(Response response) { }
        protected virtual void OnWSFrameReceived(Frame frame) { }
        protected virtual void OnWSFrameSent(Frame frame) { }
        protected virtual void OnWSOpen(Request message) { }
        protected virtual void OnWSOpen(Response message) { }
        protected virtual void OnWSClose(int status, string message) { }
        protected virtual void OnWSMessage(string message) { }

        protected override void SetupSerializer(ref BytesSerializer serializer)
        {
            serializer.Register(new HttpSerializer());
            serializer.Register(new FrameSerializer());
        }
    }
}
