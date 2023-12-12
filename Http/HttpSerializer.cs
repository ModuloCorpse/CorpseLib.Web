using CorpseLib.Serialize;

namespace CorpseLib.Web.Http
{
    public class HttpSerializer : ABytesSerializer<AMessage>
    {
        private readonly ChunkedMessageBuilder m_ChunkedMessageBuilder = new();
        private readonly MessageBuilder m_MessageBuilder = new();

        private OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            if (message.HaveHeaderField("Transfer-Encoding") && ((string)message["Transfer-Encoding"]).Contains("chunked", StringComparison.CurrentCultureIgnoreCase))
                return m_ChunkedMessageBuilder.HandleMessage(message, reader);
            else if (message.HaveHeaderField("Content-Length"))
                return m_MessageBuilder.HandleMessage(message, reader);
            else
                return new(message);
        }

        protected override OperationResult<AMessage> Deserialize(BytesReader reader)
        {
            while (reader.IndexOf("\r\n"u8.ToArray()) == 0)
                reader.ReadBytes(2);
            if (!reader.CanRead())
                return new(null);
            if (m_ChunkedMessageBuilder.IsHoldingMessage)
                return m_ChunkedMessageBuilder.HandleHeldMessage(reader);
            if (m_MessageBuilder.IsHoldingMessage)
                return m_MessageBuilder.HandleHeldMessage(reader);
            int position = reader.IndexOf("\r\n\r\n"u8.ToArray());
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

        protected override void Serialize(AMessage obj, BytesWriter writer)
        {
            writer.Write(obj.Header);
            writer.Write("\r\n");
            foreach (var field in obj.Fields)
            {
                writer.Write(field.Key);
                writer.Write(": ");
                writer.Write(field.Value.ToString() ?? string.Empty);
                writer.Write("\r\n");
            }
            writer.Write("\r\n");
            writer.Write(obj.RawBody);
        }
    }
}
