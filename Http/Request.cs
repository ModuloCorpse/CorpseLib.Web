namespace CorpseLib.Web.Http
{
    /// <summary>
    /// An HTTP request
    /// </summary>
    public class Request : AMessage
    {
        public enum MethodType
        {
            GET,
            HEAD,
            POST,
            PUT,
            DELETE,
            CONNECT,
            OPTIONS,
            TRACE,
            PATCH,
            UNDEFINED
        }

        private readonly MethodType m_Method;
        private readonly string m_Path;
        private readonly string m_Version;
        private readonly Dictionary<string, string> m_Parameters = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="request">Content of the received HTTP request</param>
        public Request(string request)
        {
            List<string> attributes = request.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            string[] requestLine = attributes[0].Trim().Split(' ');

            m_Method = (requestLine[0]) switch
            {
                "GET" => MethodType.GET,
                "HEAD" => MethodType.HEAD,
                "POST" => MethodType.POST,
                "PUT" => MethodType.PUT,
                "DELETE" => MethodType.DELETE,
                "CONNECT" => MethodType.CONNECT,
                "OPTIONS" => MethodType.OPTIONS,
                "TRACE" => MethodType.TRACE,
                "PATCH" => MethodType.PATCH,
                _ => MethodType.UNDEFINED
            };
            m_Version = requestLine[2];
            attributes.RemoveAt(0);
            ParseHeaderFields(attributes);
            string path = requestLine[1];
            int parametersIdx = path.IndexOf('?');
            if (parametersIdx < 0)
                m_Path = path;
            else
            {
                m_Path = path[..parametersIdx];
                string parameterLine = path[(parametersIdx + 1)..];
                string[] parameters = parameterLine.Split('&');
                foreach (string parameter in parameters)
                {
                    string[] tmp = parameter.Split('=');
                    if (tmp.Length == 2)
                        m_Parameters[tmp[0]] = tmp[1];
                }
            }

            if (m_Path[^1] == '/')
                m_Path = m_Path[0..^1];
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">Method of the request</param>
        /// <param name="path">URL targeted by the request</param>
        /// <param name="body">Body of the request</param>
        public Request(MethodType method, string path, string body = "")
        {
            m_Method = method;
            m_Path = path;
            m_Version = "HTTP/1.1";
            SetBody(body);
        }

        protected override string GetHeader() => m_Method switch
        {
            MethodType.GET => string.Format("GET {0} {1}", m_Path, m_Version),
            MethodType.HEAD => string.Format("HEAD {0} {1}", m_Path, m_Version),
            MethodType.POST => string.Format("POST {0} {1}", m_Path, m_Version),
            MethodType.PUT => string.Format("PUT {0} {1}", m_Path, m_Version),
            MethodType.DELETE => string.Format("DELETE {0} {1}", m_Path, m_Version),
            MethodType.CONNECT => string.Format("CONNECT {0} {1}", m_Path, m_Version),
            MethodType.OPTIONS => string.Format("OPTIONS {0} {1}", m_Path, m_Version),
            MethodType.TRACE => string.Format("TRACE {0} {1}", m_Path, m_Version),
            MethodType.PATCH => string.Format("PATCH {0} {1}", m_Path, m_Version),
            _ => throw new ArgumentException()
        };
        /// <summary>
        /// Check if the request contains the given URL parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>True if the URL parameter exists</returns>
        public bool HaveParameter(string parameterName) { return m_Parameters.ContainsKey(parameterName); }

        /// <summary>
        /// Get the URL parameter value of the given parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>The value of the URL parameter</returns>
        public string GetParameter(string parameterName) { return m_Parameters[parameterName]; }

        /// <summary>
        /// Get the URL parameter value of the given parameter if it exist
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <param name="value">Container for the value of the parameter if found</param>
        /// <returns>True if it found a value to the given parameter</returns>
        public bool TryGetParameter(string parameterName, out string? value)
        {
            return m_Parameters.TryGetValue(parameterName, out value);
        }

        /// <summary>
        /// Method of the request
        /// </summary>
        public MethodType Method { get => m_Method; }
        /// <summary>
        /// URL targeted by the request
        /// </summary>
        public string Path { get => m_Path; }
        /// <summary>
        /// HTTP version of the request
        /// </summary>
        public string Version { get => m_Version; }
    }
}
