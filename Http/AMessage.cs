using System.Text;

namespace CorpseLib.Web.Http
{
    public abstract class AMessage
    {
        private readonly Dictionary<string, object> m_Fields = [];
        private readonly Dictionary<string, object> m_LowerFields = [];
        private byte[] m_Body = [];

        internal Dictionary<string, object> Fields => m_Fields;
        internal string Header => GetHeader();

        private void SetField(string key, object value)
        {
            m_Fields[key] = value;
            m_LowerFields[key.ToLower()] = value;
        }

        private object GetField(string key) => m_LowerFields[key.ToLower()];

        internal void SetBody(string body)
        {
            m_Body = Encoding.UTF8.GetBytes(body);
            if (m_Body.Length > 0)
            {
                SetField("Accept-Ranges", "bytes");
                SetField("Content-Length", m_Body.Length);
            }
        }

        internal void SetBody(byte[] body)
        {
            m_Body = body;
            if (m_Body.Length > 0)
            {
                SetField("Accept-Ranges", "bytes");
                SetField("Content-Length", m_Body.Length);
            }
        }

        protected void ParseHeaderFields(List<string> fields)
        {
            foreach (var field in fields)
            {
                int position = field.IndexOf(": ");
                if (position >= 0)
                {
                    string fieldName = field[..position];
                    string fieldValue = field[(position + 2)..];
                    SetField(fieldName, fieldValue);
                }
            }
        }

        /// <summary>
        /// Check if the message contains the given header field
        /// </summary>
        /// <param name="headerFieldName">Name of the header field to search</param>
        /// <returns>True if the header field exists</returns>
        public bool HaveHeaderField(string headerFieldName) => m_LowerFields.ContainsKey(headerFieldName.ToLower());

        /// <summary>
        /// Body of the message as a string
        /// </summary>
        public string Body => Encoding.UTF8.GetString(m_Body);
        /// <summary>
        /// Body of the message as bytes
        /// </summary>
        public byte[] RawBody => m_Body;
        /// <summary>
        /// Header field accessors
        /// </summary>
        /// <param name="key">Name of the header field to get/set</param>
        /// <returns>The content of the header field</returns>
        public object this[string key]
        {
            get => GetField(key);
            set => SetField(key, value);
        }

        /// <returns>The formatted message</returns>
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(GetHeader());
            builder.Append("\r\n");
            foreach (var field in m_Fields)
            {
                builder.Append(field.Key);
                builder.Append(": ");
                builder.Append(field.Value.ToString());
                builder.Append("\r\n");
            }
            builder.Append("\r\n");
            builder.Append(Encoding.UTF8.GetString(m_Body));
            return builder.ToString();
        }

        protected abstract string GetHeader();
    }
}
