using WCell.Constants;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Network;
using System.Linq;
using WCell.Util;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Items;
using System;
using System.Threading;
using WCell.Core.Database;
using Castle.ActiveRecord.Queries;
using System.Data.SqlClient;

namespace WCell.RealmServer.Asda2Quests
{
    class Asda2BBQuestHandler
    {
        [PacketHandler(RealmServerOpCode.AcceptBBQuest)] //5253
        public static void U6591(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;

            var questId = packet.ReadInt16();
            if (client.ActiveCharacter.GodMode)
                client.ActiveCharacter.SendErrorMsg("" + questId);
            var qr = Asda2BBQuestRecord.GetRecordByID(questId);
            var chr = client.ActiveCharacter;
            Asda2CompletedQuests c = null;
            var cq = Asda2CompletedQuests.LoadAllRecordsFor(chr.EntityId.Low);
            var cqr = cq.FirstOrDefault(cqi => cqi.SpellId == questId);
            if (client.ActiveCharacter.Level < qr.StartLevel || client.ActiveCharacter.Level > qr.EndLevel)
            {
                client.ActiveCharacter.SendQuestMsg("مستوى خاطئ");
                return;
            }
            if (cqr != null && cqr.SpecIndex >= qr.RepeatCount)
            {
                client.ActiveCharacter.SendQuestMsg("لقد وصلت للحد الاعلى لاستلام هذه المهمة اليوم");
                return;
            }

            if (client.ActiveCharacter.CheckQuest(questId))
            {

                if (cqr == null || !cq.Contains(cqr))
                {
                    c = new Asda2CompletedQuests(questId, chr.EntityId.Low, 1);
                    c.Save();
                }
                else
                {
                    cqr.SpecIndex += 1;
                    cqr.Update();
                    cqr.Save();
                }
                var questRecord = QuestRecord.GetQuestRecordForCharacter(client.ActiveCharacter.EntityId.Low);
                var qrf = questRecord.FirstOrDefault(qt => qt.QuestTemplateId == questId);

                if (qrf.CompleteStatus == 1)
                {
                    client.ActiveCharacter.Money += (uint)qr.Gold;
                    client.ActiveCharacter.GainXp(qr.Exp, "BulltinBoardQuests", true);
                    qrf.Delete();
                    Asda2Item it1 = null;
                    Asda2Item it2 = null;
                    Asda2Item it3 = null;
                    Asda2Item it4 = null;
                    Asda2Item it5 = null;
                    if (qr.Item1Id != -1)
                    {
                        it1 = chr.Asda2Inventory.QuestItems.FirstOrDefault(it => it.Template.Id == qr.Item1Id);
                        if (it1.Amount == -1)
                            it1.Amount = 0;
                        it1.Destroy();
                    }
                    if (qr.Item2Id != -1)
                    {
                        it2 = chr.Asda2Inventory.QuestItems.FirstOrDefault(it => it.Template.Id == qr.Item2Id);
                        if (it2.Amount == -1)
                            it2.Amount = 0;
                        it2.Destroy();
                    }
                    if (qr.Item3Id != -1)
                    {
                        it3 = chr.Asda2Inventory.QuestItems.FirstOrDefault(it => it.Template.Id == qr.Item3Id);
                        if (it3.Amount == -1)
                            it3.Amount = 0;
                        it3.Destroy();
                    }
                    if (qr.Item4Id != -1)
                    {
                        it4 = chr.Asda2Inventory.QuestItems.FirstOrDefault(it => it.Template.Id == qr.Item4Id);
                        if (it4.Amount == -1)
                            it4.Amount = 0;
                        it4.Destroy();
                    }
                    if (qr.Item5Id != -1)
                    {
                        it5 = chr.Asda2Inventory.QuestItems.FirstOrDefault(it => it.Template.Id == qr.Item5Id);
                        if (it5.Amount == -1)
                            it5.Amount = 0;
                        it5.Destroy();
                    }
                    unsetQuests(client.ActiveCharacter);
                    
                    Handlers.Asda2QuestHandler.SendQuestsListResponse(client);
                }
                else
                {
                    client.ActiveCharacter.SendQuestMsg("لقد قمت باستلام هذه المهمة بالفعل او لم تكملها..");
                    SendBulltinAccept(client, -1, questId);
                    return;
                }
            }
            else
            {
                var Rec = new QuestRecord(questId, questId, client.ActiveCharacter.EntityId.Low, Asda2QuestMgr.FindFreeSlot(client.ActiveCharacter), qr.Item1Id, qr.Item2Id, qr.Item3Id, qr.Item4Id, qr.Item5Id, qr.Item1Amount, qr.Item2Amount, qr.Item3Amount, qr.Item4Amount, qr.Item5Amount, qr.Monster1Id, qr.Monster2Id, qr.Monster3Id, qr.Monster4Id, qr.Monster5Id, 1, 0);
                Rec.Create();
                switch (Rec.Slot)
                {
                    case 1:
                        chr.SetQuest1(questId);
                        break;
                    case 2:
                        chr.SetQuest2(questId);
                        break;
                    case 3:
                        chr.SetQuest3(questId);
                        break;
                    case 4:
                        chr.SetQuest4(questId);
                        break;
                    case 5:
                        chr.SetQuest5(questId);
                        break;
                    case 6:
                        chr.SetQuest6(questId);
                        break;
                    case 7:
                        chr.SetQuest7(questId);
                        break;
                    case 8:
                        chr.SetQuest8(questId);
                        break;
                    case 9:
                        chr.SetQuest9(questId);
                        break;
                    case 10:
                        chr.SetQuest10(questId);
                        break;
                    case 11:
                        chr.SetQuest11(questId);
                        break;
                    case 12:
                        chr.SetQuest12(questId);
                        break;
                }
                var questRecord = QuestRecord.GetQuestRecordForCharacter(client.ActiveCharacter.EntityId.Low);
                using (var qpacket = new RealmPacketOut(RealmServerOpCode.YouGotQuest))
                {
                    qpacket.WriteByte(0);
                    qpacket.WriteInt16(0);
                    qpacket.WriteInt32(Convert.ToInt32(qr.Id));
                    qpacket.WriteByte(4); // idk default : 4
                    qpacket.WriteByte(Rec.Slot); // slot maybe? 
                    qpacket.WriteByte(0); //idk
                    qpacket.WriteByte(2); // idk
                    qpacket.WriteInt32(Convert.ToInt32(qr.Id));
                    qpacket.WriteInt16(15014); // idk
                    qpacket.WriteInt32(qr.Item1Id); // req item
                    qpacket.WriteInt16(0); // amount req item
                    qpacket.WriteInt32(qr.Item2Id); // req item
                    qpacket.WriteInt16(0); // amount req item
                    qpacket.WriteInt32(qr.Item3Id); // req item
                    qpacket.WriteInt16(0); // amount req item
                    qpacket.WriteInt32(qr.Item4Id); // req item
                    qpacket.WriteInt16(0); // amount req item
                    qpacket.WriteInt32(qr.Item5Id); // req item
                    qpacket.WriteInt16(0); // amount req item
                    chr.Send(qpacket, true);
                }
                using (var qpacket = new RealmPacketOut(RealmServerOpCode.GetRaidQuestRespond))
                {
                    qpacket.WriteByte(0);
                    qpacket.WriteInt16(0);
                    qpacket.WriteInt32(Convert.ToInt32(qr.Id));
                    qpacket.WriteInt16(0);
                    qpacket.WriteByte(1);
                    qpacket.WriteInt16(0);
                    qpacket.WriteByte(1);
                    chr.Send(qpacket, true);
                }
                //Handlers.Asda2QuestHandler.SendQuestsListResponse(client);
                SendBulltinAccept(client, 25, questId);
            }

        }

        public static void SendBulltinAccept(IRealmClient client, int status, int questid)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.AcceptQuestRes))
            {
                packet.WriteByte(status); // status
                packet.WriteInt16(questid); // questIdByFileName
                packet.WriteInt32(questid); // questId
                packet.WriteInt32(questid); // questIdByFileName
                packet.WriteInt32(questid); // questIdByFileName
                client.Send(packet);
                //packet.WriteByte(0);

            }
        }

        public static void unsetQuests(Character chr)
        {
            var questRecord = Database.QuestRecord.GetQuestRecordForCharacter(chr.EntityId.Low);
            var recbyslot1 = questRecord.FirstOrDefault(qr => qr.Slot == 1);
            var recbyslot2 = questRecord.FirstOrDefault(qr => qr.Slot == 2);
            var recbyslot3 = questRecord.FirstOrDefault(qr => qr.Slot == 3);
            var recbyslot4 = questRecord.FirstOrDefault(qr => qr.Slot == 4);
            var recbyslot5 = questRecord.FirstOrDefault(qr => qr.Slot == 5);
            var recbyslot6 = questRecord.FirstOrDefault(qr => qr.Slot == 6);
            var recbyslot7 = questRecord.FirstOrDefault(qr => qr.Slot == 7);
            var recbyslot8 = questRecord.FirstOrDefault(qr => qr.Slot == 8);
            var recbyslot9 = questRecord.FirstOrDefault(qr => qr.Slot == 9);
            var recbyslot10 = questRecord.FirstOrDefault(qr => qr.Slot == 10);
            var recbyslot11 = questRecord.FirstOrDefault(qr => qr.Slot == 11);
            var recbyslot12 = questRecord.FirstOrDefault(qr => qr.Slot == 12);
            chr.UnsetQuest1(recbyslot1 == null ? 0 : 0);
            chr.UnsetQuest2(recbyslot2 == null ? 0 : 0);
            chr.UnsetQuest3(recbyslot3 == null ? 0 : 0);
            chr.UnsetQuest4(recbyslot4 == null ? 0 : 0);
            chr.UnsetQuest5(recbyslot5 == null ? 0 : 0);
            chr.UnsetQuest6(recbyslot6 == null ? 0 : 0);
            chr.UnsetQuest7(recbyslot7 == null ? 0 : 0);
            chr.UnsetQuest8(recbyslot8 == null ? 0 : 0);
            chr.UnsetQuest9(recbyslot9 == null ? 0 : 0);
            chr.UnsetQuest10(recbyslot10 == null ? 0 : 0);
            chr.UnsetQuest11(recbyslot11 == null ? 0 : 0);
            chr.UnsetQuest12(recbyslot12 == null ? 0 : 0);
        }
    }
}
