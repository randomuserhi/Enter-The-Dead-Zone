using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    public class Packet
    {
        public const int HeaderSize = sizeof(int) + sizeof(long);

        private List<byte> Buffer;
        public byte[] ReadableBuffer { get; private set; }
        private int ReadPosition = 0;

        /// <summary>
        /// Generates a blank packet
        /// </summary>
        public Packet()
        {
            Buffer = new List<byte>();
        }

        /// <summary>
        /// Generates a packet from which data can be read
        /// </summary>
        /// <param name="Data">Bytes to be read</param>
        public Packet(byte[] Data)
        {
            Buffer = new List<byte>(Data);
            ReadableBuffer = Buffer.ToArray();
        }

        /// <summary>
        /// Reset the packets buffers if passed true
        /// </summary>
        /// <param name="Reset"></param>
        public void Reset(bool Reset)
        {
            if (!Reset)
                return;

            Buffer.Clear();
            ReadableBuffer = null;
            ReadPosition = 0;
        }

        /// <summary>
        /// Converts the packet into a formatted buffer
        /// This automatically adds a header including information such as packet size and time sent (epoch)
        /// </summary>
        /// <returns></returns>
        public byte[] ConvertBufferToArray()
        {
            long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

            //Insert Header
            byte[] Header = new byte[sizeof(int) + sizeof(long)];
            Array.Copy(BitConverter.GetBytes(Buffer.Count()), 0, Header, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(Epoch), 0, Header, sizeof(int), sizeof(long));

            ReadableBuffer = new byte[sizeof(int) + sizeof(long) + Buffer.Count()];
            Array.Copy(Header, 0, ReadableBuffer, 0, sizeof(int) + sizeof(long));
            Array.Copy(Buffer.ToArray(), 0, ReadableBuffer, sizeof(int) + sizeof(long), Buffer.Count());
            return ReadableBuffer;
        }

        /// <summary>
        /// Writes a range of bytes and sets it to the readable buffer
        /// </summary>
        /// <param name="Data"></param>
        public void WriteRange(byte[] Data)
        {
            Buffer.AddRange(Data);
            ReadableBuffer = Buffer.ToArray();
        }

        public void Write(string Value)
        {
            Write(Value.Length); //Add length of string
            Buffer.AddRange(Encoding.ASCII.GetBytes(Value));
        }

        public void Write(int Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        public void Write(long Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        public void Write(bool Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        public int UnreadLength()
        {
            return ReadableBuffer.Length - ReadPosition;
        }

        public void Seek(int ReadPosition)
        {
            this.ReadPosition = ReadPosition;
        }

        public bool ReadBool(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(bool))
            {
                bool Value = BitConverter.ToBoolean(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(bool);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Boolean value");
            }
        }

        public long ReadLong(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(long))
            {
                long Value = BitConverter.ToInt64(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(long);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Long value");
            }
        }

        public int ReadInt(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(int))
            {
                int Value = BitConverter.ToInt32(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(int);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Int value");
            }
        }

        public string ReadString(bool MoveRead = true)
        {
            try
            {
                int Length = ReadInt();
                string Value = Encoding.ASCII.GetString(ReadableBuffer, ReadPosition, Length);
                if (MoveRead && Value.Length > 0)
                    ReadPosition += Length;
                return Value;
            }
            catch
            {
                throw new Exception("Could not read String value");
            }
        }
    }
}
