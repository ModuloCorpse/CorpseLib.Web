namespace CorpseLib.Web.API
{
    internal class EndpointTree<TEndpoint> where TEndpoint : AEndpoint
    {
        private class EndpointNode
        {
            private Dictionary<string, EndpointNode> m_Children = new();
            private readonly TEndpoint? m_Endpoint;

            public EndpointNode() { }
            public EndpointNode(TEndpoint endpoint) => m_Endpoint = endpoint;

            public void AddEndpoint(string[] path, TEndpoint endpoint)
            {
                if (path.Length == 0)
                    return;
                else if (path.Length == 1)
                    m_Children[path[0]] = new(endpoint);
                else if (m_Children.TryGetValue(path[0], out EndpointNode? node))
                {
                    string[] newPath = path.Skip(1).ToArray();
                    node.AddEndpoint(newPath, endpoint);
                }
                else
                {
                    EndpointNode endpointNode = new();
                    string[] newPath = path.Skip(1).ToArray();
                    endpointNode.AddEndpoint(newPath, endpoint);
                    m_Children[path[0]] = endpointNode;
                }
            }

            public TEndpoint? GetEndpoint(string[] path)
            {
                TEndpoint? ret = null;
                if (path.Length == 0)
                    return m_Endpoint;
                else if (m_Children.TryGetValue(path[0], out EndpointNode? node))
                {
                    string[] newPath = path.Skip(1).ToArray();
                    ret = node.GetEndpoint(newPath);
                }
                if (ret == null && m_Endpoint != null && !m_Endpoint.NeedExactPath)
                    return m_Endpoint;
                return ret;
            }
        }

        private readonly EndpointNode m_Root = new();

        private string[] SplitKey(string key)
        {
            while (key.StartsWith("/"))
                key = key[1..];
            return key.Split('/');
        }

        public void AddEndpoint(string path, TEndpoint endpoint) => m_Root.AddEndpoint(SplitKey(path), endpoint);

        public TEndpoint? GetEndpoint(string path) => m_Root.GetEndpoint(SplitKey(path));
    }
}
