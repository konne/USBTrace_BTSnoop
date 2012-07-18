namespace USBTrace_BTSnoop
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Runtime.InteropServices;
    #endregion

    #region BinaryWriterExtension
    public static class BinaryWriterExtension
    {
        public static void WriteStruct<T>(this BinaryWriter bw, T data, List<int> sizes) where T : struct
        {
            Type resType = data.GetType();

            byte[] buff = new byte[Marshal.SizeOf(resType)];

            IntPtr ptr = Marshal.AllocHGlobal(buff.Length);
            Marshal.StructureToPtr((object)data, ptr, true);
            Marshal.Copy(ptr, buff, 0x0, buff.Length);
            Marshal.FreeHGlobal(ptr);

            byte[] writebuffer;

            if (sizes != null)
            {
                writebuffer = new byte[buff.Length];
                int pos = 0;
                foreach (var item in sizes)
                {
                    for (int i = 0; i < item; i++)
                    {
                        writebuffer[pos + i] = buff[pos + item - 1 - i];
                    }
                    pos += item;
                }
            }
            else
            {
                writebuffer = buff;
            }


            bw.Write(writebuffer, 0, buff.Length);
        }



        public static T ReadStruct<T>(this BinaryReader br, List<int> sizes) where T : struct
        {
            T result = default(T);
            var resType = result.GetType();

            int count = Marshal.SizeOf(resType);
            byte[] readBuffer;

            var readBufferOrg = br.ReadBytes(count);

            if (sizes != null)
            {
                readBuffer = new byte[count];
                int pos = 0;
                foreach (var item in sizes)
                {
                    for (int i = 0; i < item; i++)
                    {
                        readBuffer[pos + i] = readBufferOrg[pos + item - 1 - i];
                    }
                    pos += item;
                }
            }
            else
            {
                readBuffer = readBufferOrg;
            }


            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), resType);
            handle.Free();

            return result;
        }
    } 
    #endregion
}
