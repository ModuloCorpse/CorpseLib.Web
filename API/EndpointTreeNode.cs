using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class EndpointTreeNode : PathTreeNode<AEndpoint>
    {
        public void Add(AEndpoint[] endpoints)
        {
            foreach (AEndpoint endpoint in endpoints)
                AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);
        }

        public void Add(AEndpoint endpoint) => AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);

        public AEndpoint? GetEndpoint(Http.Path path) => GetValue(path);
    }
}
