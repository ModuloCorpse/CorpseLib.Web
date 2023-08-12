using CorpseLib.Serialize;
using System.Text;

namespace CorpseLib.Web.Http
{
    [DefaultSerializer]
    public class HttpSerializer : BytesSerializer<AMessage>
    {
        private bool m_HoldMessage = false;
        private bool m_IsHexa = true;
        private int m_ChunkSize = 0;
        private int m_CurrentChunkSize = 0;
        private byte[] m_ChunkBuilder = Array.Empty<byte>();
        private AMessage? m_HoldingMessage = null;

        private string? HandleBodyRead(BytesReader reader)
        {
            while (reader.CanRead())
            {
                byte[] chunk;
                int position = reader.IndexOf(new byte[] { 13, 10 });
                while (position == 0)
                {
                    reader.ReadBytes(2);
                    position = reader.IndexOf(new byte[] { 13, 10 });
                }

                if (position != -1)
                    chunk = reader.ReadBytes(position);
                else
                    chunk = reader.ReadAll();
                if (m_IsHexa)
                {
                    m_ChunkSize = int.Parse(Encoding.UTF8.GetString(chunk), System.Globalization.NumberStyles.HexNumber);
                    if (m_ChunkSize == 0)
                    {
                        string bodyContent = Encoding.UTF8.GetString(m_ChunkBuilder);
                        m_ChunkBuilder = Array.Empty<byte>();
                        return bodyContent;
                    }
                    m_IsHexa = false;
                }
                else
                {
                    int chunkLength = chunk.Length;
                    m_CurrentChunkSize += chunkLength;
                    int chunkBuilderLength = m_ChunkBuilder.Length;
                    Array.Resize(ref m_ChunkBuilder, chunkBuilderLength + chunkLength);
                    for (int i = 0; i < chunkLength; ++i)
                        m_ChunkBuilder[i + chunkBuilderLength] = chunk[i];
                    if (m_ChunkSize == m_CurrentChunkSize)
                    {
                        m_CurrentChunkSize = 0;
                        m_IsHexa = true;
                    }
                }
            }
            return null;
        }

        private OperationResult<AMessage> HandleHoldMessageBodyRead(BytesReader reader)
        {
            string? body = HandleBodyRead(reader);
            if (body != null)
            {
                m_HoldMessage = false;
                m_HoldingMessage!.SetBody(body);
                return new(m_HoldingMessage);
            }
            return new(null);
        }

        private OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            if (message.HaveHeaderField("Transfer-Encoding") && ((string)message["Transfer-Encoding"]).ToLower().Contains("chunked"))
            {
                string? body = HandleBodyRead(reader);
                if (body != null)
                {
                    message.SetBody(body);
                    return new(message);
                }
                m_HoldMessage = true;
                m_HoldingMessage = message;
                return new(null);
            }
            else if (message.HaveHeaderField("Content-Length"))
            {
                message.SetBody(reader.ReadString(int.Parse((string)message["Content-Length"])));
                return new(message);
            }
            else
                return new(message);
        }

        protected override OperationResult<AMessage> Deserialize(BytesReader reader)
        {
            while (reader.IndexOf(new byte[] { 13, 10 }) == 0)
                reader.ReadBytes(2);
            if (!reader.CanRead())
                return new(null);
            if (m_HoldMessage)
                return HandleHoldMessageBodyRead(reader);
            int position = reader.IndexOf(new byte[] { 13, 10, 13, 10 });
            if (position >= 0)
            {
                string data = reader.ReadString(position);
                reader.ReadBytes(4);
                if (data.StartsWith("HTTP"))
                    return HandleMessage(new Response(data), reader);
                else
                    return HandleMessage(new Request(data), reader);
            }
            return new(null);
        }

        protected override void Serialize(AMessage obj, BytesWriter writer) => writer.Write(obj.ToString());
    }
}
