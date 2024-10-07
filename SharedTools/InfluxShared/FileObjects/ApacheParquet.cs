using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace InfluxShared.FileObjects
{
    // This is a hack MemoryStream class to trick ParquetWriter, as it seems writes in metadata channel offsets according to Stream.Position value.
    // This way ParquetWriter thinks that position in stream is real position in file, and we was able to write stream in parts to satisfy S3
    // multipart upload with low memory consumtion.
    public class ParquetStream : MemoryStream
    {
        public long offset { get; set; } = 0;

        public override long Position
        {
            get => base.Position + offset;
            set => base.Position = value - offset;
        }
        public long basePosition { get => base.Position; set => base.Position = value; }

        public override long Length => base.Length + offset;
        public long baseLength => base.Length;

        public override int Capacity
        {
            get => (int)(base.Capacity + offset);
            set
            {
                if (value < offset)
                {
                    offset = 0;
                    base.Capacity = value;
                }
                else
                    base.Capacity = (int)(value - offset);
            }
        }
        public int baseCapacity => base.Capacity;

        public override long Seek(long offset, SeekOrigin loc) => base.Seek(offset - this.offset, loc);
        public long baseSeek(long offset, SeekOrigin loc) => base.Seek(offset, loc);

        public override void SetLength(long value) => base.SetLength(value - offset);
        public void baseSetLength(long value) => base.SetLength(value);
    }

    static class ApacheParquet
    {
        public const string Extension = ".parquet";
        public const string Filter = "Apache Parquet (*.parquet)|*.parquet";
        public static Int64 chunkSize = 50 * 1024 * 1024; // 50 MB
        public static CompressionMethod compressionType = CompressionMethod.Snappy;
        public static CompressionLevel compressionLevel = CompressionLevel.Optimal;
        public static int OnChunkSizeTrigger = 10 * 1024 * 1024; // 10 MB

        public static bool ToApacheParquet(this DoubleDataCollection ddata, string ParquetFileName, Action<object> ProgressCallback = null)
        {
            using (Stream parquet = File.Create(ParquetFileName))
                return ToApacheParquet(ddata, null, parquet, ProgressCallback);
        }

        public static bool ToApacheParquet(this DoubleDataCollection ddata, Action<ParquetStream> OnChunk, Action<object> ProgressCallback = null) =>
            ToApacheParquet(ddata, OnChunk, null, ProgressCallback);

        public static bool ToApacheParquet(this DoubleDataCollection ddata, Stream ParquetOutStream, Action<object> ProgressCallback = null) =>
            ToApacheParquet(ddata, null, ParquetOutStream, ProgressCallback);

        private static bool ToApacheParquet(this DoubleDataCollection ddata, Action<ParquetStream> OnChunk = null, Stream ParquetOutStream = null, Action<object> ProgressCallback = null)
        {
            if (OnChunk is null && ParquetOutStream is null)
                return false;

            void OnDataChunk(ParquetStream ps)
            {
                long offset = ps.offset + ps.baseLength;
                ps.baseSeek(0, SeekOrigin.Begin);
                ps.basePosition = 0;

                ParquetOutStream.Seek(0, SeekOrigin.End);
                ps.CopyTo(ParquetOutStream);

                ps.baseSeek(0, SeekOrigin.Begin);
                ps.basePosition = 0;
                ps.baseSetLength(0);
                ps.offset = offset;
            }

            try
            {
                OnChunk ??= OnDataChunk;

                ProgressCallback?.Invoke(0);
                ProgressCallback?.Invoke("Writing Apache Parquet data...");

                var ci = new CultureInfo("en-US", false);
                ddata.InitReading();

                Dictionary<string, int> dict = new Dictionary<string, int>();
                List<DataField> fields = [
                    ddata.DefaultCsvDateFormat == TimeFormatType.DateTime ? 
                    new DateTimeDataField("Time", DateTimeFormat.DateAndTime) : 
                    new DataField<double>("Time")
                ];
                //List<DataField> fields = [new DataField<double>("Time")];
                foreach (var data in ddata)
                {
                    if (dict.ContainsKey(data.ChannelName))
                    {
                        fields.Add(new DataField<double>(data.ChannelName + "_" + dict[data.ChannelName].ToString()));
                        dict[data.ChannelName]++;
                    }
                    else
                    {
                        fields.Add(new DataField<double>(data.ChannelName));
                        dict.Add(data.ChannelName, 1);
                    }

                }

                var schema = new ParquetSchema(fields);

                Int64 MaxChunkCount = chunkSize / 8 / schema.DataFields.Length;
                double[][] chunk = new double[MaxChunkCount][];
                int chunkcount = 0;
                ParquetWriter writer = null;
                ParquetStream ps = new();
                double[][] matrix = new double[schema.DataFields.Length][];
                Task WriterTask = null;

                bool Transpose()
                {
                    if (chunkcount == 0)
                        return false;

                    for (var c = 0; c < schema.DataFields.Length; c++)
                    {
                        matrix[c] = new double[chunkcount];
                        for (var r = 0; r < chunkcount; r++)
                            matrix[c][r] = chunk[r][c];
                    }

                    chunkcount = 0;
                    return true;
                }

                async void WriteData()
                {
                    using (ParquetRowGroupWriter groupWriter = writer.CreateRowGroup())
                    {
                        if (ddata.DefaultCsvDateFormat == TimeFormatType.DateTime)
                        {
                            DateTime[] times = new DateTime[matrix[0].Length];
                            for (int ti = 0; ti < matrix[0].Length; ti++)
                                times[ti] = DateTime.FromOADate(ddata.RealTime.ToOADate() + matrix[0][ti] / 86400);
                            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[0], times));
                        }
                        else
                            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[0], matrix[0]));

                        for (int i = 1; i < schema.DataFields.Length; i++)
                            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[i], matrix[i]));
                    }

                    if (ps.baseLength >= OnChunkSizeTrigger)
                        OnChunk(ps);
                }

                Task.Run(async () =>
                {
                    using (writer = await ParquetWriter.CreateAsync(schema, ps))
                    {
                        writer.CompressionMethod = compressionType;
                        writer.CompressionLevel = compressionLevel;

                        double[] Values = ddata.GetValues();
                        do
                        {
                            if (Values != null)
                                chunk[chunkcount] = Values;
                            chunkcount++;
                            if (chunkcount >= MaxChunkCount)
                            {
                                WriterTask?.Wait();
                                if (Transpose())
                                    WriterTask = Task.Run(() => WriteData());
                            }
                            Values = ddata.GetValues();

                            ProgressCallback?.Invoke((int)(ddata.ReadingProgress * 100));
                        } while (Values != null);

                        WriterTask?.Wait();
                        if (Transpose())
                            Task.Run(() => WriteData()).Wait();
                    }
                }).Wait();
                if (ps.baseLength > 0)
                    OnChunk(ps);

                ProgressCallback?.Invoke(100);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                //return false;
            }
        }
    }
}
