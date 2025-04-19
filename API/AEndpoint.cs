using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public abstract class AEndpoint(Http.Path path, bool needExactPath, bool isHTTPEndpoint, bool isWebsocketEndpoint)
    {
        private readonly Http.Path m_Path = path;
        private readonly bool m_NeedExactPath = needExactPath;
        private readonly bool m_IsHTTPEndpoint = isHTTPEndpoint;
        private readonly bool m_IsWebsocketEndpoint = isWebsocketEndpoint;

        public Http.Path Path => m_Path;
        public bool NeedExactPath => m_NeedExactPath;
        public bool IsHTTPEndpoint => m_IsHTTPEndpoint;
        public bool IsWebsocketEndpoint => m_IsWebsocketEndpoint;

        protected AEndpoint(string path, bool isHTTPEndpoint, bool isWebsocketEndpoint) : this(new Http.Path(path), false, isHTTPEndpoint, isWebsocketEndpoint) { }
        protected AEndpoint(Http.Path path, bool isHTTPEndpoint, bool isWebsocketEndpoint) : this(path, false, isHTTPEndpoint, isWebsocketEndpoint) { }
        protected AEndpoint(string path, bool needExactPath, bool isHTTPEndpoint, bool isWebsocketEndpoint) : this(new Http.Path(path), needExactPath, isHTTPEndpoint, isWebsocketEndpoint) { }

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
        internal void RegisterClient(API.APIProtocol client, Http.Path path) => OnClientRegistered(client, path);
        protected virtual void OnClientRegistered(API.APIProtocol client, Http.Path path) { }
        internal void ClientMessage(string id, string message) => OnClientMessage(id, message);
        protected virtual void OnClientMessage(string id, string message) { }
        internal void ClientUnregistered(string id) => OnClientUnregistered(id);
        protected virtual void OnClientUnregistered(string id) { }
    }
}
