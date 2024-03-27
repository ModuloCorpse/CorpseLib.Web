namespace CorpseLib.Web.API
{
    public class HTTPEndpointNode : PathTreeNode<AHTTPEndpoint>
    {
        public void Add(AHTTPEndpoint endpoint) => AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);
        public AHTTPEndpoint? GetEndpoint(Http.Path path) => GetValue(path);
    }

    public class WebSocketEndpointNode : PathTreeNode<AWebsocketEndpoint>
    {
        public void Add(AWebsocketEndpoint endpoint) => AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);
        public AWebsocketEndpoint? GetEndpoint(Http.Path path) => GetValue(path);
    }
}
