using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class API
    {
        public class APIProtocol(API api, string id) : HttpProtocol
        {
            private readonly API m_API = api;
            private readonly string m_ID = id;

            public string ID => m_ID;

            protected override void OnHTTPRequest(Request request) => Send(m_API.HandleAPIRequest(request));

            protected override void OnWSOpen(Request request) => m_API.HandleWebsocketOpen(this, request);

            protected override void OnWSMessage(string message) => m_API.HandleWebsocketMessage(m_ID, message);

            protected override void OnWSClose(int status, string message) => m_API.HandleWebsocketClose(m_ID);
        }

        private readonly EndpointTreeNode m_EndpointTreeNode = new();
        private readonly Dictionary<string, AEndpoint> m_ClientEndpoints = [];
        private readonly TCPAsyncServer m_AsyncServer;

        public bool IsRunning => m_AsyncServer.IsRunning();

        public API(int port) => m_AsyncServer = new(() => new APIProtocol(this, Guid.NewGuid().ToString().Replace("-", string.Empty)), port);

        public void Start() => m_AsyncServer.Start();
        public void Stop() => m_AsyncServer.Stop();

        ~API() => m_AsyncServer.Stop();

        public void AddEndpoint(string path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler) => AddEndpoint(new Http.Path(path), methodType, methodHandler);

        public void AddEndpoint(Http.Path path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler)
        {
            AEndpoint? endpoint = m_EndpointTreeNode.GetEndpoint(path);
            if (endpoint != null && endpoint is HTTPEndpoint httpEndpoint)
                httpEndpoint.SetEndpoint(methodType, methodHandler);
            else
            {
                HTTPEndpoint newEndpoint = new(path, true);
                newEndpoint.SetEndpoint(methodType, methodHandler);
                m_EndpointTreeNode.Add(newEndpoint);
            }
        }

        public void AddEndpointTreeNode(Http.Path path, EndpointTreeNode endpointTree) => m_EndpointTreeNode.AddNode(path, endpointTree);
        public void AddEndpoint(AEndpoint endpoint) => m_EndpointTreeNode.Add(endpoint);

        internal Response HandleAPIRequest(Request request)
        {
            try
            {
                AEndpoint? endpoint = m_EndpointTreeNode.GetEndpoint(request.Path);
                if (endpoint != null && endpoint.IsHTTPEndpoint)
                    return endpoint.OnRequest(request);
                else
                    return new(404, "Not Found", string.Format("Endpoint {0} does not exist", request.Path));
            } catch (Exception e)
            {
                return new(500, "Internal Server Error", string.Format("API caught exception: {0}", e.Message));
            }
        }

        internal void HandleWebsocketOpen(APIProtocol client, Request request)
        {
            AEndpoint? endpoint = m_EndpointTreeNode.GetEndpoint(request.Path);
            if (endpoint != null && endpoint.IsWebsocketEndpoint)
            {
                endpoint.RegisterClient(client, request.Path);
                m_ClientEndpoints[client.ID] = endpoint;
            }
        }

        internal void HandleWebsocketMessage(string id, string message)
        {
            if (m_ClientEndpoints.TryGetValue(id, out AEndpoint? endpoint))
                endpoint.ClientMessage(id, message);
        }

        internal void HandleWebsocketClose(string id)
        {
            if (m_ClientEndpoints.TryGetValue(id, out AEndpoint? endpoint))
                endpoint.ClientUnregistered(id);
            m_ClientEndpoints.Remove(id);
        }

        public List<KeyValuePair<Http.Path, AEndpoint>> FlattenEndpoints() => m_EndpointTreeNode.Flatten(new());
    }
}
