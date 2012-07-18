namespace USBTrace_BTSnoop
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Globalization; 
    #endregion

    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine(" First argument: CSV File");
                return;
            }

            var bts = new BTSnoop()
                {
                    datalink= BTSnoop.DataLayerLink.H4,
                    Version = 1
                };

            var filename = args[0];

            var csv = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
                       
            while (csv.BaseStream.Position < csv.BaseStream.Length)
            {
                string line = csv.ReadLine();
                var entries = line.Split(new char[] { ',' });

                // nur Bluetooth Entries
                if (entries[1] == "Bluetooth")
                {
                    double seconds = double.Parse(entries[2], CultureInfo.InvariantCulture.NumberFormat);
                    DateTime timeStamp = new DateTime(2012, 01, 01).AddSeconds(seconds);                   

                    var dt = entries[9].Split(new Char[] { ' ' },StringSplitOptions.RemoveEmptyEntries);


                    var data = new Byte[dt.Length+1];
                    for (int i = 0; i < dt.Length; i++)
                    {
                        data[i + 1] = Byte.Parse(dt[i], System.Globalization.NumberStyles.HexNumber);
                    }
                    
                     UInt32 flag = 0;

                    switch (entries[5])
                    {
                        case "0":
                            flag = (UInt32)BTSnoop.BTSnoopDirectionFlags.HostToController | (UInt32)BTSnoop.BTSnoopACLCommand.CommandOrEvent;
                            data[0] = (byte)BTSnoop.HCI_H4_TYPE.CMD;
                            break;
                        case "2":
                            flag = (UInt32)BTSnoop.BTSnoopDirectionFlags.HostToController | (UInt32)BTSnoop.BTSnoopACLCommand.ACLDataFrame;
                            data[0] = (byte)BTSnoop.HCI_H4_TYPE.ACL;
                            break;
                        case "81":
                            flag = (UInt32)BTSnoop.BTSnoopDirectionFlags.ControllerToHost | (UInt32)BTSnoop.BTSnoopACLCommand.CommandOrEvent;
                            data[0] = (byte)BTSnoop.HCI_H4_TYPE.EVT;
                            break;
                        case "82":
                            flag = (UInt32)BTSnoop.BTSnoopDirectionFlags.ControllerToHost | (UInt32)BTSnoop.BTSnoopACLCommand.ACLDataFrame;
                            data[0] = (byte)BTSnoop.HCI_H4_TYPE.ACL;
                            break;

                    }

                    var Rec = new BTSnoopRecord()
                    {
                        Timestamp = timeStamp,
                        cum_drops = 0,
                        flags = (UInt32)flag,
                        orig_len = (UInt32)data.Length,
                        Data = data
                    };
                    bts.Records.Add(Rec);
                }

            }

            bts.SaveFile(Path.ChangeExtension(filename,".log"));        
        }
    }
}
