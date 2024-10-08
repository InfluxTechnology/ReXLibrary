using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

/* 
 * ------------------------------------
 * Author:  Emanuel Feru
 * Year:    2022
 * 
 * Copyright (C) Emanuel Feru
 * ------------------------------------
 */

namespace DbcParserLib
{
    public class Message
    {
        public uint ID { get; set; }
        public DBCMessageType Type { get; set; }
        public bool BRS { get; set; } //1 == true
        public string Name { get; set; }
        public byte DLC { get; set; }
        public string Transmitter { get; set; }
        public string Comment { get; set; }
        public int CycleTime { get; set; }
        public List<Signal> Signals = new List<Signal>();
        public NameValueCollection AttrValues = new NameValueCollection();
    }

    public class Signal
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public ushort StartBit { get; set; }
        public byte Length { get; set; }
        public byte ByteOrder { get; set; } = 1;
        public DBCValueType ValueType { get; set; }
        public double InitialValue { get; set; }
        public double Factor { get; set; } = 1;
        public double Offset { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public string Unit { get; set; }
        public string[] Receiver { get; set; }
        public string ValueTable { get; set; }
        public SortedDictionary<double, double> TableNumeric { get; set; } = null;
        public SortedDictionary<double, string> TableVerbal { get; set; } = null;
        public string Comment { get; set; }
        public string Multiplexing { get; set; }
        public NameValueCollection AttrValues = new NameValueCollection();
    }

    /* ------------------------------------
     * Author:  Georgi Georgiev
     * Year:    2023
     * 
     * Company: Influx Technology LTD
     * ------------------------------------
     */
    public enum DataType { Int, Hex, Float, Enum, String }

    public enum ApplyTo { Project, Node, Message, Signal, EnvVar }

    public enum EnvVarType { Int, Float, String, Data }

    public class DBCAttribute
    {
        public string Name;
        public DataType DataType;
        public string Min;
        public string Max;
        public string Default;
        public ApplyTo ApplyTo;
        public List<String> Enums = new List<String>();
    }

    public class EnvVariable
    {
        public string Name;
        public EnvVarType EnvVarType;
        public double InitialValue;
        public double Minimum;
        public double Maximum;
        public uint MaxLength;
        public string Units;
        public bool AllowRead;
        public bool AllowWrite;
        public string Comment;
        public NameValueCollection AttrValues = new NameValueCollection();
        public List<string> Nodes = new List<string>();
    }

}
