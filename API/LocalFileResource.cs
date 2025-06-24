using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class LocalFileResource(string filePath, MIME? mime = null) : AHTTPEndpoint()
    {
        private readonly MIME? m_MIME = mime;
        private readonly string m_FilePath = filePath;

        protected override Response OnGetRequest(Request request)
        {
            if (File.Exists(m_FilePath))
            {
                MIME? mime = m_MIME ?? MIME.GetMIME(m_FilePath);
                if (mime != null)
                    return new Response(200, "Ok", File.ReadAllBytes(m_FilePath), mime);
                return new Response(200, "Ok", File.ReadAllBytes(m_FilePath));
            }
            return new(404, "Not Found", $"{request.Path} does not exist");
        }
    }
}
