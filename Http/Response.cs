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
            List<string> attributes = response.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
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
        /// <param name="body">Body of the response (empty by default)</param>
        /// <param name="contentType">MIME type of the content to send (null by default)</param>
        public Response(int statusCode, string statusMessage, string body = "", MIME? contentType = null)
        {
            m_Version = "HTTP/1.1";
            m_StatusCode = statusCode;
            m_StatusMessage = statusMessage;
            SetBody(body);
            if (!string.IsNullOrWhiteSpace(body) && contentType != null)
            {
                if (contentType.HaveParameter())
                    base["Content-Type"] = contentType.ToString();
                else
                    base["Content-Type"] = string.Format("{0}; charset=utf-8", contentType);
            }
        }

        protected override string GetHeader() => string.Format("{0} {1} {2}", m_Version, m_StatusCode, m_StatusMessage);

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
    }
}
