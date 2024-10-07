using InfluxShared.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RXD.Blocks
{

    public class BinConfigWiFi : BinBase
    {
        public enum WiFi_Security_Type : byte
        {
            Open,
            WEP,
            WPA,
            WPA2
        }

        internal enum BinProp
        {
            NameSize,
            Name,
            UseDHCP,
            SSIDSize,
            SSID,
            SecurityType,
            PSKSize,
            PSK,
            StaticIP,
            Gateway,
            Mask,
            DNS,
            RealTimeSource,
            NoCommunicationRestartTimeOut,
            RemoteStorageType
        }

        #region Do not touch these
        public BinConfigWiFi(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
                data.AddProperty(BinProp.UseDHCP, typeof(bool));
                data.AddProperty(BinProp.SSIDSize, typeof(byte));
                data.AddProperty(BinProp.SSID, typeof(string), BinProp.SSIDSize);
                data.AddProperty(BinProp.SecurityType, typeof(WiFi_Security_Type));
                data.AddProperty(BinProp.PSKSize, typeof(byte));
                data.AddProperty(BinProp.PSK, typeof(string), BinProp.PSKSize);
                data.AddProperty(BinProp.StaticIP, typeof(IPAddress));
                data.AddProperty(BinProp.Gateway, typeof(IPAddress));
                data.AddProperty(BinProp.Mask, typeof(IPAddress));
                data.AddProperty(BinProp.DNS, typeof(IPAddress));
                data.AddProperty(BinProp.RealTimeSource, typeof(RealTimeSourceType));
                data.AddProperty(BinProp.NoCommunicationRestartTimeOut, typeof(uint));
                data.AddProperty(BinProp.RemoteStorageType, typeof(RemoteStorageType));
            });
        }
    }
}
