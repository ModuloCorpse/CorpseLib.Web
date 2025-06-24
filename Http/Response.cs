using System.Text;

namespace CorpseLib.Web.Http
{
    /// <summary>
    /// An HTTP response
    /// </summary>
    public class Response : AMessage
    {
        private readonly string m_Version;
        private readonly int m_StatusCode;
        private readonly string m_StatusMessage;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="response">Content of the received HTTP response</param>
        public Response(string response)
        {
            List<string> attributes = [.. response.Split(separator, StringSplitOptions.None)];
            string statusLine = attributes[0].Trim();
            int indexOfSeparator = statusLine.IndexOf(' ');
            m_Version = statusLine[..indexOfSeparator];
            statusLine = statusLine[(indexOfSeparator + 1)..];
            indexOfSeparator = statusLine.IndexOf(' ');
            m_StatusCode = int.Parse(statusLine[..indexOfSeparator]);
            m_StatusMessage = statusLine[(indexOfSeparator + 1)..];
            attributes.RemoveAt(0);
            ParseHeaderFields(attributes);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        /// <param name="contentType">MIME type of the content to send</param>
        public Response(int statusCode, string statusMessage, byte[] body, MIME contentType)
        {
            m_Version = "HTTP/1.1";
            m_StatusCode = statusCode;
            m_StatusMessage = statusMessage;
            SetBody(body);
            if (body.Length > 0)
            {
                if (contentType.HaveParameter())
                    base["Content-Type"] = contentType.ToString();
                else
                    base["Content-Type"] = $"{contentType}; charset=utf-8";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        public Response(int statusCode, string statusMessage, byte[] body)
        {
            m_Version = "HTTP/1.1";
            m_StatusCode = statusCode;
            m_StatusMessage = statusMessage;
            SetBody(body);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        /// <param name="contentType">MIME type of the content to send</param>
        public Response(int statusCode, string statusMessage, string body, MIME contentType) : this(statusCode, statusMessage, Encoding.UTF8.GetBytes(body), contentType) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        /// <param name="body">Body of the response</param>
        public Response(int statusCode, string statusMessage, string body) : this(statusCode, statusMessage, Encoding.UTF8.GetBytes(body)) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="statusCode">Status code of the response</param>
        /// <param name="statusMessage">Status message of the response</param>
        public Response(int statusCode, string statusMessage) : this(statusCode, statusMessage, Array.Empty<byte>()) { }

        protected override string GetHeader() => $"{m_Version} {m_StatusCode} {m_StatusMessage}";

        /// <summary>
        /// HTTP version of the response
        /// </summary>
        public string Version { get => m_Version; }

        /// <summary>
        /// Status code of the response
        /// </summary>
        public int StatusCode { get => m_StatusCode; }

        /// <summary>
        /// Status message of the response
        /// </summary>
        public string StatusMessage { get => m_StatusMessage; }

        private static readonly string[] separator = ["\r\n"];
    }
}
