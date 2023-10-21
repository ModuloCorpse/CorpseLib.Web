namespace CorpseLib.Web.WebSocket
{
    public class FragmentedFrameBuilder
    {
        private readonly List<Frame> m_Frames = new();

        public void Clear() => m_Frames.Clear();

        public Frame? BuildFrame(Frame frame)
        {
            int frameOpCode = frame.GetOpCode();
            if (frameOpCode != 0 && frameOpCode != 1 && frameOpCode != 2) //Only those op codes can represent a fragmented frame
                return frame;
            if (frame.IsFin())
            {
                if (m_Frames.Count == 0)
                    return frame;
                else
                {
                    m_Frames.Add(frame);
                    int opCode = m_Frames[0].GetOpCode();
                    short statusCode = m_Frames[0].GetStatusCode();
                    byte[] contentBuffer = Array.Empty<byte>();
                    foreach (Frame fragmentedFrame in m_Frames)
                    {
                        byte[] frameContent = fragmentedFrame.GetContent();
                        byte[] tmp = new byte[contentBuffer.Length + frameContent.Length];
                        contentBuffer.CopyTo(tmp, 0);
                        frameContent.CopyTo(tmp, contentBuffer.Length);
                        contentBuffer = tmp;
                    }
                    m_Frames.Clear();
                    return new(true, opCode, contentBuffer, statusCode);
                }
            }
            else
                m_Frames.Add(frame);
            return null;
        }
    }
}
