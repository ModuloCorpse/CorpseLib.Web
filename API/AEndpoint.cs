namespace CorpseLib.Web.API
{
    public abstract class AEndpoint
    {
        private readonly string m_Path;
        private readonly bool m_NeedExactPath;

        public string Path => m_Path;
        public bool NeedExactPath => m_NeedExactPath;

        protected AEndpoint(string path, bool needExactPath)
        {
            m_Path = (path.Length == 0 || path[0] != '/') ? "/" + path : path;
            m_NeedExactPath = needExactPath;
        }
    }
}
