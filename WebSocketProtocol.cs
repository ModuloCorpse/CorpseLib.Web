using CorpseLib.Web.Http;

namespace CorpseLib.Web
{
    public class WebSocketProtocol : HttpProtocol
    {
        public WebSocketProtocol(Dictionary<string, string> extensions, int fragmentSize, bool isWebsocket): base(extensions, fragmentSize, isWebsocket) { }
        public WebSocketProtocol(Dictionary<string, string> extensions, int fragmentSize) : base(extensions, fragmentSize, false) { }
        public WebSocketProtocol(Dictionary<string, string> extensions) : base(extensions, -1, false) { }
        public WebSocketProtocol(int fragmentSize) : base(new(), fragmentSize, false) { }
        public WebSocketProtocol() : base(new(), -1, false) { }

        protected override void OnClientConnected()
        {
            base.OnClientConnected();
            if (!IsServerSide())
                SendWebSocketHandshake();
        }

        protected override void OnClientReconnected()
        {
            base.OnClientReconnected();
            if (!IsServerSide())
                SendWebSocketHandshake();
        }

        protected sealed override void OnHTTPRequest(Request request) { }

        protected sealed override void OnHTTPResponse(Response response)
        {
            if (response.StatusCode >= 400 && response.StatusCode < 500)
            {
                OnWSClose(response.StatusCode, response.StatusMessage);
                Disconnect();
            }
        }
    }
}
