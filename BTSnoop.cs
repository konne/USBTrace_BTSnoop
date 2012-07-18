namespace USBTrace_BTSnoop
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.IO; 
    #endregion

    #region BTSnoopRecord
    public class BTSnoopRecord
    {
        public UInt32 cum_drops { get; set; }

        public UInt32 flags { get; set; }

        public DateTime Timestamp { get; set; }

        public UInt32 orig_len { get; set; }

        public byte[] Data { get; set; }
    } 
    #endregion

    #region BTSnoop
    public class BTSnoop
    {
        #region Variables
        static Int64 SymbianTimeBaseDiffToUnixTimeBase = 0x00dcddb30f2f8000;

        static private char[] btsnoop_magic = { 'b', 't', 's', 'n', 'o', 'o', 'p', '\0' };

        public UInt32 Version { get; set; }
        public DataLayerLink datalink { get; set; }
        public List<BTSnoopRecord> Records = new List<BTSnoopRecord>();
        #endregion

        #region BTSnoop Structs
        [StructLayout(LayoutKind.Sequential)]
        private struct FileHeader
        {
            /// <summary>
            ///  version number (should be 1) 
            /// </summary>
            public UInt32 Version;
            /// <summary>
            /// datalink type 
            /// </summary>
            public UInt32 datalink;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RecordHeader
        {
            /// <summary>
            /// actual length of packet 
            /// </summary>
            public UInt32 orig_len;
            /// <summary>
            /// number of octets captured in file
            /// </summary>
            public UInt32 incl_len;
            /// <summary>
            /// packet flags 
            /// </summary>
            public UInt32 flags;
            /// <summary>
            /// cumulative number of dropped packets
            /// </summary>
            public UInt32 cum_drops;
            /// <summary>
            /// timestamp microseconds
            /// </summary>
            public Int64 ts_usec;

            //public DateTime Timestamp
            //{
            //    get
            //    {
            //        return DateTime.Now;
            //    }
            //}

        }
        #endregion

        #region Enums
        public enum DataLayerLink
        {
            /// <summary>
            /// H1 is unframed data with the packet type encoded in the flags field of capture header
            /// It can be used for any datalink by placing logging above the datalink layer of HCI
            /// </summary>
            H1 = 1001,
            /// <summary>
            /// H4 is the serial HCI with packet type encoded in the first byte of each packet
            /// </summary>
            H4 = 1002,
            /// <summary>
            /// CSR's PPP derived bluecore serial protocol - in practice we log in H1 format after deframing
            /// </summary>
            BCSP = 1003,
            /// <summary>
            /// H5 is the official three wire serial protocol derived from BCSP
            /// </summary>
            H5 = 1004
        }

        [Flags]
        public enum BTSnoopDirectionFlags
        {
            HostToController = 0x00,
            ControllerToHost = 0x01,
        }

        [Flags]
        public enum BTSnoopACLCommand
        {
            ACLDataFrame = 0x00,
            CommandOrEvent = 0x02,
        }

        public enum HCI_H4_TYPE
        {
            CMD = 0x01,
            ACL = 0x02,
            EVT = 0x04,
        }
        #endregion

        #region HelperFunction HCI4Flags
        private static UInt32 HCI4Flags(bool sent, HCI_H4_TYPE type)
        {
            if (sent && type == HCI_H4_TYPE.ACL) return (UInt32)BTSnoopDirectionFlags.HostToController | (UInt32)BTSnoopACLCommand.ACLDataFrame;
            if (!sent && type == HCI_H4_TYPE.ACL) return (UInt32)BTSnoopDirectionFlags.ControllerToHost | (UInt32)BTSnoopACLCommand.ACLDataFrame;

            if (sent && type == HCI_H4_TYPE.CMD) return (UInt32)BTSnoopDirectionFlags.HostToController | (UInt32)BTSnoopACLCommand.CommandOrEvent;
            if (!sent && type == HCI_H4_TYPE.EVT) return (UInt32)BTSnoopDirectionFlags.ControllerToHost | (UInt32)BTSnoopACLCommand.CommandOrEvent;

            throw new Exception("no HCIFlag found");
        }
        #endregion

        #region OpenFile
        public void OpenFile(string FileName)
        {
            var bw = new BinaryReader(File.OpenRead(FileName));
            var buffer = new byte[btsnoop_magic.Length];
            bw.Read(buffer, 0, buffer.Length);

            var fh = bw.ReadStruct<FileHeader>(new List<int>() { 4, 4 });

            this.datalink = (DataLayerLink)fh.datalink;

            this.Version = fh.Version;

            while (bw.BaseStream.Position < bw.BaseStream.Length)
            {
                var record = bw.ReadStruct<RecordHeader>(new List<int>() { 4, 4, 4, 4, 8 });

                var recbuf = new byte[record.incl_len];

                bw.Read(recbuf, 0, recbuf.Length);

                var Record = new BTSnoopRecord()
                {
                    cum_drops = record.cum_drops,
                    flags = record.flags,
                    orig_len = record.orig_len,
                    Timestamp = new DateTime(1970, 1, 1).AddMilliseconds((record.ts_usec - SymbianTimeBaseDiffToUnixTimeBase) / 1000.0),
                    Data = recbuf,
                };
                this.Records.Add(Record);
            }

            bw.Close();
        }
        #endregion

        #region SaveFile
        public void SaveFile(string FileName)
        {
            var bw = new BinaryWriter(File.Create(FileName));

            bw.Write(btsnoop_magic, 0, btsnoop_magic.Length);
            var fh = new FileHeader() { datalink = (UInt32)datalink, Version = Version };

            bw.WriteStruct<FileHeader>(fh, new List<int>() { 4, 4 });

            foreach (var item in Records)
            {
                var record = new RecordHeader()
                {
                    cum_drops = item.cum_drops,
                    flags = item.flags,
                    incl_len = (UInt32)item.Data.Length,
                    orig_len = item.orig_len,
                    ts_usec = (Int64)((item.Timestamp - new DateTime(1970, 1, 1)).TotalMilliseconds * 1000 + SymbianTimeBaseDiffToUnixTimeBase)

                };
                bw.WriteStruct<RecordHeader>(record, new List<int>() { 4, 4, 4, 4, 8 });
                bw.Write(item.Data, 0, item.Data.Length);
            }

        }
        #endregion
    } 
    #endregion
}
