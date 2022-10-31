using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants;
using WCell.Core.Initialization;
using WCell.Core.Network;
using WCell.RealmServer.Content;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Network;
using WCell.Util.Data;

namespace WCell.RealmServer.Asda2Style
{
    public static class Asda2StyleMgr
    {
        public static Dictionary<short, FaceTableRecord> FaceTemplates = new Dictionary<short, FaceTableRecord>();
        public static Dictionary<short, HairTableRecord> HairTemplates = new Dictionary<short, HairTableRecord>();

        [Initialization(InitializationPass.Tenth, "Style shop.")]
        public static void Init()
        {
            ContentMgr.Load<HairTableRecord>();
            ContentMgr.Load<FaceTableRecord>();
        }
    }
    public static class Asda2StyleHandler
    {
        [PacketHandler(RealmServerOpCode.ChangeFaceOrHair)]//5470
        public static void ChangeFaceOrHairRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            var id = packet.ReadInt16();//default : 95Len : 2
            var hairId = packet.ReadByte();//default : 1Len : 1
            var hairColor = packet.ReadByte();//default : 23Len : 1

            client.ActiveCharacter.SendErrorMsg(string.Format("Id : {0}", id));
            client.ActiveCharacter.SendErrorMsg(string.Format("HairId : {0}", hairId));
            client.ActiveCharacter.SendErrorMsg(string.Format("HairColor : {0}", hairColor));

            var template = Asda2StyleMgr.HairTemplates[id];
            if (client.Account.AccountData.Points < template.Price)
            {
                SendFaceOrHairChangedResponse(client, ChangeHairStatus.NoEnoughPoints, hairId, hairColor);
                return;
            }
            else if (client.ActiveCharacter.Level < 1)
            {
                SendFaceOrHairChangedResponse(client, ChangeHairStatus.LowLevel, hairId, hairColor);
                return;
            }
            else if ((byte)client.ActiveCharacter.Gender != template.OneOrTwo)
            {
                SendFaceOrHairChangedResponse(client, ChangeHairStatus.WrongSex, hairId, hairColor);
                return;
            }
            else
            {
                client.ActiveCharacter.SubtractPoints(template.Price);
                client.ActiveCharacter.HairColor = template.HairColor;
                client.ActiveCharacter.HairStyle = template.HairId;
                SendFaceOrHairChangedResponse(client, ChangeHairStatus.Success, hairId, hairColor);
            }
        }

        public static void SendFaceOrHairChangedResponse(IRealmClient client, ChangeHairStatus status, int HairId, int HairColor)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.FaceOrHairChanged))//5471
            {
                packet.WriteByte((byte)status);//{status}default value : 1 Len : 1 // Status
                packet.WriteInt16(1);//{hair1face2}default value : 2 Len : 1 // Hair?
                packet.WriteByte(client.ActiveCharacter.HairStyle);//{hairColor}default value : 3 Len : 1 // HairStyle
                packet.WriteByte(client.ActiveCharacter.HairColor);//{hairColor}default value : 3 Len : 1 // HairColor
                packet.WriteInt32(client.Account.AccountData.Points);//{hairId}default value : 3 Len : 1 // Points
                client.Send(packet, addEnd: true);
            }
        }

    }
    [DataHolder]
    public class FaceTableRecord : IDataHolder
    {
        public short Id { get; set; }
        public byte IsEnabled { get; set; }
        public byte OneOrTwo { get; set; }
        public int FaceId { get; set; }
        public int Price { get; set; }
        public int CuponCount { get; set; }
        public void FinalizeDataHolder()
        {
            if (Asda2StyleMgr.FaceTemplates.ContainsKey(Id))
                return;
            Asda2StyleMgr.FaceTemplates.Add(Id, this);
        }
    }
    [DataHolder]
    public class HairTableRecord : IDataHolder
    {
        public int Id { get; set; }
        public byte IsEnabled { get; set; }
        public byte HairId { get; set; }
        public byte OneOrTwo { get; set; }
        public byte HairColor { get; set; }
        public int Price { get; set; }
        public string Name { get; set; }
        public void FinalizeDataHolder()
        {
            if (Asda2StyleMgr.HairTemplates.ContainsKey((short)Id))
                return;
            Asda2StyleMgr.HairTemplates.Add((short)Id, this);
        }
    }

    public enum ChangeHairStatus
    {
        Success = 1,
        NoEnoughPoints = 2,
        LowLevel = 3,
        WrongSex = 4,
        CantUseItInSoulMateMode = 5
    }
}
