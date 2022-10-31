using WCell.Constants;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Network;
using System.Linq;
using WCell.Util;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Asda2_Items;
using System.IO;
using System.Diagnostics;
using WCell.RealmServer.Items;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WCell.RealmServer.Asda2Quests
{
    class Asda2QuestHandler
    {
        [PacketHandler(RealmServerOpCode.CompleteQuest)] //5253
        public static void CompleteQuestRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            var npcId = packet.ReadInt16();
            var questNum = packet.ReadByte();
            if (client.ActiveCharacter.GodMode)
                client.ActiveCharacter.SendErrorMsg(string.Format("npc = {0}, num = {1}", npcId, questNum));
            var pri = packet.ReadInt16();

            var questId = Asda2QuestRewardNpc.GetQuest(npcId, questNum);
            client.ActiveCharacter.SendErrorMsg("" + questId);
            var reward = Asda2QuestRewardTable.GetRecordByID(questId);
            var questFileId = questId - 2001;
            var chr = client.ActiveCharacter;
            var qr = QuestRecord.GetQuestRecordForCharacter(chr.EntityId.Low);
            var q = qr.FirstOrDefault(qi => qi.QuestTemplateId == questId);
            var Record = Asda2QuestRecord.GetRecordByID(questId);
            Asda2Item item = null;
            if (reward == null)
            {
                client.ActiveCharacter.SendQuestMsg("هذه المهمة لم يتم اضافة جوائزها بعد");
                return;
            }
            else
            {
                try {
                    chr.Money += (uint)reward.Gold;
                    chr.GainXp(reward.Exp, "Quest", true);
                    if (q.CompleteStatus == 1)
                    {
                        switch (questId)
                        {
                            case 2022:
                                if (pri == 0)
                                {
                                    chr.SetClass(1, 1);
                                    chr.Asda2Inventory.TryAdd(36753, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                else if (pri == 256)
                                {
                                    chr.SetClass(1, 2);
                                    chr.Asda2Inventory.TryAdd(36754, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                else if (pri == 512)
                                {
                                    chr.SetClass(1, 3);
                                    chr.Asda2Inventory.TryAdd(36756, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                break;
                            case 2023:
                                if (pri == 0)
                                {
                                    chr.SetClass(1, 4);
                                    chr.Asda2Inventory.TryAdd(36755, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                else if (pri == 256)
                                {
                                    chr.SetClass(1, 5);
                                    chr.Asda2Inventory.TryAdd(36752, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                else if (pri == 512)
                                {
                                    chr.SetClass(1, 6);
                                    chr.Asda2Inventory.TryAdd(36757, 1, true, ref item, Asda2InventoryType.Shop);
                                }
                                break;
                            case 2024:
                                chr.SetClass(1, 7);
                                chr.Asda2Inventory.TryAdd(36751, 1, true, ref item, Asda2InventoryType.Shop);
                                break;

                        }
                    }
                    if (reward.Item1Id != -1)
                    {
                        chr.Asda2Inventory.TryAdd(reward.Item1Id, reward.Item1Amount, true, ref item, (Asda2InventoryType)Asda2ItemMgr.GetTemplate(reward.Item1Id).Template.InventoryType, null);
                    }
                    if (reward.Item2Id != -1)
                    {
                        chr.Asda2Inventory.TryAdd(reward.Item2Id, reward.Item2Amount, true, ref item, (Asda2InventoryType)Asda2ItemMgr.GetTemplate(reward.Item2Id).Template.InventoryType, null);
                    }
                    if (reward.Item3Id != -1)
                    {
                        chr.Asda2Inventory.TryAdd(reward.Item3Id, reward.Item3Amount, true, ref item, (Asda2InventoryType)Asda2ItemMgr.GetTemplate(reward.Item3Id).Template.InventoryType, null);
                    }
                    if (reward.Item4Id != -1)
                    {
                        chr.Asda2Inventory.TryAdd(reward.Item4Id, reward.Item4Amount, true, ref item, (Asda2InventoryType)Asda2ItemMgr.GetTemplate(reward.Item4Id).Template.InventoryType, null);
                    }
                    var cq = new Asda2CompletedQuests(questId, chr.EntityId.Low, 1);
                    switch (q.Slot)
                    {
                        case 1:
                            chr.Quest1 = 0;
                            break;
                        case 2:
                            chr.Quest2 = 0;
                            break;
                        case 3:
                            chr.Quest3 = 0;
                            break;
                        case 4:
                            chr.Quest4 = 0;
                            break;
                        case 5:
                            chr.Quest5 = 0;
                            break;
                        case 6:
                            chr.Quest6 = 0;
                            break;
                        case 7:
                            chr.Quest7 = 0;
                            break;
                        case 8:
                            chr.Quest8 = 0;
                            break;
                        case 9:
                            chr.Quest9 = 0;
                            break;
                        case 10:
                            chr.Quest10 = 0;
                            break;
                        case 11:
                            chr.Quest11 = 0;
                            break;
                        case 12:
                            chr.Quest12 = 0;
                            break;
                    }
                    cq.Save();
                    q.Delete();
                    SendCompleteQuestResponse(client, 0);
                    Handlers.Asda2QuestHandler.SendQuestsListResponse(client);
                }
                catch
                {
                    var cq = new Asda2CompletedQuests(questId, chr.EntityId.Low, 1);
                    cq.Save();
                    q.Delete();
                    SendCompleteQuestResponse(client, 0);
                    Handlers.Asda2QuestHandler.SendQuestsListResponse(client);
                }
            }
        }

        public static void SendCompleteQuestResponse(IRealmClient client, byte status)
        {
            using (var packet = new RealmPacketOut((RealmServerOpCode)5042))
            {
                packet.WriteSkip(stab6);//value name : stab6 default value : stab6Len : 1
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 131548 Len : 4
                packet.WriteByte(0);//value name : unk6 default value : 2Len : 1
                packet.WriteInt16(30);//value name : unk7 default value : 32Len : 2
                packet.WriteInt16(91);//value name : unk8 default value : 0Len : 2
                packet.WriteByte(0);//value name : unk9 default value : 3Len : 1
                client.Send(packet);
            }
        }
        static readonly byte[] stab6 = new byte[] { 0x00 };
        static readonly byte[] stab31 = new byte[] { 0x04, 0x01, 0x00, 0x02, 0xDC, 0x01, 0x01, 0x00, 0xEA, 0x69, 0x29, 0x51, 0x00, 0x00, 0x03, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x02, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x02, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFC, 0x5E, 0x00, 0x00, 0x01, 0x05, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x64, 0x7C, 0x02, 0x94, 0x21, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x54, 0x00, 0x09, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x69, 0x50, 0x00, 0x00, 0x02, 0x0D, 0x00, 0xFF, 0xFF, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x78, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 };
        static readonly byte[] stab501 = new byte[] { 0x04, 0x01, 0x00, 0x02, 0xDC, 0x01, 0x01, 0x00, 0xEA, 0x69, 0x29, 0x51, 0x00, 0x00, 0x03, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        public static void SendUpdateQuestResponse(IRealmClient client, int num)
        {
            using (var packet = new RealmPacketOut((RealmServerOpCode)5052))
            {
                packet.WriteByte(1);//value name : stab6 default value : stab6Len : 1
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 131548 Len : 4
                packet.WriteByte(1);//value name : unk6 default value : 2Len : 1
                packet.WriteInt16(91);//value name : unk7 default value : 32Len : 2
                packet.WriteInt16(91);//value name : unk8 default value : 0Len : 2
                //packet.WriteSkip(stab40);

                client.Send(packet);
            }
        }
        public static byte[] StrToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);
            
            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
        }
        public static void SendUpdateQuestResponse2(IRealmClient client)
        {
            using (var packet = new RealmPacketOut((RealmServerOpCode)5052))
            {
                packet.WriteInt32(2092);//{questId}default value : -1 Len : 4
                packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                packet.WriteInt16(1);//{questSlot}default value : -1 Len : 2
                packet.WriteInt32(21149); // Updated item
                packet.WriteInt16(1); // slot maybe? 
                packet.WriteSkip(StrToByteArray("FFFFFFFF0000000000"));
                packet.WriteInt32(92);
                packet.WriteByte(4);
                packet.WriteInt16(1);
                packet.WriteByte(1);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
                packet.WriteInt16(2092);//{oneMoreQuestId}default value : -1 Len : 2
                packet.WriteInt16(1);//{IsCompleted}default value : 2 Len : 2  0 or 1
                packet.WriteInt16(0);
                packet.WriteInt32(21149);//{questItemId}default value : -1 Len : 4
                packet.WriteInt16(2);//{questItemAmount}default value : 0 Len : 2
                packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                packet.WriteInt32(-1);//{questItemId}default value : -1 Len : 4
                packet.WriteInt16(0);//{questItemAmount}default value : 0 Len : 2
                packet.WriteSkip(StrToByteArray("FFFFFFFF00FFFF00FFFF0200FFFFFFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF00FFFF00FFFF0200FFFFFFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF00000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000000000000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000000000000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000000000000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000000000000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000000000000000000000FFFFFFFF00000000000000FFFFFFFFFFFFFFFF0000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0000000000000000000000000000FFFFFFFF0000FE"));
                packet.WriteByte(4);
                packet.WriteByte(5);
                packet.WriteByte(8);
                packet.WriteByte(7);
                packet.WriteInt32(2092);//{questId}default value : -1 Len : 4
                packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                packet.WriteInt16(1);//{questSlot}default value : -1 Len : 2
                packet.WriteInt32(21149); // Updated item
                packet.WriteInt16(1); // slot maybe? 
                packet.WriteSkip(StrToByteArray("FFFFFFFF0000000000"));
                packet.WriteInt32(92);
                packet.WriteByte(4);
                packet.WriteInt16(1);
                packet.WriteByte(1);//{questStage}default value : 0 Len : 1 2 - in progress 1 - completed
                packet.WriteInt16(2092);//{oneMoreQuestId}default value : -1 Len : 2
               
                client.ActiveCharacter.Send(packet);
                client.ActiveCharacter.SendErrorMsg("Here is : " + packet.TotalLength);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(@"Here : 
 " + packet.ToHexDump());
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static readonly byte[] stub87 = new byte[28];
        [PacketHandler(RealmServerOpCode.AcceptQuest)] //5253
        public static void AcceptQuestRequest(IRealmClient client, RealmPacketIn packet)
        {
            
            packet.Position -= 9;
            var npcId = packet.ReadInt16();
            var questNum = packet.ReadInt16();
            if (client.ActiveCharacter.GodMode)
                client.ActiveCharacter.SendErrorMsg(string.Format("npc = {0}, num = {1}", npcId, questNum));

            var questId = Asda2QuestNpc.GetQuest(npcId, questNum);
            var questFileId = 0;
            if (questId <= 2411)
            {
                questFileId = questId - 2001;
            }
            else if (questId > 2411 && questId <= 2995)
            {
                questFileId = questId - 1999;
            }

            var qr = Asda2QuestRecord.GetRecordByID(questId);
            Asda2CompletedQuests c = null;
            var cq = Asda2CompletedQuests.LoadAllRecordsFor(client.ActiveCharacter.EntityId.Low);
            var cqr = cq.FirstOrDefault(cqi => cqi.SpellId == questId);

            if (qr == null)
            {
                client.ActiveCharacter.SendQuestMsg("هذه المهمة لم يتم اضافتها بعد");
                return;
            }
            if (cq.Contains(cqr))
            {
                client.ActiveCharacter.SendQuestMsg("لقد قمت باكمال هذه المهمة من قبل");
                return;
            }
            QuestRecord Rec = null;
            if (qr.QuestType == 2)
            {
                client.ActiveCharacter.SendQuestMsg("المهام الفرعية لم يتم اضافتها بعد");
                return;
            }
            else
            {
                Rec = new QuestRecord(questId, questFileId, client.ActiveCharacter.EntityId.Low, Asda2QuestMgr.FindFreeSlot(client.ActiveCharacter), qr.Item1Id, qr.Item2Id, qr.Item3Id, qr.Item4Id, qr.Item5Id, qr.Item1Amount, qr.Item2Amount, qr.Item3Amount, qr.Item4Amount, qr.Item5Amount, qr.Monster1Id, qr.Monster2Id, qr.Monster3Id, qr.Monster4Id, qr.Monster5Id, 0, 0);
            }
            var qrf = client.ActiveCharacter.QuestRecords.FirstOrDefault(qt => qt.QuestTemplateId == questId);

            //if (client.ActiveCharacter.Level < qr.Level)
            //{
            //    client.ActiveCharacter.SendQuestMsg("مستوى منخفض");
            //    return;
            //}
            if (client.ActiveCharacter.CheckQuest(questId))
            {
                client.ActiveCharacter.SendQuestMsg("لقد قمت باستلام هذه المهمة بالفعل او لم تكملها..");
                SendAcceptQuestResponse(client, 0, questFileId, questId);
                return;
            }
            else
            {
                Rec.Create();
                var chr = client.ActiveCharacter;
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
                SendAcceptQuestResponse(client, 0, questFileId, questId);
                //Handlers.Asda2QuestHandler.SendQuestsListResponseTest(client, questId, questFileId);
                Handlers.Asda2QuestHandler.SendQuestsListResponse(client);

            }
        }
        public static void SendAcceptQuestResponse(IRealmClient client, byte status, int questidbyfilename, int questid)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.AcceptQuestRes))
            {
                packet.WriteByte(status); // status
                packet.WriteInt16(questidbyfilename); // questIdByFileName
                packet.WriteInt32(questid); // questIdByFileName
                packet.WriteInt32(questidbyfilename); // questIdByFileName
                packet.WriteInt32(questidbyfilename);
                client.Send(packet);
            }
        }

    }
}
