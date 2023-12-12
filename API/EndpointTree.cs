namespace CorpseLib.Web.API
{
    internal class EndpointTree<TEndpoint> : PathTree<TEndpoint> where TEndpoint : AEndpoint
    {
        public void AddEndpoint(Http.Path path, TEndpoint endpoint) => AddValue(path, endpoint, endpoint.NeedExactPath);

        public TEndpoint? GetEndpoint(Http.Path path) => GetValue(path);
    }
}
