using SolarGames.Networking.Crypting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SolarGames.Networking
{
    /// <summary>
    /// 基础类型和object类型的读写，写入memory stream
    /// BinaryFormatter 进行obj的序列化
    /// </summary>
    public class BasePacket
    {
        protected ushort type;

        /// <summary>
        /// 数据写入的stream
        /// </summary>
        protected MemoryStream stream;
        protected BinaryWriter writer;
        protected BinaryReader reader;
        
        public ushort Type
        {
            get { return type; }
            set { type = value; }
        }
        
        public virtual byte[] GetBody()
        {
            return stream.ToArray();
        }

        protected static ushort CodePacketType(ushort type, int len)
        {
            int a1 = ((ushort)len) ^ 0xAC53;
            int a2 = ((ushort)len) ^ 0xAAAA;
            return (ushort)(type ^ a1 ^ a2);
        }
        
        /// <summary>
        /// 将object序列化为byte[]，然后写入writer
        /// </summary>
        /// <param name="obj"></param>
        public void WriteSerialize(object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                byte[] data = ms.ToArray();
                writer.Write(data.Length);
                writer.Write(data);
            }
        }

        /// <summary>
        /// 使用  做
        /// </summary>
        /// <returns></returns>
        public object ReadSerialized()
        {
            int len = reader.ReadInt32();
            byte[] data = reader.ReadBytes(len);

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return bf.Deserialize(ms);
            }
        }

        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public float ReadSingle()
        {
            return reader.ReadSingle();
        }

        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        public short ReadInt16()
        {
            return reader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return reader.ReadUInt16();
        }

        public int ReadInt32()
        {
            return reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }

        public long ReadInt64()
        {
            return reader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            return reader.ReadUInt64();
        }

        public string ReadString()
        {
            return reader.ReadString();
        }

        public void Write(byte data)
        {
            writer.Write(data);
        }

        public void Write(float data)
        {
            writer.Write(data);
        }

        public void Write(double data)
        {
            writer.Write(data);
        }

        public void Write(long data)
        {
            writer.Write(data);
        }

        public void Write(ulong data)
        {
            writer.Write(data);
        }

        public void Write(int data)
        {
            writer.Write(data);
        }

        public void Write(uint data)
        {
            writer.Write(data);
        }

        public void Write(short data)
        {
            writer.Write(data);
        }

        public void Write(ushort data)
        {
            writer.Write(data);
        }

        public void Write(string data)
        {
            writer.Write(data);
        }

        public void Write(byte[] data)
        {
            writer.Write(data);
        }

        public void Write(byte[] data, int index, int count)
        {
            writer.Write(data, index, count);
        }
	
    }
}
