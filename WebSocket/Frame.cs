using System.Text;

namespace CorpseLib.Web.WebSocket
{
    /// <summary>
    /// Class representing a websocket frame
    /// </summary>
    public class Frame
    {
        private readonly bool m_Fin;
        private readonly bool m_Rsv1; //Not used for now
        private readonly bool m_Rsv2; //Not used for now
        private readonly bool m_Rsv3; //Not used for now
        private int m_OpCode;
        private bool m_UseMask;
        private byte[] m_Mask = new byte[4];
        private readonly short m_StatusCode;
        private readonly byte[] m_Content;

        internal Frame(bool fin, bool rsv1, bool rsv2, bool rsv3, int opCode, bool useMask, byte[] mask, short statusCode, byte[] content)
        {
            m_Fin = fin;
            m_Rsv1 = rsv1;
            m_Rsv2 = rsv2;
            m_Rsv3 = rsv3;
            m_OpCode = opCode;
            m_UseMask = useMask;
            m_Mask = mask;
            m_StatusCode = statusCode;
            m_Content = content;
        }

        public Frame(bool fin, int opcode, byte[] content, short statusCode = 0)
        {
            m_Fin = fin;
            m_Rsv1 = false;
            m_Rsv2 = false;
            m_Rsv3 = false;
            m_OpCode = opcode;
            m_UseMask = false;
            if (opcode == 8)
                m_StatusCode = statusCode;
            else
                m_StatusCode = 0;
            m_Content = content;
        }

        internal bool IsControlFrame() { return m_OpCode >= 8; }
        public bool IsFin() { return m_Fin; }
        public bool IsRsv1() { return m_Rsv1; }
        public bool IsRsv2() { return m_Rsv2; }
        public bool IsRsv3() { return m_Rsv3; }
        internal void SetOpCode(int opCode) { m_OpCode = opCode; }
        public int GetOpCode() { return m_OpCode; }
        public short GetStatusCode() { return m_StatusCode; }
        public byte[] GetContent() { return m_Content; }
        internal void SetMask(int mask)
        {
            m_UseMask = true;
            m_Mask = BitConverter.GetBytes(mask);
        }
        public bool UseMask() => m_UseMask;
        public byte[] GetMask() => m_Mask;

        /// <returns>String representing the frame</returns>
        public override string ToString()
        {
            return string.Format("Fin: {0}, Op code: {1}, Is mask: {2}, Status code: {3}, Content: {4}",
                (m_Fin) ? "true" : "false", m_OpCode, (m_UseMask) ? "true" : "false", m_StatusCode, Encoding.UTF8.GetString(m_Content)); ;
        }
    }
}
