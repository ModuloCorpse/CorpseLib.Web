namespace CorpseLib.Web.API
{
    public class EndpointTreeNode
    {
        private readonly Dictionary<string, EndpointTreeNode> m_Children = [];
        private AEndpoint? m_Value;
        private bool m_NeedExactPath = true;

        public EndpointTreeNode() { }
        private EndpointTreeNode(AEndpoint value, bool needExactPath)
        {
            m_Value = value;
            m_NeedExactPath = needExactPath;
        }

        public void AddNode(Http.Path path, EndpointTreeNode node)
        {
            Http.Path? nextPath = path.NextPath();
            if (nextPath == null)
                m_Children[path.CurrentPath] = node;
            else if (m_Children.TryGetValue(path.CurrentPath, out EndpointTreeNode? childNode))
                childNode.AddNode(nextPath, node);
            else
            {
                EndpointTreeNode newNode = new();
                newNode.AddNode(nextPath, node);
                m_Children[path.CurrentPath] = newNode;
            }
        }

        public void AddValue(Http.Path path, AEndpoint value, bool needExactPath)
        {
            if (path.Paths.Length == 0)
            {
                m_Value = value;
                m_NeedExactPath = needExactPath;
                return;
            }
            Http.Path? nextPath = path.NextPath();
            if (nextPath == null)
                m_Children[path.CurrentPath] = new(value, needExactPath);
            else if (m_Children.TryGetValue(path.CurrentPath, out EndpointTreeNode? node))
                node.AddValue(nextPath, value, needExactPath);
            else
            {
                EndpointTreeNode newNode = new();
                newNode.AddValue(nextPath, value, needExactPath);
                m_Children[path.CurrentPath] = newNode;
            }
        }

        public AEndpoint? GeAEndpoint(Http.Path path)
        {
            AEndpoint? ret = default;
            Http.Path? nextPath = path.NextPath();
            if (m_Children.TryGetValue(path.CurrentPath, out EndpointTreeNode? node))
            {
                if (nextPath == null)
                    ret = node.m_Value;
                else
                    ret = node.GeAEndpoint(nextPath);
            }
            if (ret == null && !m_NeedExactPath)
                ret = m_Value;
            return ret;
        }

        public List<KeyValuePair<Http.Path, AEndpoint>> Flatten(Http.Path root)
        {
            List<KeyValuePair<Http.Path, AEndpoint>> list = [];
            if (m_Value != null)
                list.Add(new(root, m_Value));
            foreach (var pair in m_Children)
            {
                EndpointTreeNode child = pair.Value;
                Http.Path childPath = root.Append(pair.Key);
                list.AddRange(child.Flatten(childPath));
            }
            return list;
        }

        public void Add(AEndpoint[] endpoints)
        {
            foreach (AEndpoint endpoint in endpoints)
                AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);
        }

        public void Add(AEndpoint endpoint) => AddValue(endpoint.Path, endpoint, endpoint.NeedExactPath);
        public AEndpoint? GetEndpoint(Http.Path path) => GeAEndpoint(path);
    }
}
