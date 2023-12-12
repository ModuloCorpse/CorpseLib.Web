using CorpseLib.Web.Http;
using CorpseLib.Web.WebSocket;

namespace CorpseLib.Web
{
    public class WebSocketEventProtocol : WebSocketProtocol
    {
        public delegate void FrameDelegate(Frame frame);
        public delegate void OpenDelegate(Response message);
        public delegate void CloseDelegate(int status, string message);
        public delegate void MessageDelegate(string message);

        public event OpenDelegate? OnOpen;
        public event FrameDelegate? OnFrameReceived;
        public event FrameDelegate? OnFrameSent;
        public event MessageDelegate? OnMessage;
        public event CloseDelegate? OnClose;

        public WebSocketEventProtocol(Dictionary<string, string> extensions, int fragmentSize) : base(extensions, fragmentSize) { }
        public WebSocketEventProtocol(Dictionary<string, string> extensions) : base(extensions, -1) { }
        public WebSocketEventProtocol(int fragmentSize) : base(new(), fragmentSize) { }
        public WebSocketEventProtocol() : base(new(), -1) { }


        protected override void OnWSFrameReceived(Frame frame) => OnFrameReceived?.Invoke(frame);
        protected override void OnWSFrameSent(Frame frame) => OnFrameSent?.Invoke(frame);
        protected override void OnWSOpen(Response message) => OnOpen?.Invoke(message);
        protected override void OnWSClose(int status, string message) => OnClose?.Invoke(status, message);
        protected override void OnWSMessage(string message) => OnMessage?.Invoke(message);
    }
}
