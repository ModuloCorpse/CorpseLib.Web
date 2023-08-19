using CorpseLib.Serialize;

namespace CorpseLib.Web.Http
{
    internal class MessageBuilder
    {
        private bool m_HoldingMessage = false;
        private int m_ContentLength = 0;
        private AMessage? m_HeldMessage = null;

        public bool IsHoldingMessage => m_HoldingMessage;

        public OperationResult<AMessage> HandleHeldMessage(BytesReader reader)
        {
            if (reader.Length < m_ContentLength)
                return new(null);
            m_HeldMessage!.SetBody(reader.ReadString(m_ContentLength));
            m_HoldingMessage = false;
            return new(m_HeldMessage);
        }

        public OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            int contentLength = int.Parse((string)message["Content-Length"]);
            if (reader.Length > contentLength)
            {
                message.SetBody(reader.ReadString(contentLength));
                return new(message);
            }
            m_ContentLength = contentLength;
            m_HoldingMessage = true;
            m_HeldMessage = message;
            return new(null);
        }
    }
}
