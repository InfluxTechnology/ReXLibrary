using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    internal class BinUDS: BinBase
    {
        internal enum BinProp
        {
            NameSize,
            Name,
            TesterMessageUID,
            ModuleMessageUID,
            FirstUDSMessageUID,
            TesterPresentTime,
            NoResponseTimeout
        }

        #region Do not touch these
        public BinUDS(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.TesterMessageUID, typeof(ushort));
                data.AddProperty(BinProp.ModuleMessageUID, typeof(ushort));
                data.AddProperty(BinProp.FirstUDSMessageUID, typeof(ushort));
                data.AddProperty(BinProp.TesterPresentTime, typeof(ushort));
                data.AddProperty(BinProp.NoResponseTimeout, typeof(ushort), 1000);

                AddInput("UID");
                AddOutput(BinProp.FirstUDSMessageUID.ToString());
            });
        }
    }
}
