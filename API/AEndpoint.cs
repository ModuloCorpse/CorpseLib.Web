namespace CorpseLib.Web.API
{
    public abstract class AEndpoint(Http.Path path, bool needExactPath)
    {
        private readonly Http.Path m_Path = path;
        private readonly bool m_NeedExactPath = needExactPath;
        public Http.Path Path => m_Path;
        public bool NeedExactPath => m_NeedExactPath;
    }
}
