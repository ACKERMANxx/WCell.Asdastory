namespace WCell.Core.Network
{
    public unsafe class Asda1Cipher
    {
        public static uint[] Keys = new uint[8] { 0x9b60ab17, 0x9979c408, 0x7a5de3b7, 0xf90dc791, 0x1e617d99,
            0xa1caf267,0xb9296234,0x42169ab3 };
        public static uint[] KeysGenerate = new uint[8];
        public unsafe static void CryptPacket(byte[] BufferTarget,int BufferOffset = 0)
        {
            fixed (byte* BufferPointer = BufferTarget)
            {
                fixed (uint* KeysPointer = Keys)
                {
                    fixed (uint* KeysGeneratePointer = KeysGenerate)
                    {
                        var BufferLength = ((uint)*(ushort*)(BufferPointer + BufferOffset + 1) - 6) >> 3;
                        byte* BasePacket = BufferPointer + BufferOffset + 3;
                        int CountAll = 0;
                        while (CountAll < BufferLength)
                        {
                            int CountBytesinInt = 0;
                            while (CountBytesinInt < 4 && CountAll < BufferLength)
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
                        }
                    }
                }
            }

        }
    }
}