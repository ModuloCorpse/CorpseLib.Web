namespace CorpseLib.Web.API
{
    public class PathTree<TValue>
    {
        private readonly Dictionary<string, PathTree<TValue>> m_Children = [];
        private readonly TValue? m_Value;
        private readonly bool m_NeedExactPath = true;

        public PathTree() { }
        private PathTree(TValue value, bool needExactPath)
        {
            m_Value = value;
            m_NeedExactPath = needExactPath;
        }

        public void AddValue(Http.Path path, TValue value, bool needExactPath)
        {
            Http.Path? nextPath = path.NextPath();
            if (nextPath == null)
                m_Children[path.CurrentPath] = new(value, needExactPath);
            else if (m_Children.TryGetValue(path.CurrentPath, out PathTree<TValue>? node))
                node.AddValue(nextPath, value, needExactPath);
            else
            {
                PathTree<TValue> newNode = new();
                newNode.AddValue(nextPath, value, needExactPath);
                m_Children[path.CurrentPath] = newNode;
            }
        }

        public TValue? GetValue(Http.Path path)
        {
            TValue? ret = default;
            Http.Path? nextPath = path.NextPath();
            if (m_Children.TryGetValue(path.CurrentPath, out PathTree<TValue>? node))
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
    }
}
