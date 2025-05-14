using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public abstract class AEndpoint(bool needExactPath, bool isHTTPEndpoint, bool isWebsocketEndpoint) : ResourceSystem.Resource
    {
        private readonly bool m_NeedExactPath = needExactPath;
        private readonly bool m_IsHTTPEndpoint = isHTTPEndpoint;
        private readonly bool m_IsWebsocketEndpoint = isWebsocketEndpoint;

        public bool NeedExactPath => m_NeedExactPath;
        public bool IsHTTPEndpoint => m_IsHTTPEndpoint;
        public bool IsWebsocketEndpoint => m_IsWebsocketEndpoint;

        protected AEndpoint(bool isHTTPEndpoint, bool isWebsocketEndpoint) : this(false, isHTTPEndpoint, isWebsocketEndpoint) { }

        //HTTP
        internal Response HandleRequest(Request request) => OnRequest(request);

        protected virtual Response OnRequest(Request request) => request.Method switch
        {
            Request.MethodType.GET => OnGetRequest(request),
            Request.MethodType.HEAD => OnHeadRequest(request),
            Request.MethodType.POST => OnPostRequest(request),
            Request.MethodType.PUT => OnPutRequest(request),
            Request.MethodType.DELETE => OnDeleteRequest(request),
            Request.MethodType.CONNECT => OnConnectRequest(request),
            Request.MethodType.OPTIONS => OnOptionsRequest(request),
            Request.MethodType.TRACE => OnTraceRequest(request),
            Request.MethodType.PATCH => OnPatchRequest(request),
            _ => new(400, "Bad Request")
        };

        protected virtual Response OnGetRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnHeadRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPostRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPutRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnDeleteRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnConnectRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnOptionsRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnTraceRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPatchRequest(Request request) => new(405, "Method Not Allowed");

        //Websocket
        internal void RegisterClient(WebsocketReference wsReference) => OnClientRegistered(wsReference);
        protected virtual void OnClientRegistered(WebsocketReference wsReference) { }
        internal void ClientMessage(WebsocketReference wsReference, string message) => OnClientMessage(wsReference, message);
        protected virtual void OnClientMessage(WebsocketReference wsReference, string message) { }
        internal void ClientUnregistered(WebsocketReference wsReference) => OnClientUnregistered(wsReference);
        protected virtual void OnClientUnregistered(WebsocketReference wsReference) { }
    }
}
