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

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return [];
            return path.Split('/');
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
            while (path.EndsWith('/'))
                path = path[..^1];
            int parametersIdx = path.IndexOf('?');
            if (parametersIdx < 0)
                m_SplittedPath = SplitPath(path);
            else
            {
                string clearedPath = path[..parametersIdx];
                if (!string.IsNullOrEmpty(clearedPath))
                    m_SplittedPath = SplitPath(clearedPath);
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

        public bool IsEmpty() => m_SplittedPath.Length == 0;

        public Path? NextPath()
        {
            if (m_SplittedPath.Length <= 1)
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
        public bool TryGetParameter(string parameterName, [MaybeNullWhen(false)] out string? value)
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
            string fullpath = (string.IsNullOrEmpty(m_FullPath)) ? "/" : m_FullPath;
            if (m_Data.Count == 0)
                return fullpath;
            StringBuilder builder = new(fullpath);
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

        public Path Duplicate()
        {
            string[] duplicatedPath = new string[m_SplittedPath.Length];
            m_SplittedPath.CopyTo(duplicatedPath, 0);
            Dictionary<string, string?> duplicatedData = [];
            foreach (var pairA in m_Data)
                duplicatedData[pairA.Key] = pairA.Value;
            return new(duplicatedPath, duplicatedData);
        }

        public static Path Append(Path a, Path b)
        {
            string[] concatenatedPath = new string[a.m_SplittedPath.Length + b.m_SplittedPath.Length];
            a.m_SplittedPath.CopyTo(concatenatedPath, 0);
            b.m_SplittedPath.CopyTo(concatenatedPath, a.m_SplittedPath.Length);
            Dictionary<string, string?> concatenatedData = [];
            foreach (var pairA in a.m_Data)
                concatenatedData[pairA.Key] = pairA.Value;
            foreach (var pairB in b.m_Data)
                concatenatedData[pairB.Key] = pairB.Value;
            return new(concatenatedPath, concatenatedData);
        }

        public Path Append(string b)
        {
            string[] concatenatedPath = new string[m_SplittedPath.Length + 1];
            m_SplittedPath.CopyTo(concatenatedPath, 0);
            concatenatedPath[m_SplittedPath.Length] = b;
            return new(concatenatedPath, []);
        }
    }
}
