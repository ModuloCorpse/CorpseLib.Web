using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class API
    {
        public class APIProtocol : HttpProtocol
        {
            private readonly API m_API;
            private readonly string m_ID;

            public string ID => m_ID;

            public APIProtocol(API api, string id)
            {
                m_API = api;
                m_ID = id;
            }

            protected override void OnHTTPRequest(Request request) => Send(m_API.HandleAPIRequest(request));

            protected override void OnWSOpen(Request request) => m_API.HandleWebsocketOpen(this, request);

            protected override void OnWSMessage(string message) => m_API.HandleWebsocketMessage(m_ID, message);

            protected override void OnWSClose(int status, string message) => m_API.HandleWebsocketClose(m_ID);
        }

        private readonly EndpointTree<AHTTPEndpoint> m_HTTPEndpointTree = new();
        private readonly EndpointTree<AWebsocketEndpoint> m_WebsocketEndpointTree = new();
        private readonly Dictionary<string, AWebsocketEndpoint> m_ClientEndpoint = new();
        private readonly TCPAsyncServer m_AsyncServer;

        public bool IsRunning => m_AsyncServer.IsRunning();

        public API(int port) => m_AsyncServer = new(() => new APIProtocol(this, Guid.NewGuid().ToString().Replace("-", "")), port);

        public void Start() => m_AsyncServer.Start();
        public void Stop() => m_AsyncServer.Stop();

        ~API() => m_AsyncServer.Stop();

        public void AddEndpoint(string path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler)
        {
            AHTTPEndpoint? endpoint = m_HTTPEndpointTree.GetEndpoint(path);
            if (endpoint != null && endpoint is HTTPEndpoint httpEndpoint)
                httpEndpoint.SetEndpoint(methodType, methodHandler);
            else
            {
                HTTPEndpoint newEndpoint = new(path, true);
                newEndpoint.SetEndpoint(methodType, methodHandler);
                m_HTTPEndpointTree.AddEndpoint(path, newEndpoint);
            }
        }

        public void AddEndpoint(AEndpoint endpoint)
        {
            if (endpoint is AWebsocketEndpoint websocketEndpoint)
                m_WebsocketEndpointTree.AddEndpoint(endpoint.Path, websocketEndpoint);
            else if (endpoint is AHTTPEndpoint httpEndpoint)
                m_HTTPEndpointTree.AddEndpoint(endpoint.Path, httpEndpoint);
        }

        internal Response HandleAPIRequest(Request request)
        {
            AHTTPEndpoint? httpEndpoint = m_HTTPEndpointTree.GetEndpoint(request.Path);
            if (httpEndpoint != null)
                return httpEndpoint.OnRequest(request);
            else
                return new(404, "Not Found", string.Format("Endpoint {0} does not exist", request.Path));
        }

        internal void HandleWebsocketOpen(APIProtocol client, Request request)
        {
            AWebsocketEndpoint? websocketEndpoint = m_WebsocketEndpointTree.GetEndpoint(request.Path);
            if (websocketEndpoint != null)
            {
                websocketEndpoint.RegisterClient(client);
                m_ClientEndpoint[client.ID] = websocketEndpoint;
            }
        }

        internal void HandleWebsocketMessage(string id, string message)
        {
            if (m_ClientEndpoint.TryGetValue(id, out AWebsocketEndpoint? endpoint))
                endpoint.ClientMessage(id, message);
        }

        internal void HandleWebsocketClose(string id)
        {
            if (m_ClientEndpoint.TryGetValue(id, out AWebsocketEndpoint? endpoint))
                endpoint.ClientUnregistered(id);
            m_ClientEndpoint.Remove(id);
        }
    }
}
