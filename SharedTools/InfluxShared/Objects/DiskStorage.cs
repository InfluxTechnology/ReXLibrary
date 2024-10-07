using InfluxShared.Helpers;
using InfluxShared.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace InfluxShared.Objects
{
    public class DiskStorage<T> : FileStream, IStorage<T>, IDisposable, IEnumerable<T> where T : struct
    {
        internal readonly string FilePath;
        byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
        public int elementSize { get; }
        public long elementCount => Length / elementSize;

        private bool disposedValue;
        private byte[] readArr;

        public DiskStorage(string filePath) : base(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose)
        {
            elementSize = Marshal.SizeOf(typeof(T));
            FilePath = filePath;
            readArr = new byte[elementSize];
        }

        #region Destructors

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Flush(true);
                    //Close();
                    //if (File.Exists(FilePath))
                    //File.Delete(FilePath);
                    //base.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DoubleData()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public new void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public static string GenerateFileName(string Directory, string Extension, int FileNameLength = 20)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();

            string FileName;
            do
                FileName = Path.Combine(Directory, new string(Enumerable.Repeat(chars, FileNameLength).Select(s => s[random.Next(s.Length)]).ToArray()) + "." + Extension);
            while (File.Exists(FileName));

            return FileName;
        }

        public T this[long index]
        {
            get
            {
                if (index < 0 || index > elementCount)
                    throw new ArgumentOutOfRangeException();

                Seek(index * elementSize, SeekOrigin.Begin);
                Read(buffer, 0, (int)elementSize);
                return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

        public void InitRead()
        {
            Flush();
            Seek(0, SeekOrigin.Begin);
        }

        public void Write(T value) => Write(BitConverter.GetBytes((dynamic)value), 0, elementSize);

        public bool Read(ref T value)
        {
            if (Read(readArr, 0, elementSize) != elementSize)
                return false;

            value = readArr.ConvertTo<T>();
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            Seek(0, SeekOrigin.Begin);
            for (int index = 0; index < elementCount; index++)
            {
                Read(buffer, 0, (int)elementSize);
                yield return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Seek(0, SeekOrigin.Begin);
            for (int index = 0; index < elementCount; index++)
            {
                Read(buffer, 0, (int)elementSize);
                yield return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

        public static T[] GetArray(string filePath)
        {
            T[] elements;
            int elementSize;
            long numberOfElements;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException();
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            FileInfo info = new FileInfo(filePath);
            using (MemoryMappedFile mappedFile = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (MemoryMappedViewAccessor accesor = mappedFile.CreateViewAccessor(0, info.Length))
                {
                    elementSize = Marshal.SizeOf(typeof(T));
                    numberOfElements = info.Length / elementSize;
                    elements = new T[numberOfElements];

                    if (numberOfElements > int.MaxValue)
                    {
                        //you will need to split the array
                    }
                    else
                    {
                        accesor.ReadArray<T>(0, elements, 0, (int)numberOfElements);
                    }
                }
            }

            return elements;
        }
    }
}
