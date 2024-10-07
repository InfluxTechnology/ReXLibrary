using InfluxShared.FileObjects;
using MatlabFile.Base;
using MDF4xx.IO;
using RXD.Base;
using RXD.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Influx.Shared.Helpers
{
    public static class DataHelper
    {
        [Flags]
        public enum TypeFlags
        {
            AllowMerge = 1 << 0,
            ContainTrace = 1 << 1,
            ContainData = 1 << 2,
        }

        public class FileType
        {
            public string Filter;
            public string Extension;
            public string Name = string.Empty;
            public override string ToString() => Filter.Split('|')[0];
            public TypeFlags Flags;
            public bool supportMerge => Flags.HasFlag(TypeFlags.AllowMerge);
            public bool traceOnly => Flags.HasFlag(TypeFlags.ContainTrace) && !Flags.HasFlag(TypeFlags.ContainData);
        }

        public class FileTypeList : List<FileType>
        {
            public FileTypeList()
            {
                Add(new FileType()
                {
                    Filter = DoubleDataCollection.Filter,
                    Extension = DoubleDataCollection.Extension,
                    Name = "DiaLOG",
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = DoubleDataCollection.svFilter,
                    Extension = DoubleDataCollection.svExtension,
                    Name = "Regional",
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = Matlab.Filter,
                    Extension = Matlab.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = ApacheParquet.Filter,
                    Extension = ApacheParquet.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = ASC.Filter,
                    Extension = ASC.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainTrace,
                });
                Add(new FileType()
                {
                    Filter = BLF.Filter,
                    Extension = BLF.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainTrace,
                });
                Add(new FileType()
                {
                    Filter = TRC.Filter,
                    Extension = TRC.Extension,
                    Flags = TypeFlags.ContainTrace,

                });
                Add(new FileType()
                {
                    Filter = CSTrace.Filter,
                    Extension = CSTrace.Extension,
                    Flags = TypeFlags.ContainTrace,
                });
                Add(new FileType()
                {
                    Filter = MDF.Filter,
                    Extension = MDF.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainTrace | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = BinRXD.Filter,
                    Extension = BinRXD.Extension,
                    Flags = TypeFlags.AllowMerge | TypeFlags.ContainTrace | TypeFlags.ContainData,
                });
                Add(new FileType()
                {
                    Filter = XmlHandler.Filter,
                    Extension = XmlHandler.Extension
                });
            }

            public bool ValidExtension(string extension) => Exists(e => extension.Equals(e.Extension, StringComparison.OrdinalIgnoreCase));
            public string TypeName(string filter) => this.Where(f => f.Filter == filter).Select(f => f.Name ?? "").FirstOrDefault();
        }

        public static FileTypeList FileTypeCollection = new();

        public static string LastConvertMessage = "";
        public static bool LastConvertStatus;

        private static void blfProcessing(BLF blf, TraceRow row)
        {
            if (row is not ITraceConvertAdapter)
                return;

            // TraceCanError should be first because it inherits TraceCan, and check for TraceCan will handle TraceCanErrors also. This produces crashes.
            if (row is TraceCanError canerr)
            {
                blf.WriteCanError((UInt64)(canerr.FloatTimestamp * 1000000), (byte)(canerr.BusChannel + 1), canerr.ErrorCode);
            }
            else if (row is TraceCan can)
            {
                if (can.flagEDL)
                    blf.WriteCanFDMessage(
                        can.flagIDE ? (can.CanID | 0x80000000) : can.CanID,
                        (UInt64)(can.FloatTimestamp * 1000000),
                        (byte)(can.BusChannel + 1),
                        can.flagDIR, can.flagBRS,
                        (byte)can.DLC, can.Data
                    );
                else
                    blf.WriteCanMessage(
                        can.flagIDE ? (can.CanID | 0x80000000) : can.CanID,
                        (UInt64)(can.FloatTimestamp * 1000000),
                        (byte)(can.BusChannel + 1),
                        can.flagDIR,
                        (byte)can.DLC, can.Data
                    );
            }
            else if (row is TraceLin lin)
            {

                if (!lin.isError)
                    blf.WriteLinMessage(
                        lin.LinID,
                        (UInt64)(lin.FloatTimestamp * 1000000),
                        (byte)(lin.BusChannel + 1),
                        lin.flagDIR,
                        lin.DLC,
                        lin.Data
                    );
                else
                {
                    if (lin.flagLCSE)
                        blf.WriteLinCrcError(
                            lin.LinID,
                            (UInt64)(lin.FloatTimestamp * 1000000),
                            (byte)(lin.BusChannel + 1),
                            lin.flagDIR,
                            lin.DLC,
                            lin.Data
                        );
                    else if (lin.flagLTE)
                        blf.WriteLinSendError(
                            lin.LinID,
                            (UInt64)(lin.FloatTimestamp * 1000000),
                            (byte)(lin.BusChannel + 1),
                            lin.DLC
                        );
                }
            }
        }

        public static bool ToBLF(this TraceCollection trace, string outputPath, Action<object> ProgressCallback)
        {
            ProgressCallback?.Invoke("Writing BLF file...");
            ProgressCallback?.Invoke(0);
            try
            {
                using (BLF blf = new BLF())
                {
                    if (blf.CreateFile(outputPath, trace.StartLogTime))
                        for (int i = 0; i < trace.Count; i++)
                        {
                            blfProcessing(blf, trace[i]);
                            ProgressCallback?.Invoke(i * 100 / trace.Count);
                        }
                    ProgressCallback?.Invoke(100);
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static bool ToBLF(this TraceCollection trace, Stream blfStream, Action<object> ProgressCallback)
        {
            ProgressCallback?.Invoke("Writing BLF file...");
            ProgressCallback?.Invoke(0);
            try
            {
                using (BLF blf = new BLF())
                {
                    if (blf.CreateStream(blfStream, trace.StartLogTime))
                        for (int i = 0; i < trace.Count; i++)
                        {
                            blfProcessing(blf, trace[i]);
                            ProgressCallback?.Invoke(i * 100 / trace.Count);
                        }
                    ProgressCallback?.Invoke(100);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool ToBLF(this BinRXD rxd, string outputPath, Action<object> ProgressCallback)
        {
            try
            {
                using (BLF blf = new BLF())
                    if (blf.CreateFile(outputPath, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords(
                            (tc) =>
                            {
                                foreach (var row in tc)
                                    blfProcessing(blf, row);
                            },
                            ProgressCallback
                        );
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToBLF(this BinRXD rxd, Stream blfStream, Action<object> ProgressCallback)
        {
            try
            {
                using (BLF blf = new BLF())
                    if (blf.CreateStream(blfStream, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords(
                            (tc) =>
                            {
                                foreach (var row in tc)
                                    blfProcessing(blf, row);
                            },
                            ProgressCallback
                        );
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToASCII(this BinRXD rxd, string outputPath, Action<object> ProgressCallback)
        {
            try
            {
                using (ASC asc = new ASC())
                    if (asc.Start(outputPath, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => asc.WriteLine(tc.asASCII), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToASCII(this BinRXD rxd, Stream outputStream, Action<object> ProgressCallback)
        {
            try
            {
                using (ASC asc = new ASC())
                    if (asc.Start(outputStream, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => asc.WriteLine(tc.asASCII), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToTRC(this BinRXD rxd, string outputPath, Action<object> ProgressCallback)
        {
            try
            {
                using (TRC trc = new TRC())
                    if (trc.Start(outputPath, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => trc.WriteLine(tc.asTRC), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToTRC(this BinRXD rxd, Stream outputStream, Action<object> ProgressCallback)
        {
            try
            {
                using (TRC trc = new TRC())
                    if (trc.Start(outputStream, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => trc.WriteLine(tc.asTRC), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToCSTrace(this BinRXD rxd, string outputPath, Action<object> ProgressCallback)
        {
            try
            {
                using (CSTrace cst = new CSTrace())
                    if (cst.Start(outputPath, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => cst.WriteLine(tc.asCST), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToCSTrace(this BinRXD rxd, Stream outputStream, Action<object> ProgressCallback)
        {
            try
            {
                using (CSTrace cst = new CSTrace())
                    if (cst.Start(outputStream, rxd.DatalogStartTime))
                    {
                        rxd.ProcessTraceRecords((tc) => cst.WriteLine(tc.asCST), ProgressCallback);
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ToMatlab(this DoubleDataCollection ddata, string MatlabFileName, Action<object> ProgressCallback = null) =>
            Matlab.CreateFromDoubleData(MatlabFileName, ddata, ProgressCallback);

        public static bool ToMatlab(this DoubleDataCollection ddata, Stream MatlabStream, Action<object> ProgressCallback = null) =>
            Matlab.CreateFromDoubleData(MatlabStream, ddata, ProgressCallback);

        public static async Task<bool> Convert(BinRXD rxd, BinRXD.ExportSettings settings, string outputPath, string outputFormat = "", Action<object> ProgressCallback = null) =>
            await Convert(rxd, null, null, settings, outputPath, outputFormat, ProgressCallback);

        public static async Task<bool> Convert(BinRXD rxd, TraceCollection trace, DoubleDataCollection channels, BinRXD.ExportSettings settings, string outputPath, string outputFormat = "", Action<object> ProgressCallback = null)
        {
            bool Exported = true;
            bool isError = false;
            settings ??= new();
            try
            {
                string ext = Path.GetExtension(outputPath);
                if (rxd != null)
                {
                    if (ext.Equals(BinRXD.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!rxd.rxdUri.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
                            File.Copy(rxd.rxdUri, outputPath);
                        return true;
                    }
                    else if (ext.Equals(XmlHandler.Extension, StringComparison.OrdinalIgnoreCase))
                        return rxd.ToXML(outputPath);
                    else if (ext.Equals(MDF.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return rxd.ToMF4(outputPath, settings, ProgressCallback);
                    }
                }

                if (trace != null || rxd != null)
                {
                    Func<string, Action<object>, bool> TraceConvert = null;
                    if (ext.Equals(ASC.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToASCII : trace.ToASCII;
                    else if (ext.Equals(BLF.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToBLF : trace.ToBLF;
                    else if (ext.Equals(TRC.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToTRC : trace.ToTRC;
                    else if (ext.Equals(CSTrace.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToCSTrace : trace.ToCST;

                    if (TraceConvert != null)
                        return TraceConvert(outputPath, ProgressCallback);
                }

                if (channels != null || rxd != null)
                {
                    Func<string, Action<object>, bool> GetChannelConverter()
                    {
                        DoubleDataCollection BuildChannels(TimeFormatType csvTimeFormat = TimeFormatType.Seconds)
                        {
                            if (rxd is not null)
                                channels = rxd.ToDoubleData(settings);
                            if (channels is null || channels.Count == 0)
                                throw new Exception("There is no data channels to export!");
                            else
                            {
                                channels.DefaultCsvDateFormat = csvTimeFormat;
                                return channels;
                            }
                        }

                        bool FullDateTime = outputFormat.Contains("_FullDateTime");
                        outputFormat = outputFormat.Replace("_FullDateTime", "");

                        if (ext.Equals(Matlab.Extension, StringComparison.OrdinalIgnoreCase))
                            return BuildChannels().ToMatlab;
                        else if (ext.Equals(ApacheParquet.Extension, StringComparison.OrdinalIgnoreCase))
                            return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToApacheParquet;
                        else if (ext.Equals(DoubleDataCollection.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            if (outputFormat.Equals("InfluxDB", StringComparison.OrdinalIgnoreCase))
                                return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToInfluxDBCSV;
                            else if (outputFormat.Equals("Regional", StringComparison.OrdinalIgnoreCase))
                                return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToSV;
                            else
                                return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToCSV;
                        }
                        return null;
                    }

                    var ChannelConvert = GetChannelConverter();
                    if (ChannelConvert != null)
                        return isError = !ChannelConvert(outputPath, ProgressCallback);
                }

                Exported = false;
                return false;
            }
            catch (Exception e)
            {
                isError = true;
                LastConvertMessage = e.Message;
                LastConvertStatus = false;
                return false;
            }
            finally
            {
                if (Exported && !isError)
                {
                    LastConvertMessage = "File " + Path.GetFileName(outputPath) + " successfully exported!";
                    LastConvertStatus = true;
                }
            }
        }

        public static async Task<bool> Convert(BinRXD rxd, BinRXD.ExportSettings settings, Stream outputStream, string outputFormat = "", Action<object> ProgressCallback = null) =>
            await Convert(rxd, null, null, settings, outputStream, outputFormat, ProgressCallback);

        public static async Task<bool> Convert(BinRXD rxd, TraceCollection trace, DoubleDataCollection channels, BinRXD.ExportSettings settings, Stream outputStream, string outputFormat = "", Action<object> ProgressCallback = null)
        {
            bool Exported = true;
            bool isError = false;
            settings ??= new()
            {
                StorageCache = StorageCacheType.Memory
            };

            try
            {
                outputFormat = outputFormat.Trim();
                var tmp = outputFormat.Split(':');
                outputFormat = tmp.Length > 1 ? tmp[1] : "";
                string ext = "." + tmp[0];
                if (rxd != null)
                {
                    if (ext.Equals(BinRXD.Extension, StringComparison.OrdinalIgnoreCase))
                        return rxd.ToRXData(outputStream);
                    else if (ext.Equals(XmlHandler.Extension, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Not implemented!");
                    //return rxd.ToXML(outputPath);
                    else if (ext.Equals(MDF.Extension, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Not implemented!");
                    //return rxd.ToMF4(outputPath, settings.SignalsDatabase, ProgressCallback);
                }

                if (trace != null || rxd != null)
                {
                    Func<Stream, Action<object>, bool> TraceConvert = null;
                    if (ext.Equals(ASC.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToASCII : trace.ToASCII;
                    else if (ext.Equals(BLF.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToBLF : trace.ToBLF;
                    else if (ext.Equals(TRC.Extension, StringComparison.OrdinalIgnoreCase))
                        TraceConvert = trace is null ? rxd.ToTRC : trace.ToTRC;

                    if (TraceConvert != null)
                        TraceConvert(outputStream, ProgressCallback);
                }

                if (channels != null || rxd != null)
                {
                    Func<Stream, Action<object>, bool> GetChannelConverter()
                    {
                        DoubleDataCollection BuildChannels(TimeFormatType csvTimeFormat = TimeFormatType.Seconds)
                        {
                            channels ??= rxd.ToDoubleData(settings);
                            if (channels is null || channels.Count == 0)
                                throw new Exception("There is no data channels to export!");
                            else
                                return channels;
                        }

                        bool FullDateTime = outputFormat.Contains("_FullDateTime");
                        outputFormat = outputFormat.Replace("_FullDateTime", "");

                        if (ext.Equals(Matlab.Extension, StringComparison.OrdinalIgnoreCase))
                            return BuildChannels().ToMatlab;
                        else if (ext.Equals(ApacheParquet.Extension, StringComparison.OrdinalIgnoreCase))
                            return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToApacheParquet;
                        else if (ext.Equals(DoubleDataCollection.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            if (outputFormat.Equals("InfluxDB", StringComparison.OrdinalIgnoreCase))
                                return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToInfluxDBCSV;
                            else if (outputFormat.Equals("Regional", StringComparison.OrdinalIgnoreCase))
                                return BuildChannels(FullDateTime ? TimeFormatType.DateTime : TimeFormatType.Seconds).ToSV;
                            else
                                return BuildChannels().ToCSV;
                        }
                        return null;
                    }

                    var ChannelConvert = GetChannelConverter();
                    if (ChannelConvert != null)
                        return isError = !ChannelConvert(outputStream, ProgressCallback);
                }

                Exported = false;
                return false;
            }
            catch (Exception e)
            {
                isError = true;
                LastConvertMessage = e.Message;
                LastConvertStatus = false;
                return false;
            }
            finally
            {
                if (Exported && !isError)
                {
                    LastConvertMessage = "Data stream successfully exported!";
                    LastConvertStatus = true;
                }
            }
        }

    }
}
