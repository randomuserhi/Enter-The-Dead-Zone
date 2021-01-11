﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    public class Packet
    {
        public int ClientIndex = -1; //The index of the client sender
        public const int HeaderSize = sizeof(int) + sizeof(long);

        private List<byte> Buffer;
        public byte[] ReadableBuffer { get; private set; }
        private int ReadPosition = 0;

        /// <summary>
        /// Generates a blank packet
        /// </summary>
        public Packet(int ClientIndex = -1)
        {
            this.ClientIndex = ClientIndex;
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
        public byte[] ConvertBufferToArray(int ClientIndex = -1)
        {
            this.ClientIndex = this.ClientIndex == -1 ? ClientIndex : this.ClientIndex;

            long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

            //Insert Header
            int HeaderSize = sizeof(int) + sizeof(long) + sizeof(int);
            byte[] Header = new byte[HeaderSize];
            Array.Copy(BitConverter.GetBytes(Buffer.Count()), 0, Header, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(Epoch), 0, Header, sizeof(int), sizeof(long));
            Array.Copy(BitConverter.GetBytes(this.ClientIndex), 0, Header, sizeof(int) + sizeof(long), sizeof(int));

            ReadableBuffer = new byte[HeaderSize + Buffer.Count()];
            Array.Copy(Header, 0, ReadableBuffer, 0, HeaderSize);
            Array.Copy(Buffer.ToArray(), 0, ReadableBuffer, HeaderSize, Buffer.Count());
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
        /// Writes an int without setting ReadableBuffer
        /// </summary>
        /// <param name="Data"></param>
        public void Write(int Value)
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
