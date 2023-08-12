using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web
{
    public class URLRequest
    {
        private readonly URI m_URL;
        private readonly Dictionary<string, string> m_RequestHeaderFields = new();
        private readonly Request.MethodType m_Method = Request.MethodType.GET;
        private readonly string m_Content = string.Empty;

        public URLRequest(URI url)
        {
            m_URL = url;
        }

        public URLRequest(URI url, Request.MethodType method)
        {
            m_URL = url;
            m_Method = method;
        }

        public URLRequest(URI url, Request.MethodType method, string content)
        {
            m_URL = url;
            m_Method = method;
            m_Content = content;
        }

        public URLRequest(URI url, Request.MethodType method, JObject content)
        {
            m_URL = url;
            m_Method = method;
            m_Content = content.ToNetworkString();
            m_RequestHeaderFields["Content-Type"] = MIME.APPLICATION.JSON.ToString();
        }

        public void AddHeaderField(string field, string value)
        {
            m_RequestHeaderFields[field] = value;
        }

        public void AddContentType(MIME mime) => m_RequestHeaderFields["Content-Type"] = mime.ToString();

        public Request Request
        {
            get
            {
                Request request = new(m_Method, m_URL.Path, m_Content);
                request["Host"] = m_URL.Host;
                foreach (var field in m_RequestHeaderFields)
                    request[field.Key] = field.Value;
                return request;
            }
        }

        public Response Send()
        {
            TCPSyncClient client = new(new HttpProtocol(), m_URL);
            if (client.Connect())
            {
                client.Send(Request);
                List<object> packets = client.Read();
                foreach (object packet in packets)
                {
                    if (packet is Response response)
                        return response;
                }
            }
            return new(503, "Service Unavailable", "Cannot connect to the server");
        }
    }
}
