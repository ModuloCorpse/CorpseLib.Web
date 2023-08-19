using CorpseLib.Serialize;
using System.Text;

namespace CorpseLib.Web.Http
{
    //TODO Debug chunked packet
    [DefaultSerializer]
    public class HttpSerializer : BytesSerializer<AMessage>
    {
        private readonly ChunkedMessageBuilder m_ChunkedMessageBuilder = new();
        private readonly MessageBuilder m_MessageBuilder = new();

        private OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            if (message.HaveHeaderField("Transfer-Encoding") && ((string)message["Transfer-Encoding"]).ToLower().Contains("chunked"))
                return m_ChunkedMessageBuilder.HandleMessage(message, reader);
            else if (message.HaveHeaderField("Content-Length"))
                return m_MessageBuilder.HandleMessage(message, reader);
            else
                return new(message);
        }

        protected override OperationResult<AMessage> Deserialize(BytesReader reader)
        {
            while (reader.IndexOf(new byte[] { 13, 10 }) == 0)
                reader.ReadBytes(2);
            if (!reader.CanRead())
                return new(null);
            if (m_ChunkedMessageBuilder.IsHoldingMessage)
                return m_ChunkedMessageBuilder.HandleHeldMessage(reader);
            if (m_MessageBuilder.IsHoldingMessage)
                return m_MessageBuilder.HandleHeldMessage(reader);
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
