using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Items;
using WCell.RealmServer.Network;
using WCell.Util.Variables;

namespace WCell.RealmServer.Asda2Quests
{
    public static class Asda2QuestMgr
    {
        public static int FindFreeSlot(Character chr)
        {
            var questRecord = Database.QuestRecord.GetQuestRecordForCharacter(chr.EntityId.Low);
            for (var i = 0; i < questRecord.Length; i++)
            {
                if (chr.QuestRecords[i] == null)
                {
                    return i++;
                }
            }
            return (questRecord.Length + 1);
        }

        public static void UpdateQuest(IRealmClient client, QuestRecord quest, int itemIndex)
        {
            Asda2Item asda2Item = null;
            var questInfo = Asda2QuestRecord.GetRecordByID(quest.QuestTemplateId);
            switch (itemIndex)
            {
                case 1:
                    //client.ActiveCharacter.Asda2Inventory.TryAdd(quest.Item1Id, 1, true, ref asda2Item, Asda2_Items.Asda2InventoryType.Quests);
                    quest.Item1Amount += 1;
                    //asda2Item.Amount = quest.Item1Amount;
                    //asda2Item.Save();
                    quest.Update();
                    quest.Save();
                    //client.ActiveCharacter.SendQuestMsg(string.Format("تم تحديث المهمة < {0} < {1} - {2} / {3}", questInfo.QuestName, Asda2ItemMgr.GetTemplate(quest.Item1Id).Name, quest.Item1Amount, questInfo.Item1ReqAmount));
                    break;
                case 2:
                    //client.ActiveCharacter.Asda2Inventory.TryAdd(quest.Item2Id, 1, true, ref asda2Item, Asda2_Items.Asda2InventoryType.Quests);
                    quest.Item2Amount += 1;
                    quest.Update();
                    quest.Save();
                    //client.ActiveCharacter.SendQuestMsg(string.Format("تم تحديث المهمة < {0} < {1} - {2} / {3}", questInfo.QuestName, Asda2ItemMgr.GetTemplate(quest.Item2Id).Name, quest.Item2Amount, questInfo.Item2ReqAmount));
                    break;
                case 3:
                    //client.ActiveCharacter.Asda2Inventory.TryAdd(quest.Item3Id, 1, true, ref asda2Item, Asda2_Items.Asda2InventoryType.Quests);
                    quest.Item3Amount += 1;
                    quest.Update();
                    quest.Save();
                    //client.ActiveCharacter.SendQuestMsg(string.Format("تم تحديث المهمة < {0} < {1} - {2} / {3}", questInfo.QuestName, Asda2ItemMgr.GetTemplate(quest.Item3Id).Name, quest.Item3Amount, questInfo.Item3ReqAmount));
                    break;
                case 4:
                    //client.ActiveCharacter.Asda2Inventory.TryAdd(quest.Item4Id, 1, true, ref asda2Item, Asda2_Items.Asda2InventoryType.Quests);
                    quest.Item4Amount += 1;
                    quest.Update();
                    quest.Save();
                    //client.ActiveCharacter.SendQuestMsg(string.Format("تم تحديث المهمة < {0} < {1} - {2} / {3}", questInfo.QuestName, Asda2ItemMgr.GetTemplate(quest.Item4Id).Name, quest.Item4Amount, questInfo.Item4ReqAmount));
                    break;
                case 5:
                    //client.ActiveCharacter.Asda2Inventory.TryAdd(quest.Item5Id, 1, true, ref asda2Item, Asda2_Items.Asda2InventoryType.Quests);
                    quest.Item5Amount += 1;
                    quest.Update();
                    quest.Save();
                    //client.ActiveCharacter.SendQuestMsg(string.Format("تم تحديث المهمة < {0} < {1} - {2} / {3}", questInfo.QuestName, Asda2ItemMgr.GetTemplate(quest.Item5Id).Name, quest.Item5Amount, questInfo.Item5ReqAmount));
                    break;
            }
            if (quest.Item1Amount >= questInfo.Item1ReqAmount && quest.Item2Amount >= questInfo.Item2ReqAmount && quest.Item3Amount >= questInfo.Item3ReqAmount && quest.Item4Amount >= questInfo.Item4ReqAmount && quest.Item5Amount >= questInfo.Item5ReqAmount)
            {
                quest.QuestStage = 1;
                quest.CompleteStatus = 1;
                quest.Update();
            }
            Asda2QuestHandler.SendUpdateQuestResponse(client, 1);
            //Asda2QuestHandler.SendUpdateQuestResponse2(client);
            Handlers.Asda2QuestHandler.SendQuestsListResponse(client);
        }
    }
}
