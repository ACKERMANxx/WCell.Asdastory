/*using System;
using WCell.Constants;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Network;
using WCell.RealmServer.Quests;

namespace WCell.RealmServer.Handlers
{
    public static class Asda2QuestHandler
    {

        public static void SendQuestsListResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.QuestsList))//5047
            {
                var qr = QuestRecord.GetQuestRecordForCharacter(client.ActiveCharacter.EntityId.Low);
                /*for (int i = 0; i < 11; i++)
                {
                    packet.WriteInt32(-1);//{questId}default value : -1 Len : 4
                    packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                    packet.WriteInt16(i+1);//{questSlot}default value : -1 Len : 2
                    packet.WriteByte(0);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
                    packet.WriteInt16(-1);//{oneMoreQuestId}default value : -1 Len : 2
                    packet.WriteInt16(0);//{IsCompleted}default value : 2 Len : 2  0 or 1
                    packet.WriteInt16(-1);//value name : unk2 default value : -1Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                }
                for (int i = 0; i < 1; i++)
                {
                    packet.WriteByte(250);
                }
                for (int i = 0; i < 149; i++)
                {
                    packet.WriteByte((int)byte.MaxValue);
                }
                client.Send(packet, addEnd: false);*/
/*for (int i = 0; i < qr.Length; i++)
{
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].QuestFileId);//{questId}default value : -1 Len : 4
    packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
    packet.WriteInt16(qr[i] == null ? -1 : qr[i].Slot);//{questSlot}default value : -1 Len : 2
    packet.WriteByte(qr[i] == null ? 0 : qr[i].QuestStage);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
    packet.WriteInt16(qr[i] == null ? -1 : qr[i].QuestTemplateId);//{oneMoreQuestId}default value : -1 Len : 2
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].CompleteStatus);//{IsCompleted}default value : 2 Len : 2  0 or 1
    packet.WriteInt16(-1);//value name : unk2 default value : -1Len : 2
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item1Id);//{questItemId}default value : -1 Len : 4
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item1Amount);//{questItemAmount}default value : 0 Len : 2
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item2Id);//{questItemId}default value : -1 Len : 4
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item2Amount);//{questItemAmount}default value : 0 Len : 2
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item3Id);//{questItemId}default value : -1 Len : 4
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item3Amount);//{questItemAmount}default value : 0 Len : 2
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item4Id);//{questItemId}default value : -1 Len : 4
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item4Amount);//{questItemAmount}default value : 0 Len : 2
    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item5Id);//{questItemId}default value : -1 Len : 4
    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item5Amount);//{questItemAmount}default value : 0 Len : 2
}
//var completedquests = Asda2CompletedQuests.LoadAllRecordsFor(client.ActiveCharacter.EntityId.Low);

/*for (int i = 0; i < 1; i++)
{
    packet.WriteByte(254);

}

//Console.WriteLine(packet.ToHexDump());
for (int i = 0; i < 155; i++)
{

    if ( i ==4)
    packet.WriteInt16(90);
    else
    if (i == 9)
        packet.WriteInt16(-1);
    else
    packet.WriteInt16(91);
}             

for (int i = 0; i < 1; i++)
{
    packet.WriteByte(8);
}*/
/*client.ActiveCharacter.Send(packet,true);

}
}

}
}*/

using WCell.Constants;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Network;
using WCell.RealmServer.Quests;

namespace WCell.RealmServer.Handlers
{
    public static class Asda2QuestHandler
    {

        public static void SendQuestsListResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.QuestsList))//5047
            {
                var qr = QuestRecord.GetQuestRecordForCharacter(client.ActiveCharacter.EntityId.Low);
                var cq = Asda2CompletedQuests.LoadAllRecordsFor(client.ActiveCharacter.EntityId.Low);
                for (int i = 0; i < qr.Length; i++)
                {
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].QuestTemplateId);//{questId}default value : -1 Len : 4
                    packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                    packet.WriteInt16(qr[i] == null ? -1 : qr[i].Slot);//{questSlot}default value : -1 Len : 2
                    packet.WriteByte(qr[i] == null ? 0 : qr[i].QuestStage);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
                    packet.WriteInt16(qr[i] == null ? -1 : qr[i].QuestFileId);//{oneMoreQuestId}default value : -1 Len : 2
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].CompleteStatus);//{IsCompleted}default value : 2 Len : 2  0 or 1
                    packet.WriteInt16(-1);//value name : unk2 default value : -1Len : 2
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item1Id);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item1Amount);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item2Id);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item2Amount);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item3Id);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item3Amount);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item4Id);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item4Amount);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(qr[i] == null ? -1 : qr[i].Item5Id);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(qr[i] == null ? 0 : qr[i].Item5Amount);//{questItemAmount}default value : 0 Len : 2
                }
                for (int i = 0; i < 1; i++)
                {
                    packet.WriteByte(254);
                }
                for (int i = 0; i < 149; i++)
                {
                    packet.WriteByte((int)byte.MaxValue);
                }
                client.Send(packet, addEnd: true);
            }
        }

        public static void SendQuestsListResponseTest(IRealmClient client, int id)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.QuestsList))//5047
            {
                //var qr = QuestRecord.GetQuestRecordForCharacter(client.ActiveCharacter.EntityId.Low);
                for (int i = 0; i < 12; i++)
                {
                    packet.WriteInt32(id);//{questId}default value : -1 Len : 4
                    packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                    packet.WriteInt16(i);//{questSlot}default value : -1 Len : 2
                    packet.WriteByte(2);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
                    packet.WriteInt16(id);//{oneMoreQuestId}default value : -1 Len : 2
                    packet.WriteInt16(0);//{IsCompleted}default value : 2 Len : 2  0 or 1
                    packet.WriteInt16(-1);//value name : unk2 default value : -1Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                    packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                    packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                }
                for (int i = 0; i < 1; i++)
                {
                    packet.WriteByte(254);
                }
                for (int i = 0; i < 149; i++)
                {
                    packet.WriteByte((int)byte.MaxValue);
                }
                client.Send(packet, addEnd: false);
            }
        }

    }
}