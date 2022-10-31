using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCell.RealmServer.Network
{
    public unsafe class Asda1Cipher
    {
        public static uint[] Keys = new uint[8] { 0x57C57242, 0x8B113D35, 0x59947769, 0xD5699555, 0xAA9A3C95, 0xCD15E120, 0x4E8AA50, 0xB4968740 };
        public static uint[] KeysGenerate = new uint[8];
        public unsafe static void CryptPacket(byte[] BufferTarget, int BufferOffset = 0)
        {
            fixed (byte* BufferPointer = BufferTarget)
            {
                fixed (uint* KeysPointer = Keys)
                {
                    fixed (uint* KeysGeneratePointer = KeysGenerate)
                    {
                        var BufferLength = ((uint)*(ushort*)(BufferPointer + BufferOffset + 1) - 4) >> 3;
                        byte* BasePacket = BufferPointer + BufferOffset + 3;
                        int CountAll = 0;
                        while (CountAll < BufferLength)
                        {
                            int CountBytesinInt = 0;
                            while (CountBytesinInt < 4 && CountAll < BufferLength)
                            {
                                try
                                {

                                *(KeysGeneratePointer + 2 * CountBytesinInt) = *(uint*)(BasePacket + 8 * CountAll);
                                *((KeysGeneratePointer + 1) + 2 * CountBytesinInt) = *(uint*)(BasePacket + 8 * CountAll + 4);
                                *(KeysGeneratePointer + 2 * CountBytesinInt) ^= *(KeysPointer + 2 * CountBytesinInt);
                                *((KeysGeneratePointer + 1) + 2 * CountBytesinInt) ^= *((KeysPointer + 1) + 2 * CountBytesinInt);
                                *(uint*)(BasePacket + 8 * CountAll) = *(KeysGeneratePointer + 2 * CountBytesinInt);
                                *(uint*)(BasePacket + 8 * CountAll + 4) = *((KeysGeneratePointer + 1) + 2 * CountBytesinInt);
                                CountBytesinInt++;
                                CountAll++;
                                }
                                catch
                                {
                                    Console.WriteLine("There is error in packets");
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}
