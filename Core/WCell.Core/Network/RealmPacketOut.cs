/*************************************************************************
 *
 *   file		: RealmPacketOut.cs
 *   copyright		: (C) The WCell Team
 *   email		: info@wcell.org
 *   last changed	: $LastChangedDate: 2008-05-04 00:27:17 +0800 (Sun, 04 May 2008) $
 *   last author	: $LastChangedBy: domiii $
 *   revision		: $Rev: 314 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using System;
using System.Text;
using WCell.Constants;
using System.Linq;
using WCell.Util.Variables;

namespace WCell.Core.Network
{
    /// <summary>
    /// Packet sent to the realm server
    /// </summary>
    public class RealmPacketOut : PacketOut
    {

        /// <summary>
        /// Constant indicating this <c>RealmPacketOut</c> header size.
        /// </summary>
        public const int HEADER_SIZE = 4;

        public const int FullUpdatePacketHeaderSize = HEADER_SIZE + 4;

        public const int MaxPacketSize = ushort.MaxValue;

        public static void WriteHeader(byte[] buffer, int offset, int length, PacketId opcode)
        {

        }

        public RealmPacketOut(PacketId packetId)
            : this(packetId, 128)
        {
        }

      
        public RealmPacketOut(RealmServerOpCode packetOpCode)
            : base(new PacketId(packetOpCode))
        {
            base.WriteByte(0xFB);//0
            base.WriteInt16(0);//packetLen//1-2
            base.WriteInt32(0);//4-5-6-7
            base.WriteInt16((ushort)packetOpCode);
        }
        public RealmPacketOut(PacketId packetId, int maxContentLength)
            : base(packetId, maxContentLength + HEADER_SIZE)
        {
            Position += 2;						// length
            Write((ushort)packetId.RawId);
        }

        /// <summary>
        /// The <c>RealmPacketOut</c> header size.
        /// </summary>
        public override int HeaderSize
        {
            get { return HEADER_SIZE; }
        }

        /// <summary>
        /// The opcode of the packet
        /// </summary>
        public RealmServerOpCode OpCode
        {
            get { return (RealmServerOpCode)PacketId.RawId; }
            set
            {
                if (OpCode != value)
                {
                    var pos = Position;
                    Position = 7;
                    WriteShort((short)value);
                    Position = pos;
                }
            }
        }

        public bool? FinalizedForRus { get; set; }


        /// <summary>
        /// todo optimize perfomance by sortin eng rus clients
        /// </summary>       
        /// 
        public bool Finilialized = false;
        public void FinalizeAsda()
        {
            //Asda2CryptHelper.XorData(BufferSegment.Buffer.Array, BufferSegment.Offset + 3, Position - 3, EncodedLocale, Locale.Ar);
            //Console.WriteLine("S -> C : " + OpCode);
            if (Finilialized == true)
                return;
            //if (addend)
            WriteByte(0xFE);
            var packetLen = (short)Position;
            Array.Copy(BitConverter.GetBytes(packetLen), 0, BufferSegment.Buffer.Array,
                       BufferSegment.Offset + 1, 2);
            Array.Copy(new byte[] { 0xFB }, 0, BufferSegment.Buffer.Array,
                       BufferSegment.Offset , 1);
            Asda1Cipher.CryptPacket(BufferSegment.Buffer.Array, BufferSegment.Offset);
            Finilialized = true;
            if (packetLen >= 700)
            {
                throw new InvalidOperationException(string.Format("Packet {0} Len >= 700",this));
            }

        }
        /// <summary>
        /// Finalize packet data
        /// </summary>
        protected override void FinalizeWrite()
        {
            base.FinalizeWrite();

            Position = 0;
            WriteUShortBE((ushort)(BaseStream.Length - 2));
        }

        public void WriteSkip(byte[] bytes)
        {
            Write(bytes);
        }

        public void WriteFixedAsciiString(string str, int len, Locale locale = Locale.En)
        {
            WriteAsdaString(str, len, locale);
        }

        public void WriteAsciiString(string msg, Locale locale = Locale.En)
        {
            var msgData = Asda2EncodingHelper.Encode(msg, locale);
            Write(msgData);
            WriteByte(0);
        }

    }
}