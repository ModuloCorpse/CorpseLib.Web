using CorpseLib.Serialize;
using System.Text;

namespace CorpseLib.Web.Http
{
    internal class ChunkedMessageBuilder
    {
        private bool m_HoldingMessage = false;
        private bool m_IsHexa = true;
        private int m_ChunkSize = 0;
        private int m_CurrentChunkSize = 0;
        private byte[] m_ChunkBuilder = [];
        private AMessage? m_HeldMessage = null;

        public bool IsHoldingMessage => m_HoldingMessage;

        private string? HandleBodyRead(BytesReader reader)
        {
            while (reader.CanRead())
            {
                byte[] chunk;
                int position = reader.IndexOf("\r\n"u8.ToArray());
                while (position == 0)
                {
                    reader.ReadBytes(2);
                    position = reader.IndexOf("\r\n"u8.ToArray());
                }

                if (position != -1)
                    chunk = reader.ReadBytes(position);
                else
                {
                    if (!reader.CanRead())
                        return null;
                    chunk = reader.ReadAll();
                }
                if (m_IsHexa)
                {
                    string hexaStr = Encoding.UTF8.GetString(chunk);
                    m_ChunkSize = int.Parse(hexaStr, System.Globalization.NumberStyles.HexNumber);
                    if (m_ChunkSize == 0)
                    {
                        string bodyContent = Encoding.UTF8.GetString(m_ChunkBuilder);
                        m_ChunkBuilder = [];
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

        public OperationResult<AMessage> HandleHeldMessage(BytesReader reader)
        {
            string? body = HandleBodyRead(reader);
            if (body != null)
            {
                m_HoldingMessage = false;
                m_HeldMessage!.SetBody(body);
                return new(m_HeldMessage);
            }
            return new(null);
        }

        public OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            string? body = HandleBodyRead(reader);
            if (body != null)
            {
                message.SetBody(body);
                return new(message);
            }
            m_HoldingMessage = true;
            m_HeldMessage = message;
            return new(null);
        }
    }
}
