using CorpseLib.Serialize;

namespace CorpseLib.Web.Http
{
    internal class MessageBuilder
    {
        private AMessage? m_HeldMessage = null;
        private byte[] m_HeldBody = [];
        private int m_ContentLength = 0;
        private bool m_HoldingMessage = false;

        public bool IsHoldingMessage => m_HoldingMessage;

        public OperationResult<AMessage> HandleHeldMessage(BytesReader reader)
        {
            int remainingBytes = m_ContentLength - m_HeldBody.Length;
            byte[] body;
            if (reader.Length < remainingBytes)
                body = reader.ReadAll();
            else
                body = reader.ReadBytes(remainingBytes);
            byte[] tmp = new byte[m_HeldBody.Length + body.Length];
            m_HeldBody.CopyTo(tmp, 0);
            body.CopyTo(tmp, m_HeldBody.Length);
            m_HeldBody = tmp;
            if (m_HeldBody.Length == m_ContentLength)
            {
                m_HeldMessage!.SetBody(m_HeldBody);
                m_HoldingMessage = false;
                m_HeldBody = [];
                return new(m_HeldMessage);
            }
            return new(null);
        }

        public OperationResult<AMessage> HandleMessage(AMessage message, BytesReader reader)
        {
            int contentLength = int.Parse((string)message["Content-Length"]);
            if (reader.Length > contentLength)
            {
                message.SetBody(reader.ReadBytes(contentLength));
                return new(message);
            }
            m_ContentLength = contentLength;
            m_HoldingMessage = true;
            m_HeldMessage = message;
            m_HeldBody = [];
            return new(null);
        }
    }
}
