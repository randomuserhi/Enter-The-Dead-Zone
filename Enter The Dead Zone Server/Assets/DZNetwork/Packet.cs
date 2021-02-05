using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DZNetwork
{
    public class IPEndPointComparer : IEqualityComparer<IPEndPoint>
    {
        public bool Equals(IPEndPoint A, IPEndPoint B)
        {
            return A.Equals(B);
        }

        public int GetHashCode(IPEndPoint A)
        {
            return A.GetHashCode();
        }
    }

    public static class PacketHandler
    {
        public static int ProtocolID = 0;
        private static int PacketHeaderSize = sizeof(int) * 2 + sizeof(long) + sizeof(ushort);
        public static int HeaderSize = PacketHeaderSize + sizeof(ushort) * 2 + sizeof(int) * 3;

        public static ushort PacketID = 0;
        public static ushort LocalPacketSequence = 0;

        public class Acknowledgement
        {
            public ushort PacketAcknowledgement = 0;
            public int PacketAcknowledgementBitField = 0;
        }
        public static Dictionary<IPEndPoint, Acknowledgement> PacketAcknowledgements = new Dictionary<IPEndPoint, Acknowledgement>(new IPEndPointComparer());

        public struct PacketGroup
        {
            public ushort StartingPacketSequence;
            public byte[][] Packets;
        }

        public static Acknowledgement GetAcknowledgement(IPEndPoint EndPoint)
        {
            if (!PacketAcknowledgements.ContainsKey(EndPoint)) PacketAcknowledgements.Add(EndPoint, new Acknowledgement());
            return PacketAcknowledgements[EndPoint];
        }

        public static void RemoveAcknowledgement(IPEndPoint EndPoint)
        {
            if (PacketAcknowledgements.ContainsKey(EndPoint))
                PacketAcknowledgements.Remove(EndPoint);
        }

        public static int CalculateCheckSum(byte[] Data, int Offset = sizeof(int) * 2)
        {
            int Sum = 0;
            for (int i = Offset; i < Data.Length; i++)
                Sum += Data[i];
            return Sum;
        }

        public static PacketGroup GeneratePackets(Packet P, IPEndPoint EndPoint)
        {
            long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

            byte[] Data = P.GetBytes();
            float NumPacketsNoHeader = UnityEngine.Mathf.Ceil((float)Data.Length / Loader.Socket.BufferSize);
            int NumPackets = UnityEngine.Mathf.CeilToInt((Data.Length + NumPacketsNoHeader * HeaderSize) / Loader.Socket.BufferSize);
            byte[][] Packets = new byte[NumPackets][];

            byte[] HeaderBytes = new byte[PacketHeaderSize];
            int WriteHead = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(ProtocolID), 0, HeaderBytes, WriteHead, sizeof(int)); WriteHead += sizeof(int) * 2;
            Buffer.BlockCopy(BitConverter.GetBytes(Epoch), 0, HeaderBytes, WriteHead, sizeof(long)); WriteHead += sizeof(long);
            Buffer.BlockCopy(BitConverter.GetBytes(PacketID), 0, HeaderBytes, WriteHead, sizeof(ushort));

            PacketGroup PacketGroup = new PacketGroup()
            {
                StartingPacketSequence = LocalPacketSequence
            };

            lock (PacketAcknowledgements)
            {
                Acknowledgement Ack = GetAcknowledgement(EndPoint);

                int RemainingPacketSize = Data.Length;
                int ReadHead = 0;
                for (int i = 0; i < NumPackets; i++)
                {
                    int PacketSize = Math.Min(RemainingPacketSize, Loader.Socket.BufferSize - HeaderSize);
                    Packets[i] = new byte[PacketSize + HeaderSize];
                    Buffer.BlockCopy(HeaderBytes, 0, Packets[i], 0, HeaderBytes.Length);

                    int HeaderIndex = HeaderBytes.Length;
                    Buffer.BlockCopy(BitConverter.GetBytes(LocalPacketSequence), 0, Packets[i], HeaderIndex, sizeof(ushort)); HeaderIndex += sizeof(ushort);
                    Buffer.BlockCopy(BitConverter.GetBytes(Ack.PacketAcknowledgement), 0, Packets[i], HeaderIndex, sizeof(ushort)); HeaderIndex += sizeof(ushort);
                    Buffer.BlockCopy(BitConverter.GetBytes(Ack.PacketAcknowledgementBitField), 0, Packets[i], HeaderIndex, sizeof(int)); HeaderIndex += sizeof(int);
                    Buffer.BlockCopy(BitConverter.GetBytes(Data.Length), 0, Packets[i], HeaderIndex, sizeof(int)); HeaderIndex += sizeof(int);
                    Buffer.BlockCopy(BitConverter.GetBytes(i), 0, Packets[i], HeaderIndex, sizeof(int));

                    Buffer.BlockCopy(Data, ReadHead, Packets[i], HeaderSize, PacketSize);

                    int CheckSum = CalculateCheckSum(Packets[i]);
                    Buffer.BlockCopy(BitConverter.GetBytes(CheckSum), 0, Packets[i], sizeof(int), sizeof(int));

                    ReadHead += PacketSize;
                    RemainingPacketSize -= PacketSize;

                    LocalPacketSequence++;
                }
            }

            PacketID++;

            PacketGroup.Packets = Packets;
            return PacketGroup;
        }
    }

    public class Packet
    {
        public byte[] ReadableBuffer;

        private List<byte> Buffer;
        private int ReadPosition = 0;

        /// <summary>
        /// Generates a blank packet
        /// </summary>
        public Packet()
        {
            Buffer = new List<byte>();
        }

        /// <summary>
        /// Generates a packet from which data can be read, making a copy
        /// </summary>
        /// <param name="Data">Bytes to be read</param>
        public Packet(byte[] Buffer, int Start, int Count)
        {
            ReadableBuffer = new byte[Count];
            System.Buffer.BlockCopy(Buffer, Start, ReadableBuffer, 0, Count);
        }

        /// <summary>
        /// Generates a packet from which data can be read, without making a copy
        /// </summary>
        /// <param name="Data">Bytes to be read</param>
        public Packet(byte[] Buffer)
        {
            ReadableBuffer = Buffer;
        }

        public void InsertServerCode(ServerCode Code)
        {
            Buffer.InsertRange(0, BitConverter.GetBytes((int)Code));
        }

        public void SeekHeader()
        {
            ReadPosition = PacketHandler.HeaderSize;
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
        /// Converts buffer to byte array
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            ReadableBuffer = Buffer.ToArray();
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

        /// <summary>
        /// Writes a range of bytes without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(byte[] Data)
        {
            Buffer.AddRange(Data);
        }

        /// <summary>
        /// Writes a string without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(string Value)
        {
            Write(Value.Length); //Add length of string
            Buffer.AddRange(Encoding.ASCII.GetBytes(Value));
        }

        /// <summary>
        /// Writes a byte without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(byte Value)
        {
            Buffer.Add(Value);
        }

        /// <summary>
        /// Writes an int without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(int Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Writes an ushort without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(ushort Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Writes a float without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(float Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Writes a long without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(long Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Writes an ulong without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(ulong Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Writes a boolean without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(bool Value)
        {
            Buffer.AddRange(BitConverter.GetBytes(Value));
        }

        /// <summary>
        /// Returns the unread length of the packet
        /// </summary>
        /// <param name="Data"></param>
        public int UnreadLength()
        {
            return ReadableBuffer.Length - ReadPosition;
        }

        /// <summary>
        /// Set the reading position for the packet
        /// </summary>
        /// <param name="ReadPosition"></param>
        public void Seek(int ReadPosition)
        {
            this.ReadPosition = ReadPosition;
        }

        /// <summary>
        /// Skip bytes of a packet
        /// </summary>
        /// <param name="ReadPosition"></param>
        public void Skip(int NumBytes)
        {
            ReadPosition += NumBytes;
        }

        /// <summary>
        /// Reads a byte value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public byte ReadByte(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(byte))
            {
                byte Value = ReadableBuffer[ReadPosition];
                if (MoveRead)
                    ReadPosition += sizeof(byte);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Byte value");
            }
        }


        /// <summary>
        /// Reads a boolean value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a long value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
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

        /// <summary>
        /// Reads an ulong value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public ulong ReadULong(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(ulong))
            {
                ulong Value = BitConverter.ToUInt64(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(ulong);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Unsigned Long value");
            }
        }

        /// <summary>
        /// Reads a Short value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public short ReadShort(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(short))
            {
                short Value = BitConverter.ToInt16(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(short);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Short value");
            }
        }

        /// <summary>
        /// Reads a ushort value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public ushort ReadUShort(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(ushort))
            {
                ushort Value = BitConverter.ToUInt16(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(ushort);
                return Value;
            }
            else
            {
                throw new Exception("Could not read UShort value");
            }
        }

        /// <summary>
        /// Reads a int value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
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

        /// <summary>
        /// Reads a uint value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public uint ReadUInt(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(int))
            {
                uint Value = BitConverter.ToUInt32(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(uint);
                return Value;
            }
            else
            {
                throw new Exception("Could not read UInt value");
            }
        }

        /// <summary>
        /// Reads a float value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
        public float ReadFloat(bool MoveRead = true)
        {
            if (UnreadLength() >= sizeof(float))
            {
                float Value = BitConverter.ToSingle(ReadableBuffer, ReadPosition);
                if (MoveRead)
                    ReadPosition += sizeof(float);
                return Value;
            }
            else
            {
                throw new Exception("Could not read Int value");
            }
        }

        /// <summary>
        /// Reads a string value from the current ReadPosition of the packet
        /// </summary>
        /// <param name="MoveRead">Move the ReadPosition after read</param>
        /// <returns></returns>
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
