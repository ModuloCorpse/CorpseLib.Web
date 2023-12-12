namespace CorpseLib.Web.API
{
    public abstract class AEndpoint
    {
        private readonly Http.Path m_Path;
        private readonly bool m_NeedExactPath;

        public Http.Path Path => m_Path;
        public bool NeedExactPath => m_NeedExactPath;

        protected AEndpoint(Http.Path path, bool needExactPath)
        {
            m_Path = path;
            m_NeedExactPath = needExactPath;
        }
    }
}
