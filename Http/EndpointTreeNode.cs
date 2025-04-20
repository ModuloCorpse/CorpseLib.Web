namespace CorpseLib.Web.Http
{
    public class PathTreeNode<TValue>
    {
        private readonly Dictionary<string, PathTreeNode<TValue>> m_Children = [];
        private TValue? m_Value;
        private bool m_NeedExactPath = true;

        public PathTreeNode() { }
        private PathTreeNode(TValue value, bool needExactPath)
        {
            m_Value = value;
            m_NeedExactPath = needExactPath;
        }

        public void AddNode(Path path, PathTreeNode<TValue> node)
        {
            Path? nextPath = path.NextPath();
            if (nextPath == null)
                m_Children[path.CurrentPath] = node;
            else if (m_Children.TryGetValue(path.CurrentPath, out PathTreeNode<TValue>? childNode))
                childNode.AddNode(nextPath, node);
            else
            {
                PathTreeNode<TValue> newNode = new();
                newNode.AddNode(nextPath, node);
                m_Children[path.CurrentPath] = newNode;
            }
        }

        public void AddValue(Path path, TValue value, bool needExactPath)
        {
            if (path.IsEmpty())
            {
                m_Value = value;
                m_NeedExactPath = needExactPath;
                return;
            }
            Path? nextPath = path.NextPath();
            if (nextPath == null)
            {
                if (m_Children.TryGetValue(path.CurrentPath, out PathTreeNode<TValue>? childNode))
                {
                    childNode.m_Value = value;
                    childNode.m_NeedExactPath = needExactPath;
                }
                else
                    m_Children[path.CurrentPath] = new(value, needExactPath);
            }
            else if (m_Children.TryGetValue(path.CurrentPath, out PathTreeNode<TValue>? node))
                node.AddValue(nextPath, value, needExactPath);
            else
            {
                PathTreeNode<TValue> newNode = new();
                newNode.AddValue(nextPath, value, needExactPath);
                m_Children[path.CurrentPath] = newNode;
            }
        }

        public TValue? GetValue(Path path)
        {
            TValue? ret = default;
            Path? nextPath = path.NextPath();
            if (m_Children.TryGetValue(path.CurrentPath, out PathTreeNode<TValue>? node))
            {
                if (nextPath == null)
                    ret = node.m_Value;
                else
                    ret = node.GetValue(nextPath);
            }
            if (ret == null && !m_NeedExactPath)
                ret = m_Value;
            return ret;
        }

        public List<KeyValuePair<Path, TValue>> Flatten(Path root)
        {
            List<KeyValuePair<Path, TValue>> list = [];
            if (m_Value != null)
                list.Add(new(root, m_Value));
            foreach (var pair in m_Children)
            {
                PathTreeNode<TValue> child = pair.Value;
                Path childPath = root.Append(pair.Key);
                list.AddRange(child.Flatten(childPath));
            }
            return list;
        }
    }
}
