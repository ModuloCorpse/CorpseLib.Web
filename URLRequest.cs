using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;
using CorpseLib.Web.OAuth;

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
            m_RequestHeaderFields["Host"] = url.Host;
        }

        public URLRequest(URI url, Request.MethodType method): this(url) => m_Method = method;
        public URLRequest(URI url, Request.MethodType method, string content): this(url, method) => m_Content = content;

        public URLRequest(URI url, Request.MethodType method, JObject content) : this(url, method)
        {
            m_Content = content.ToNetworkString();
            m_RequestHeaderFields["Content-Type"] = MIME.APPLICATION.JSON.ToString();
        }

        public void AddHeaderField(string field, string value) => m_RequestHeaderFields[field] = value;

        public void AddContentType(MIME mime) => m_RequestHeaderFields["Content-Type"] = mime.ToString();

        public void AddRefreshToken(RefreshToken token)
        {
            m_RequestHeaderFields["Authorization"] = string.Format("Bearer {0}", token.AccessToken);
            m_RequestHeaderFields["Client-Id"] = token.ClientID;
        }

        public Request Request
        {
            get
            {
                Request request = new(m_Method, m_URL.FullPath, m_Content);
                request["Host"] = m_URL.Host;
                foreach (var field in m_RequestHeaderFields)
                    request[field.Key] = field.Value;
                return request;
            }
        }
        public Response Send() => Send(TimeSpan.FromSeconds(60));

        public Response Send(TimeSpan timeout)
        {
            TCPSyncClient client = new(new HttpProtocol(), m_URL);
            client.SetReadTimeout((int)timeout.TotalMilliseconds);
            if (client.Connect())
            {
                client.Send(Request);
                DateTime requestTime = DateTime.Now;
                List<object> packets;
                do
                {
                    packets = client.Read();
                    foreach (object packet in packets)
                    {
                        if (packet is Response response)
                            return response;
                    }
                    TimeSpan timeSinceRequestSent = DateTime.Now - requestTime;
                    if (timeSinceRequestSent > timeout)
                        return new(530, "Site is frozen", "No packet received, maybe the server is freezed ?");
                } while (packets.Count == 0);
            }
            return new(503, "Service Unavailable", "Cannot connect to the server");
        }
    }
}
