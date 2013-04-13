using System;
/*using System.Diagnostics;*/
using System.IO;
/*using System.Windows;
using System.Windows.Resources;*/
/*using System.Threading;*/

namespace Helper
{
    public sealed class helper
    {
        public const int SEEK_SET = 0;
        static Random r = new Random();

        #region Memory Buffers
        #region ObjectBuffer
        public class ObjectBuffer
        {
            public object[] buffer;
            public int ofs;

            public ObjectBuffer(object[] buffer, int ofs)
            {
                this.buffer = buffer;
                this.ofs = ofs;
            }

            public object this[int index]
            {
                get { return buffer[ofs + index]; }
                set { buffer[ofs + index] = value; }
            }

            public static ObjectBuffer operator +(ObjectBuffer obj, int ofs)
            {
                obj.ofs += ofs;
                return obj;
            }

            public static ObjectBuffer operator -(ObjectBuffer obj, int ofs)
            {
                obj.ofs -= ofs;
                return obj;
            }

            public static bool operator >=(ObjectBuffer obj1, ObjectBuffer obj2)
            {
                return obj1.ofs >= obj2.ofs;
            }

            public static bool operator <=(ObjectBuffer obj1, ObjectBuffer obj2)
            {
                return obj1.ofs <= obj2.ofs;
            }
        }
        #endregion

        #region ByteBuffer
        public class ByteBuffer
        {
            public byte[] buffer;
            public int ofs;

            public ByteBuffer(byte[] buffer)
                : this(buffer, 0)
            { }
            public ByteBuffer(ByteBuffer buf)
                : this(buf.buffer, buf.ofs)
            { }
            public ByteBuffer(ByteBuffer buf, int ofs)
                : this(buf.buffer, buf.ofs + ofs)
            { }
            public ByteBuffer(byte[] buffer, int ofs)
            {
                this.buffer = buffer;
                this.ofs = ofs;
            }

            public byte this[int index]
            {
                get { return buffer[ofs + index]; }
                set { buffer[ofs + index] = value; }
            }

            public static ByteBuffer operator +(ByteBuffer obj, int ofs)
            {
                return new ByteBuffer(obj.buffer, obj.ofs + ofs);
            }

            public void Add(int ofs)
            {
                this.ofs += ofs;
            }
            public void Sub(int ofs)
            {
                this.ofs -= ofs;
            }

            public static bool operator >=(ByteBuffer obj1, ByteBuffer obj2)
            {
                return obj1.ofs >= obj2.ofs;
            }

            public static bool operator <=(ByteBuffer obj1, ByteBuffer obj2)
            {
                return obj1.ofs <= obj2.ofs;
            }
        }
        #endregion

        #region UIntBuffer
        public class UIntBuffer
        {
            public uint[] buffer;
            public int ofs;

            public UIntBuffer(uint[] buffer, int ofs)
            {
                this.buffer = buffer;
                this.ofs = ofs;
            }

            public uint this[int index]
            {
                get { return buffer[ofs + index]; }
                set { buffer[ofs + index] = value; }
            }

            public static UIntBuffer operator +(UIntBuffer obj, int ofs)
            {
                obj.ofs += ofs;
                return obj;
            }

            public static UIntBuffer operator -(UIntBuffer obj, int ofs)
            {
                obj.ofs -= ofs;
                return obj;
            }

            public static bool operator >=(UIntBuffer obj1, UIntBuffer obj2)
            {
                return obj1.ofs >= obj2.ofs;
            }

            public static bool operator <=(UIntBuffer obj1, UIntBuffer obj2)
            {
                return obj1.ofs <= obj2.ofs;
            }
        }
        #endregion
        #endregion

        public class FILE
        {
            public Stream stream;
        }
        
        public static char getc(FILE file)
        {
            return (char)file.stream.ReadByte();
        }

        public static int fread(out double data, int size, int count, FILE file)
        {
            BinaryReader reader = new BinaryReader(file.stream);
            data = (size == 4) ? reader.ReadSingle() : reader.ReadDouble();
            return count;
        }

        public static int fread(out int data, int size, int count, FILE file)
        {
            BinaryReader reader = new BinaryReader(file.stream);
            data = reader.ReadInt32();
            return count;
        }

        public static int fread(ref byte[] data, int size, int count, FILE file)
        {
            BinaryReader reader = new BinaryReader(file.stream);
            byte[] buf = reader.ReadBytes(size);
            Buffer.BlockCopy(buf, 0, data, 0, size);
            return count;
        }

        public static void fseek(FILE file, int position, int seek)
        {
            file.stream.Seek(position, SeekOrigin.Begin);
        }

        public static void fclose(FILE file)
        {
            file.stream.Close();
            file.stream = null;
            file = null;
        }

        public static int rand()
        {
            return r.Next();
        }
    }
}
