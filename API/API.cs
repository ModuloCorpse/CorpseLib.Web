using CorpseLib.Network;
using CorpseLib.Web.Http;
using static CorpseLib.Web.Http.ResourceSystem;
using Path = CorpseLib.Web.Http.Path;

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
            protected override bool AllowWSOpen(Request request) => m_API.CanOpenWebsocket(request);
            protected override void OnWSOpen(Request request) => m_API.HandleWebsocketOpen(this, request);
            protected override void OnWSMessage(string message) => m_API.HandleWebsocketMessage(this, message);
            protected override void OnWSClose(int status, string message) => m_API.HandleWebsocketClose(this);
        }

        private readonly ResourceSystem m_ResourceSystem = new();
        private readonly Dictionary<string, Http.Path> m_WebsocketClientsPath = [];
        private readonly TCPAsyncServer m_AsyncServer;

        public bool IsRunning => m_AsyncServer.IsRunning();

        public API(int port) => m_AsyncServer = new(() => new APIProtocol(this, Guid.NewGuid().ToString().Replace("-", string.Empty)), port);

        public void Start() => m_AsyncServer.Start();
        public void Stop() => m_AsyncServer.Stop();

        ~API() => m_AsyncServer.Stop();

        public void AddEndpoint(string path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler) => AddEndpoint(new Http.Path(path), methodType, methodHandler);

        public void AddEndpoint(Http.Path path, Request.MethodType methodType, HTTPEndpoint.MethodHandler methodHandler)
        {
            Resource? endpoint = m_ResourceSystem.Get(path);
            if (endpoint != null && endpoint is HTTPEndpoint httpEndpoint)
                httpEndpoint.SetEndpoint(methodType, methodHandler);
            else
            {
                HTTPEndpoint newEndpoint = new(true);
                newEndpoint.SetEndpoint(methodType, methodHandler);
                m_ResourceSystem.Add(path, newEndpoint);
            }
        }

        public void AddDirectory(Http.Path path, ResourceSystem.Directory directory) => m_ResourceSystem.Add(path, directory);
        public void AddEndpoint(Http.Path path, AEndpoint endpoint) => m_ResourceSystem.Add(path, endpoint);

        internal Response HandleAPIRequest(Request request)
        {
            try
            {
                Resource? resource = m_ResourceSystem.Get(request.Path);
                if (resource != null && resource is AEndpoint endpoint && endpoint.IsHTTPEndpoint)
                    return endpoint.HandleRequest(request);
                else if (resource != null && resource is ResourceSystem.Directory directory)
                {
                    Path newPath = request.Path.Append(string.Empty);
                    Response response = new(301, "Moved Permanently");
                    response["Location"] = newPath.ToString();
                    return response;
                }
                else
                    return new(404, "Not Found", string.Format("Endpoint {0} does not exist", request.Path));
            } catch (Exception e)
            {
                return new(500, "Internal Server Error", string.Format("API caught exception: {0}", e.Message));
            }
        }

        internal bool CanOpenWebsocket(Request request)
        {
            Resource? resource = m_ResourceSystem.Get(request.Path);
            return (resource != null && resource is AEndpoint endpoint && endpoint.IsWebsocketEndpoint);
        }

        internal void HandleWebsocketOpen(APIProtocol client, Request request)
        {
            Resource? resource = m_ResourceSystem.Get(request.Path);
            if (resource != null && resource is AEndpoint endpoint && endpoint.IsWebsocketEndpoint)
            {
                endpoint.RegisterClient(new(client, request.Path));
                m_WebsocketClientsPath[client.ID] = request.Path;
            }
            else
            {
                client.Send(new Response(400, "Bad Request", "Not a websocket"));
                client.Disconnect();
                return;
            }
        }

        internal void HandleWebsocketMessage(APIProtocol client, string message)
        {
            if (m_WebsocketClientsPath.TryGetValue(client.ID, out Http.Path? path))
            {
                Resource? resource = m_ResourceSystem.Get(path);
                if (resource != null && resource is AEndpoint endpoint && endpoint.IsWebsocketEndpoint)
                    endpoint.ClientMessage(new(client, path), message);
            }
        }

        internal void HandleWebsocketClose(APIProtocol client)
        {
            if (m_WebsocketClientsPath.TryGetValue(client.ID, out Http.Path? path))
            {
                Resource? resource = m_ResourceSystem.Get(path);
                if (resource != null && resource is AEndpoint endpoint && endpoint.IsWebsocketEndpoint)
                    endpoint.ClientUnregistered(new(client, path));
                m_WebsocketClientsPath.Remove(client.ID);
            }
        }

        public List<KeyValuePair<Http.Path, Resource>> FlattenEndpoints() => m_ResourceSystem.Flatten();
    }
}
