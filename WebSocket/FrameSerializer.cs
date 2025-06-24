using CorpseLib.Serialize;

namespace CorpseLib.Web.WebSocket
{
    public class FrameSerializer : ABytesSerializer<Frame>
    {
        protected override OperationResult<Frame> Deserialize(ABytesReader reader)
        {
            byte b = reader.Read<byte>();
            bool fin = (b & 0b10000000) != 0;
            bool rsv1 = (b & 0b01000000) != 0;
            bool rsv2 = (b & 0b00100000) != 0;
            bool rsv3 = (b & 0b00010000) != 0;
            int opCode = b & 0b00001111;
            short statusCode = 0;
            b = reader.Read<byte>();
            bool useMask = (b & 0b10000000) != 0;
            long payloadLen = b & 0b01111111;

            if (payloadLen == 126)
                payloadLen = reader.Read<short>();
            else if (payloadLen == 127)
                payloadLen = reader.Read<long>();

            byte[] mask = (useMask) ? reader.ReadBytes(4) : [];
            byte[] buffer = reader.ReadBytes((int)payloadLen);
            if (buffer.Length != payloadLen)
                return new("Not enough bytes", $"Payload expected {payloadLen} bytes but {buffer.Length} where read");

            if (useMask)
            {
                for (long i = 0; i < payloadLen; ++i)
                    buffer[i] = (byte)(buffer[i] ^ mask[i % 4]);
            }

            if (opCode == 8)
            {
                statusCode = BitConverter.ToInt16([buffer[1], buffer[0]]);
                buffer = buffer[2..];
            }

            return new(new(fin, rsv1, rsv2, rsv3, opCode, useMask, mask, statusCode, buffer));
        }

        protected override void Serialize(Frame obj, ABytesWriter writer)
        {
            byte[] bytesRaw = obj.GetContent();

            if (obj.GetOpCode() == 8)
            {
                byte[] statusByte = BitConverter.GetBytes(obj.GetStatusCode());
                (statusByte[1], statusByte[0]) = (statusByte[0], statusByte[1]);
                bytesRaw = [.. statusByte, .. bytesRaw];
            }

            long length = bytesRaw.Length;

            byte firstByte = 0;
            if (obj.IsFin()) firstByte += 128;
            if (obj.IsRsv1()) firstByte += 64;
            if (obj.IsRsv2()) firstByte += 32;
            if (obj.IsRsv3()) firstByte += 16;
            firstByte += (byte)obj.GetOpCode();
            writer.Write(firstByte);
            if (length <= 125)
                writer.Write((byte)((obj.UseMask()) ? length + 128 : length));
            else if (length >= 126 && length <= 65535)
            {
                writer.Write((byte)((obj.UseMask()) ? 254 : 126));
                writer.Write((byte)((length >> 8) & 255));
                writer.Write((byte)(length & 255));
            }
            else
            {
                writer.Write((byte)((obj.UseMask()) ? 255 : 127));
                writer.Write((byte)((length >> 56) & 255));
                writer.Write((byte)((length >> 48) & 255));
                writer.Write((byte)((length >> 40) & 255));
                writer.Write((byte)((length >> 32) & 255));
                writer.Write((byte)((length >> 24) & 255));
                writer.Write((byte)((length >> 16) & 255));
                writer.Write((byte)((length >> 8) & 255));
                writer.Write((byte)(length & 255));
            }

            byte[] frameMask = (obj.UseMask()) ? obj.GetMask() : [];
            if (obj.UseMask())
            {
                for (byte i = 0; i != 4; ++i)
                    writer.Write(frameMask[i]);
            }

            for (long i = 0; i < length; i++)
                writer.Write((obj.UseMask()) ? (byte)(bytesRaw[i] ^ frameMask[i % 4]) : bytesRaw[i]);
        }
    }
}
