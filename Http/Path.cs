using CorpseLib.Network;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CorpseLib.Web.Http
{
    public class Path
    {
        private readonly Dictionary<string, string?> m_Data = [];
        private readonly string[] m_SplittedPath;
        private readonly string m_FullPath;

        public string CurrentPath => (m_SplittedPath.Length > 0) ? m_SplittedPath[0] : string.Empty;
        public string FullPath => m_FullPath;

        public string[] Paths => m_SplittedPath;

        public string this[string key]
        {
            get => GetParameter(key);
            set => AddParameter(key, value);
        }

        private string GenerateFullPath()
        {
            StringBuilder builder = new();
            foreach (string s in m_SplittedPath)
            {
                builder.Append('/');
                builder.Append(s);
            }
            return builder.ToString();
        }

        private Path(string[] paths, Dictionary<string, string?> data)
        {
            m_SplittedPath = paths;
            m_Data = data;
            m_FullPath = GenerateFullPath();
        }

        public Path()
        {
            m_SplittedPath = [];
            m_FullPath = string.Empty;
        }

        public Path(string path)
        {
            while (path.StartsWith('/'))
                path = path[1..];
            int parametersIdx = path.IndexOf('?');
            if (parametersIdx < 0)
                m_SplittedPath = path.Split('/');
            else
            {
                string clearedPath = path[..parametersIdx];
                if (!string.IsNullOrEmpty(clearedPath))
                    m_SplittedPath = clearedPath.Split('/');
                else
                    m_SplittedPath = [];
                string parameterLine = path[(parametersIdx + 1)..];
                string[] datas = parameterLine.Split('&');
                foreach (string s in datas)
                {
                    int n = s.IndexOf('=');
                    if (n >= 0)
                        AddParameter(s[..n], s[(n + 1)..]);
                    else
                        m_Data[s] = null;
                }
            }
            
            m_FullPath = GenerateFullPath();
        }

        public Path? NextPath()
        {
            if (m_SplittedPath.Length == 1)
                return null;
            return new(m_SplittedPath.Skip(1).ToArray(), m_Data);
        }

        public void AddParameter(string key, string value) => m_Data[key] = value;

        public void AddParameters(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string, string> attr in attributes)
                AddParameter(attr.Key, attr.Value);
        }

        /// <summary>
        /// Check if the request contains the given URL parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>True if the URL parameter exists</returns>
        public bool HaveParameter(string parameterName) => m_Data.ContainsKey(parameterName);

        /// <summary>
        /// Get the URL parameter value of the given parameter
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <returns>The value of the URL parameter</returns>
        public string GetParameter(string parameterName) => m_Data[parameterName] ?? string.Empty;

        /// <summary>
        /// Get the URL parameter value of the given parameter if it exist
        /// </summary>
        /// <param name="parameterName">Name of the URL parameter to search</param>
        /// <param name="value">Container for the value of the parameter if found</param>
        /// <returns>True if it found a value to the given parameter</returns>
        public bool TryGetParameter(string parameterName, [NotNullWhen(true)] out string? value)
        {
            if (m_Data.TryGetValue(parameterName, out string? ret))
            {
                value = ret ?? string.Empty;
                return true;
            }
            value = null;
            return false;
        }

        public override string ToString()
        {
            if (m_Data.Count == 0)
                return m_FullPath;
            StringBuilder builder = new(m_FullPath);
            builder.Append('?');
            uint i = 0;
            foreach (KeyValuePair<string, string?> data in m_Data)
            {
                if (i != 0)
                    builder.Append('&');
                builder.Append(data.Key);
                if (data.Value != null)
                {
                    builder.Append('=');
                    builder.Append(data.Value);
                }
                ++i;
            }
            return builder.ToString();
        }
    }
}
