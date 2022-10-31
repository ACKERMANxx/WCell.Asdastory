using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants;
using WCell.Core;
using WCell.Core.Network;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Logs;
using WCell.RealmServer.Network;

namespace WCell.RealmServer.Asda2Mail
{
    public static class Asda2MailMgr
    {

    }
    public static class Asda2MailHandler
    {
        [PacketHandler(RealmServerOpCode.OpenMailBox)]//6618
        public static void OpenMailBoxRequest(IRealmClient client, RealmPacketIn packet)
        {
            var pages = (client.ActiveCharacter.MailMessages.Count+9)/10;
            for (int i = 0; i <pages ; i++)
            {
                var msgs = client.ActiveCharacter.MailMessages.Values.Skip(i * 10).Take(10).ToList();
                SendMailsListResponse(client);
            }
            
        }
        public static void SendYouHaveNewMailResponse(IRealmClient client,int amount)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.YouHaveNewMail))//6628
            {
                packet.WriteInt32(25);//{mailAmount}default value : 1 Len : 4
                client.Send(packet,addEnd: true);
            }
        }

        public static void SendMailsListResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MailsList))//6619
            {
                packet.WriteByte(1);//{page}default value : 1 Len : 1
                packet.WriteByte(1);//{guidRec}default value : 379808 Len : 4
                packet.WriteByte(1);//{taked}default value : 0 Len : 1
                packet.WriteByte(1);//{timeTodeletemins}default value : 10061 Len : 4
                packet.WriteByte(1);//{senderName}default value :  Len : 20
                packet.WriteByte(1);//{title}default value :  Len : 32
                packet.WriteInt32(1);//{page}default value : 1 Len : 1
                packet.WriteInt32(8);//{guidRec}default value : 379808 Len : 4
                packet.WriteInt32(9);//{taked}default value : 0 Len : 1
                packet.WriteInt32(10);//{timeTodeletemins}default value : 10061 Len : 4
                packet.WriteInt32(11);//{senderName}default value :  Len : 20
                packet.WriteInt32(12);//{title}default value :  Len : 32
                client.Send(packet,addEnd: true);
            }
        }
        [PacketHandler(RealmServerOpCode.SendMailMessage)]//6620
        public static void SendMailMessageRequest(IRealmClient client, RealmPacketIn packet)
        {
            var sendedItemId = packet.ReadInt32();//default : 37823Len : 4
            var slot = packet.ReadInt16();//default : 23Len : 2
            var inv = (Asda2InventoryType)packet.ReadByte();//default : 2Len : 1
            var amount = packet.ReadUInt16();//default : 0Len : 2
            var sendedGold = packet.ReadUInt32();//default : 555Len : 4
            var targetName = packet.ReadAsdaString(20,Locale.En);//default : Len : 18
            var subject = packet.ReadAsdaString(32,Locale.En);//default : Len : 32
            var message = packet.ReadAsdaString(126,Locale.En);//default : Len : 126
            var prueEng = Asda2EncodingHelper.IsPrueEnglish(message);
            if (!prueEng)
            {
                client.ActiveCharacter.SendOnlyEnglishCharactersAllowed("message");
                SendMailMessageSendedResponse(client, MailMessageSendedStatus.WrongInformation);
                return;
            }
            prueEng = Asda2EncodingHelper.IsPrueEnglish(subject);
            if (!prueEng)
            {
                client.ActiveCharacter.SendOnlyEnglishCharactersAllowed("subject");
                SendMailMessageSendedResponse(client, MailMessageSendedStatus.WrongInformation);
                return;
            }
            var item = inv == Asda2InventoryType.Shop
                           ? client.ActiveCharacter.Asda2Inventory.GetShopShopItem(slot)
                           : client.ActiveCharacter.Asda2Inventory.GetRegularItem(slot);
            if(client.ActiveCharacter.Money<1001 || client.ActiveCharacter.Money-1000<sendedGold)
            {
                SendMailMessageSendedResponse(client, MailMessageSendedStatus.IncifitientGold);
                return;
            }
            var chr = World.GetCharacter(targetName, false);
            var chrRec = chr == null ? CharacterRecord.GetRecordByName(targetName) : chr.Record;
            if(chrRec == null)
            {
                SendMailMessageSendedResponse(client, MailMessageSendedStatus.RecipentNotFounded);
                return;
            }
            Asda2Item sendItem = null;
            if (item != null)
            {
                if(item.IsSoulbound)
                {
                    //todo asda2 ItemMove log MailMessage
                    SendMailMessageSendedResponse(client, MailMessageSendedStatus.YouCantSendThisItem);
                    return;
                }
                if (item.Amount < amount)
                {
                    client.ActiveCharacter.YouAreFuckingCheater("Tying to mail wrong item amount", 50);
                    SendMailMessageSendedResponse(client, MailMessageSendedStatus.WrongInformation);
                    return;
                }
                item.ModAmount(-(amount==0?1:amount));
                sendItem = Asda2Item.CreateItem(item.Template, null, amount == 0 ? 1 : amount);
                sendItem.Record.SaveLater();
                
            }
            client.ActiveCharacter.SubtractMoney(sendedGold + 1000);
         var   resLog = Log.Create(Log.Types.ItemOperations, LogSourceType.Character, client.ActiveCharacter.EntryId)
                                                         .AddAttribute("source", 0, "send_mail")
                                                         .AddItemAttributes(sendItem, "sent")
                                                         .AddItemAttributes(item, "source")
                                                         .AddAttribute("gold",sendedGold)
                                                         .AddAttribute("receiver", chrRec.EntityLowId, targetName)
                                                         .Write();
           RealmServer.IOQueue.AddMessage(() =>
           {
               var newMessage = new Asda2MailMessage(subject, message, sendItem, sendedGold, chrRec.EntityLowId, client.ActiveCharacter.Name);
               newMessage.CreateLater();
               if (chr != null)
               {
                   chr.MailMessages.Add(newMessage.Guid, newMessage);
                   chr.SendMailMsg(string.Format("You recieve new message from {0}. Subject {1}.", client.ActiveCharacter.Name, newMessage.Subject));
                   SendYouHaveNewMailResponse(chr.Client, 1);
               }
           });
           SendMailMessageSendedResponse(client, MailMessageSendedStatus.Ok, item);
            //client.ActiveCharacter.SubtractMoney(sendedGold + 1000);
        }
        public static void SendMailMessageSendedResponse(IRealmClient client,MailMessageSendedStatus status,Asda2Item item=null)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MailMessageSended))//6621
            {
                packet.WriteInt32(13);//{status}default value : 1 Len : 1
                packet.WriteInt32(14);//{weight0}default value : 10565 Len : 2
                packet.WriteInt32(15);//{money}default value : 5539958 Len : 4
                client.Send(packet,addEnd: true);
            }
        }
        [PacketHandler(RealmServerOpCode.ShowMailMsg)]//6622
        public static void ShowMailMsgRequest(IRealmClient client, RealmPacketIn packet)
        {
            var messageGuid = packet.ReadInt32();//default : 379808Len : 4
            if(!client.ActiveCharacter.MailMessages.ContainsKey(messageGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Try to view not existing mail message.",30);
                SendMailMessageInfoResponse(client, ShowMailMessageStatus.Fail, null);
                return;
            }
            var msg = client.ActiveCharacter.MailMessages[messageGuid];
            if (msg != null)
                msg.IsReaded = true;
            SendMailMessageInfoResponse(client,ShowMailMessageStatus.Ok, msg);
        }

        public static void SendMailMessageInfoResponse(IRealmClient client,ShowMailMessageStatus status,Asda2MailMessage msg)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MailMessageInfo))//6623
            {
                packet.WriteInt32(16);//{status}default value : 1 Len : 1
                packet.WriteInt32(17);//{msgGuid}default value : 379808 Len : 4
                packet.WriteInt32(18);//{goldAmount}default value : 5600 Len : 4        
                packet.WriteInt32(19);//{message}default value :  Len : 128
                client.Send(packet,addEnd: true);
            }
        }

        [PacketHandler(RealmServerOpCode.TakeMailItem)]//6624
        public static void TakeMailItemRequest(IRealmClient client, RealmPacketIn packet)
        {
            var mailGuid = packet.ReadInt32();//default : 379816Len : 4
          
                    if (!client.ActiveCharacter.MailMessages.ContainsKey(mailGuid))
                    {
                        client.ActiveCharacter.YouAreFuckingCheater("Try to take not existing mail message.", 50);
                        SendMailItemTakedResponse(client, Asda2MailItemTakedStatus.WrongInfo, null);
                        return;
                    }
                    var msg = client.ActiveCharacter.MailMessages[mailGuid];
                    if (msg == null)
                    {
                        SendMailItemTakedResponse(client, Asda2MailItemTakedStatus.WrongInfo, null);
                        return;
                    }
                    var item = msg.Item;
                    Asda2Item resultItem = null;
                    if (item != null)
                    {
                        client.ActiveCharacter.Asda2Inventory.TryAdd(item.ItemId,item.Amount, true, ref resultItem,null,item);
                        Log.Create(Log.Types.ItemOperations, LogSourceType.Character, client.ActiveCharacter.EntryId)
                           .AddAttribute("source", 0, "taked_from_mail")
                           .AddAttribute("message_id",msg.Guid)
                           .AddItemAttributes(resultItem)
                           .Write();
                        msg.ItemGuid = 0;
                        msg.Item = null;
                        item.Destroy();
                    }
                    client.ActiveCharacter.AddMoney(msg.Gold);
                    msg.Gold = 0;
                    msg.UpdateLater();
                    SendMailItemTakedResponse(client, Asda2MailItemTakedStatus.Ok, resultItem);
                    client.ActiveCharacter.SendMoneyUpdate();
                
        }

        public static void SendMailItemTakedResponse(IRealmClient client,Asda2MailItemTakedStatus status,Asda2Item item)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MailItemTaked))//6625
            {
                packet.WriteInt32(20);//{status}default value : 1 Len : 1
                packet.WriteInt16(21);//{weight}default value : 10565 Len : 2
                packet.WriteInt32(22);//{money}default value : 5538958 Len : 4
                client.Send(packet,addEnd: true);
            }
        }

        [PacketHandler(RealmServerOpCode.DeleteMailMessage)]//6626
        public static void DeleteMailMessageRequest(IRealmClient client, RealmPacketIn packet)
        {
            var messageId = packet.ReadInt32();//default : 379808Len : 4
            if(client.ActiveCharacter.MailMessages.ContainsKey(messageId))
            {
                var msg = client.ActiveCharacter.MailMessages[messageId];
                if (msg != null)
                {
                    client.ActiveCharacter.MailMessages.Remove(messageId);
                    msg.DeleteLater();
                }
            }
            SendMailMessageDeletedResponse(client, messageId);
        }
        public static void SendMailMessageDeletedResponse(IRealmClient client,int msgGuid)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MailMessageDeleted))//6627
            {
                packet.WriteInt32(23);//{status}default value : 1 Len : 1
                packet.WriteInt32(24);//{msgGuid}default value : 379808 Len : 4
                client.Send(packet);
            }
        }


    }
    public enum MailMessageSendedStatus
    {
        WrongInformation =0,
        Ok =1,
        RecipentNotFounded =2,
        IncifitientQuantity =3,
        YouCantSendThisItem =4,
        IncifitientGold =5,
        TheUserMailBosIsFull =6,
        YouNeed10LevelToSendMail =7
    }
    public enum ShowMailMessageStatus
    {
        Fail =0,
        Ok =1,
    }
    public enum Asda2MailItemTakedStatus
    {
        WrongInfo=0,
        Ok=1,
        WeightExideds =3,

    }
}
