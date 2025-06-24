namespace CorpseLib.Web
{
    /// <summary>
    /// Class representing a MIME
    /// </summary>
    public class MIME
    {
        private readonly string m_Type;
        private readonly string m_Subtype;
        private Tuple<string, string>? m_Parameter = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the MIME</param>
        /// <param name="subtype">Subtype of the MIME</param>
        public MIME(string type, string subtype)
        {
            m_Type = type;
            m_Subtype = subtype;
        }

        /// <returns>The type of the MIME</returns>
        public string GetMIMEType() { return m_Type; }

        /// <returns>The subtype of the MIME</returns>
        public string GetSubType() { return m_Subtype; }

        /// <returns>The name of the MIME's parameter</returns>
        public string GetParameterName() { return (m_Parameter != null) ? m_Parameter.Item1 : string.Empty; }

        /// <returns>The name of the MIME's value</returns>
        public string GetParameterValue() { return (m_Parameter != null) ? m_Parameter.Item2 : string.Empty; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type of the MIME</param>
        /// <param name="subtype">Subtype of the MIME</param>
        /// <param name="parameter">Parameter name of the MIME</param>
        /// <param name="value">Parameter value of the MIME</param>
        public MIME(string type, string subtype, string parameter, string value)
        {
            m_Type = type;
            m_Subtype = subtype;
            m_Parameter = new(parameter, value);
        }

        /// <returns>True if the MIME have a parameter</returns>
        public bool HaveParameter() { return m_Parameter != null; }

        /// <summary>
        /// Set the parameter of the MIME
        /// </summary>
        /// <param name="parameter">Parameter name of the MIME</param>
        /// <param name="value">Parameter value of the MIME</param>
        public void SetParameter(string parameter, string value)
        {
            m_Parameter = new(parameter, value);
        }

        /// <returns>The MIME has string</returns>
        public override string ToString()
        {
            if (m_Parameter != null)
                return $"{m_Type}/{m_Subtype};{m_Parameter.Item1}={m_Parameter.Item2}";
            return $"{m_Type}/{m_Subtype}";
        }

        /// <summary>
        /// Parse the given mime type
        /// </summary>
        /// <param name="contentType">string containing the mime type</param>
        /// <returns>Corresponding MIME type</returns>
        public static MIME? Parse(string contentType)
        {
            string[] typeSubtypeArray = contentType.Split('/');
            if (typeSubtypeArray.Length != 2)
                return null;
            if (!typeSubtypeArray[1].Contains(';'))
                return new(typeSubtypeArray[0], typeSubtypeArray[1]);

            string[] subTypeParameterArray = typeSubtypeArray[1].Split(';');
            if (subTypeParameterArray.Length == 2)
            {
                string[] parameterArray = subTypeParameterArray[1].Split('=');
                if (parameterArray.Length != 2)
                    return null;
                return new(typeSubtypeArray[0], subTypeParameterArray[0], parameterArray[0], parameterArray[1]);
            }
            else
                return null;
        }

        private static string GetExtension(string path)
        {
            string[] pathArray = path.Split('.');
            if (pathArray.Length == 0)
                return string.Empty;
            return pathArray[^1];
        }

        /// <summary>
        /// Simple mime type detection based on file extension
        /// </summary>
        public static MIME? GetMIME(string path) => GetExtension(path).ToLower() switch
        {
            "html" => TEXT.HTML,
            "js" => TEXT.JAVASCRIPT,
            "css" => TEXT.CSS,
            "webp" => IMAGE.WEBP,
            "png" => IMAGE.PNG,
            "jpg" => IMAGE.JPEG,
            "jpeg" => IMAGE.JPEG,
            _ => null
        };

        /// <summary>
        /// application/ type
        /// </summary>
        public static class APPLICATION
        {
            /// <summary>
            /// application/octet-stream
            /// </summary>
            public static readonly MIME JSON = new("application", "json");
            /// <summary>
            /// application/octet-stream
            /// </summary>
            public static readonly MIME OCTET_STREAM = new("application", "octet-stream");
            /// <summary>
            /// application/javascript
            /// </summary>
            [ObsoleteAttribute()]
            public static readonly MIME JAVASCRIPT = new("application", "javascript");
            /// <summary>
            /// application/ecmascript
            /// </summary>
            [ObsoleteAttribute()]
            public static readonly MIME ECMASCRIPT = new("application", "ecmascript");
            /// <summary>
            /// application/ogg
            /// </summary>
            public static readonly MIME OGG = new("application", "ogg");
            /// <summary>
            /// application/ogg
            /// </summary>
            public static readonly MIME X_WWW_FORM_URLENCODED = new ("application", "x-www-form-urlencoded");
        }

        /// <summary>
        /// text/ type
        /// </summary>
        public static class TEXT
        {
            /// <summary>
            /// text/plain
            /// </summary>
            public static readonly MIME PLAIN = new("text", "plain");
            /// <summary>
            /// text/css
            /// </summary>
            public static readonly MIME CSS = new("text", "css");
            /// <summary>
            /// text/html
            /// </summary>
            public static readonly MIME HTML = new("text", "html");
            /// <summary>
            /// text/javascript
            /// </summary>
            public static readonly MIME JAVASCRIPT = new("text", "javascript");
            /// <summary>
            /// text/ecmascript
            /// </summary>
            [ObsoleteAttribute()]
            public static readonly MIME ECMASCRIPT = new("text", "ecmascript");
        }

        /// <summary>
        /// image/ type
        /// </summary>
        public static class IMAGE
        {
            /// <summary>
            /// image/apng
            /// </summary>
            public static readonly MIME APNG = new("image", "apng");
            /// <summary>
            /// image/avif
            /// </summary>
            public static readonly MIME AVIF = new("image", "avif");
            /// <summary>
            /// image/gif
            /// </summary>
            public static readonly MIME GIF = new("image", "gif");
            /// <summary>
            /// image/jpeg
            /// </summary>
            public static readonly MIME JPEG = new("image", "jpeg");
            /// <summary>
            /// image/png
            /// </summary>
            public static readonly MIME PNG = new("image", "png");
            /// <summary>
            /// image/svg+xml
            /// </summary>
            public static readonly MIME SVG_XML = new("image", "svg+xml");
            /// <summary>
            /// image/webp
            /// </summary>
            public static readonly MIME WEBP = new("image", "webp");
        }

        /// <summary>
        /// audio/ type
        /// </summary>
        public static class AUDIO
        {
            /// <summary>
            /// audio/wave
            /// </summary>
            public static readonly MIME WAVE = new("audio", "wave");
            /// <summary>
            /// audio/wav
            /// </summary>
            public static readonly MIME WAV = new("audio", "wav");
            /// <summary>
            /// audio/x-wave
            /// </summary>
            public static readonly MIME X_WAVE = new("audio", "x-wave");
            /// <summary>
            /// audio/x-wav
            /// </summary>
            public static readonly MIME X_WAV = new("audio", "x-wav");
            /// <summary>
            /// audio/webm
            /// </summary>
            public static readonly MIME WEBM = new("audio", "webm");
            /// <summary>
            /// audio/ogg
            /// </summary>
            public static readonly MIME OGG = new("audio", "ogg");
        }

        /// <summary>
        /// video/ type
        /// </summary>
        public static class VIDEO
        {
            /// <summary>
            /// video/webm
            /// </summary>
            public static readonly MIME WEBM = new("video", "webm");
            /// <summary>
            /// video/ogg
            /// </summary>
            public static readonly MIME OGG = new("video", "ogg");
        }
    }
}
