using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Writers
{
    public class FArchiveWriter : BinaryWriter
    {
        private readonly MemoryStream _memoryData;

        public FArchiveWriter()
        {
            _memoryData = new MemoryStream {Position = 0};
            OutStream = _memoryData;
        }

        public virtual void DumpStruct<T>(T struc) where T : struct
        {
            var size = Unsafe.SizeOf<T>();
            var bytes = new byte[size];
            Unsafe.WriteUnaligned(ref bytes[0], struc);
            OutStream.Write(bytes);
        }
        
        public virtual void WriteArray<T>(T[] array) where T : struct
        {
            var size = Unsafe.SizeOf<T>();
            var bytes = new byte[size * array.Length];
            Unsafe.WriteUnaligned(ref bytes[0], array);
            OutStream.Write(bytes);
        }

        public byte[] GetBuffer() => _memoryData.ToArray();

        public long Length => _memoryData.Length;
        public long Position => _memoryData.Position;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _memoryData.Dispose();
        }
    }
}