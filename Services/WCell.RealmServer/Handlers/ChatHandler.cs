/*************************************************************************
 *
 *   file		    : ChatHandler.cs
 *   copyright		: (C) The WCell Team
 *   email		    : info@wcell.org
 *   last changed	: $LastChangedDate: 2008-02-19 07:46:37 -0800 (Tue, 19 Feb 2008) $
 *   last author	: $LastChangedBy: anonemous $
 *   revision		: $Rev: 148 $
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using WCell.Constants;
using WCell.Constants.Misc;
using WCell.Core;
using WCell.Core.Network;
using WCell.RealmServer.Asda2Titles;
using WCell.RealmServer.Commands;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Events.Asda2;
using WCell.RealmServer.Global;
using WCell.RealmServer.Groups;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Network;
using WCell.Util;
using WCell.Util.Graphics;

namespace WCell.RealmServer.Chat
{
    public static partial class ChatMgr
    {
        #region Asda2
        #region GMChat
        internal static void SendGlobalMessageGM(string message)
        {
            using (RealmPacketOut realmPacketOut = new RealmPacketOut(RealmServerOpCode.MessageServerAboutWarStarts))
            {
                realmPacketOut.WriteInt32(1);
                realmPacketOut.WriteFixedAsciiString(message, 100, Locale.Start);
                realmPacketOut.WriteByte(255); // red
                realmPacketOut.WriteByte(255); // green 
                realmPacketOut.WriteByte(102); // blue
                World.Broadcast(realmPacketOut, false, Locale.Any);
            }
        }
        internal static void SendGlobalMessageWhite(string message)
        {
            using (RealmPacketOut realmPacketOut = new RealmPacketOut(RealmServerOpCode.MessageServerAboutWarStarts))
            {
                realmPacketOut.WriteInt32(1);
                realmPacketOut.WriteFixedAsciiString(message, 100, Locale.Start);
                realmPacketOut.WriteByte(255); // red
                realmPacketOut.WriteByte(255); // green 
                realmPacketOut.WriteByte(255); // blue
                World.Broadcast(realmPacketOut, false, Locale.Any);
            }
        }

        public static RealmPacketOut SendTalkMessage(string msg, Color c)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MessageServerAboutWarStarts))//3113
            {
                packet.WriteInt16(0);//value name : unk4 default value : 0Len : 2
                packet.WriteInt16(0);//value name : unk5 default value : 100Len : 2
                packet.WriteFixedAsciiString(msg, 36);//{Message}default value : 48 Len : 36           
                packet.WriteSkip(unk80);//value name : unk80 default value : unk80Len : 107
                World.Broadcast(packet, true, Locale.Any);
                return packet;
            }
        }
        static readonly byte[] unk80 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00 };

        #endregion

        #region Normal
        [PacketHandler(RealmServerOpCode.NormalChat)] //5084
        public static void NormalChatRequest(IRealmClient client, RealmPacketIn packet)
        {
            
            packet.Position -= 33;
            var msg = packet.ReadAsciiString(client.Locale); //default : 
            if (msg.Length < 1 ||
                RealmCommandHandler.HandleCommand(client.ActiveCharacter, msg,
                                                  client.ActiveCharacter.Target as Character))
                return;
            if (client.ActiveCharacter.ChatBanned)
            {
                client.ActiveCharacter.SendInfoMsg("Your chat is banned.");
                return;
            }
                client.ActiveCharacter.SendPacketToArea(
                    CreateNormalChatMessagePacket(client.ActiveCharacter.Name, msg, client.Locale, client.ActiveCharacter), true, false, Locale.Any);
        }

        static readonly byte[] stab44 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };


        public static void SendSystemChatResponse(IRealmClient client, string msg)
        {
            client.Send(CreateNormalChatMessagePacket("System", msg, Locale.En,null), addEnd: false);
        }

        public static RealmPacketOut CreateNormalChatMessagePacket(string sender, string message, Locale locale,
      Character chr = null)
        {
            RealmPacketOut realmPacketOut = new RealmPacketOut(RealmServerOpCode.NormalChatResponse);
            realmPacketOut.WriteInt16(chr.SessionId);
            realmPacketOut.WriteInt32(chr.AccId);
            realmPacketOut.Position = 11;
            realmPacketOut.WriteFixedAsciiString(sender, 20, locale);
            realmPacketOut.WriteInt32(chr.Asda2Rank);
            realmPacketOut.WriteAsciiString(message, locale);
            return realmPacketOut;
        }
        #endregion

        #region Announce
        [PacketHandler(RealmServerOpCode.AnnChat)]
        public static void AnnChatRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 6;
            string str = packet.ReadAsciiString(client.Locale);
            if (client.ActiveCharacter.Level < 5 || str.Length > 200)
            {
                client.ActiveCharacter.YouAreFuckingCheater("your level is less than 5.", 50);
                client.ActiveCharacter.SendErrorMsg("your level is less than 5. [you got 50 Pan Points for that]");
            }
            else
            {
                if (str.Length < 1 || RealmCommandHandler.HandleCommand(client.ActiveCharacter, str,
                     client.ActiveCharacter.Target as Character))
                    return;
                Locale locale = Asda2EncodingHelper.MinimumAvailableLocale(client.Locale, str);
                client.ActiveCharacter.SendPacketToArea(
                    SendAnnChatResponse(client, client.ActiveCharacter.Name, str,
                  locale), true, false, Locale.Any, 999f);
            }
        }

        public static RealmPacketOut SendAnnChatResponse(IRealmClient client, string sender, string str, Locale locale)
        {
            RealmPacketOut packet = new RealmPacketOut(RealmServerOpCode.AnnChatResponse);
            
            packet.WriteInt16(client.ActiveCharacter.SessionId);
            packet.WriteInt32(client.ActiveCharacter.AccId);
            packet.WriteInt16(1);
            packet.WriteFixedAsciiString(sender, 20, Locale.Any);
            packet.WriteAsciiString(str, Locale.Any);
            return packet;
        }
        #endregion

        #region Wishper

        [PacketHandler(RealmServerOpCode.WishperChatRequest)] //5088
        public static void WishperChatRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 33;
            var isSoulmate = packet.ReadByte(); //default : 0Len : 1
            var target = packet.ReadAsdaString(20, Locale.En); //default : Len : 20
            client.ActiveCharacter.SendErrorMsg(string.Format("Target = {0}", target));
            var msg = packet.ReadAsciiString(client.Locale); //default : Len : 0
            if (msg.Length > 100)
            {
                client.ActiveCharacter.SendSystemMessage(string.Format(
                    "Can't send wishper to {0} cause it's length more than 100 symbols.", target));
                return;
            }
            if (msg.Length < 1 ||
                RealmCommandHandler.HandleCommand(client.ActiveCharacter, msg,
                                                  client.ActiveCharacter.Target as Character))
            {
                return;
            }
            var targetChar = World.GetCharacter(target, false);
            if (targetChar == null)
            {
                CharacterRefuseWishper(isSoulmate, (int)client.ActiveCharacter.AccId,
                                    client.ActiveCharacter.SessionId, client.ActiveCharacter.Name, msg, client, WishperStatus.NotFound, target);
                return;
            }
            if (!targetChar.EnableWishpers)
            {
                CharacterRefuseWishper(isSoulmate, (int)client.ActiveCharacter.AccId,
                                    targetChar.SessionId, client.ActiveCharacter.Name, msg, client, WishperStatus.DontAcceptWishper);
                return;
            }

            if ((targetChar.IsInBattleground || client.ActiveCharacter.IsInBattleground) && (targetChar.Asda2FactionId != client.ActiveCharacter.Asda2FactionId))
            {
                CharacterRefuseWishper(isSoulmate, (int)client.ActiveCharacter.AccId,
                                    client.ActiveCharacter.SessionId, client.ActiveCharacter.Name, msg, client, WishperStatus.AnotherFaction, target);
                return;
            }
            if (client.ActiveCharacter.ChatBanned)
            {
                client.ActiveCharacter.SendInfoMsg("Your chat is banned.");
                return;
            }

            // TO
                SendWishperChatResponse(targetChar.Client, isSoulmate, (int)client.ActiveCharacter.AccId,
                        client.ActiveCharacter.SessionId, targetChar.Name, msg, client);

            // Recever
            SendWishperChatResponse(targetChar.Client, isSoulmate, (int)client.ActiveCharacter.AccId,
        targetChar.SessionId, client.ActiveCharacter.Name, msg, client);

            /*SendWishperChatResponse(targetChar.Client, isSoulmate, (int)client.ActiveCharacter.AccId,
                                    targetChar.SessionId, client.ActiveCharacter.Name, msg, client);*/
            client.ActiveCharacter.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)44, 1000, 3000);
        }


        public static void SendWishperChatResponse(IRealmClient recieverClient, byte soulmate, int senderAccId,
                                                   short rcvSessId, string sender, string msg,
                                                   IRealmClient senderClient = null)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.WishperChat)) //5089
            {
                packet.WriteByte(soulmate); //{soulmate}default value : 0 Len : 1
                packet.WriteByte(1); //value name : unk0 default value : 1Len : 1
                packet.WriteInt32(senderAccId); //{senderAccId}default value : 340701 Len : 4
                packet.WriteInt16(rcvSessId); //{rcvSessId}default value : 37 Len : 2
                packet.WriteFixedAsciiString(sender, 21);
                //{sender}default value :  Len : 21
                packet.WriteAsciiString(msg); //{msg}default value :  Len : 0
                if (senderClient != null)
                    senderClient.Send(packet, addEnd: true);
                recieverClient.Send(packet, addEnd: true);
            }
        }
        public static void CharacterRefuseWishper(byte soulmate, int senderAccId,
                                                   short rcvSessId, string sender, string msg,
                                                   IRealmClient senderClient, WishperStatus status, string target = "")
        {
            byte[] unk = new byte[] { 0xB4, 0xD4, 0xB2, 0xB2, 0xBC, 0xAD, 0x20, 0xB1, 0xD3, 0xB8, 0xBB, 0xC3, 0xBC, 0xC6, 0xC3, 0xC0, 0xBB, 0x20, 0xB0, 0xC5, 0xBA, 0xCE, 0xC7, 0xCF, 0xBC, 0xCC, 0xBD, 0xC0, 0xB4, 0xCF, 0xB4, 0xD9, 0x2E };

            using (var packet = new RealmPacketOut(RealmServerOpCode.WishperChat)) //5089
            {
                packet.WriteByte(soulmate); //{soulmate}default value : 0 Len : 1
                packet.WriteByte((int)status); //value name : unk0 default value : 1Len : 1
                packet.WriteInt32(senderAccId); //{senderAccId}default value : 340701 Len : 4
                packet.WriteInt16(rcvSessId); //{rcvSessId}default value : 37 Len : 2
                if (status == WishperStatus.DontAcceptWishper)
                {
                    packet.WriteFixedAsciiString(sender, 21);
                    packet.WriteInt16(11822);
                    packet.WriteByte(0);
                }
                if (status == WishperStatus.NotFound)
                {
                    packet.WriteFixedAsciiString(target, 21);
                    packet.WriteInt16(11822);
                    packet.WriteByte(0);

                    //packet.Write(unk);
                }
                //{sender}default value :  Len : 21
                if (senderClient != null)
                    senderClient.Send(packet, addEnd: false);
            }
        }
        #endregion

        #region group

        [PacketHandler(RealmServerOpCode.PartyChat)] //5105
        public static void PartyChatRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 31; //nk default : 40Len : 2
            var msg = packet.ReadAsciiString(client.Locale); //default : Len : 0
            if (msg.Length < 1 ||
                RealmCommandHandler.HandleCommand(client.ActiveCharacter, msg,
                                                  client.ActiveCharacter.Target as Character))
                return;
            if (!client.ActiveCharacter.IsInGroup)
                return;
            SendPartyChatResponse(client.ActiveCharacter, msg);
        }

        public static void SendPartyChatResponse(Character sender, string msg)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PartyChatResponse)) //5106
            {
                packet.WriteInt16(sender.SessionId); //{sessId}default value : 37 Len : 2
                packet.WriteFixedAsciiString(sender.Name, 20); //{sender}default value :  Len : 20
                packet.WriteInt16(sender.SessionId); //{sessId}default value : 37 Len : 2
                packet.WriteByte(0);
                packet.WriteAsciiString(msg, sender.Client.Locale); //{msg}default value :  Len : 0

                sender.Group.Send(packet, addEnd: false);
            }
        }


        #endregion

        #region global chating

        [PacketHandler(RealmServerOpCode.GlobalChatWithItem)] //6560
        public static void GlobalChatWithItemRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 33;
            var success = client.ActiveCharacter.Asda2Inventory.UseGlobalChatItem();
            if (!success)
                return;
            //var accId = packet.ReadInt32();//default : 0Len : 4
            packet.Position += 1; //nk1 default : 0Len : 1
            var msg = packet.ReadAsciiString(client.Locale); //default : Len : 0
            if (msg.Length < 1 ||
                RealmCommandHandler.HandleCommand(client.ActiveCharacter, msg,
                                                  client.ActiveCharacter.Target as Character))
                return;
            if (msg.Length > 200)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Global chat message more than 200 symbols.", 80);
                return;
            }
            if (client.ActiveCharacter.ChatBanned)
            {
                client.ActiveCharacter.SendInfoMsg("Your chat is banned.");
                return;
            }
            if (Asda2EventMgr.IsGuessWordEventStarted)
            {
                Asda2EventMgr.TryGuess(msg, client.ActiveCharacter);
            }
            var locale = Asda2EncodingHelper.MinimumAvailableLocale(client.Locale, msg);
            SendGlobalChatWithItemResponseResponse(client.ActiveCharacter.Name, msg, client.ActiveCharacter.ChatColor, locale);
        }

        public static void SendGlobalChatRemoveItemResponse(IRealmClient client, bool success, Asda2Item globalChatItem)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.GlobalChatRemoveItem)) //6610
            {
                packet.WriteByte(success ? 1 : 0); //{status}default value : 1 Len : 1
                packet.WriteInt16(client.ActiveCharacter.Asda2Inventory.Weight); //{intvWeight}default value : 0 Len : 2
                packet.WriteInt32(globalChatItem == null ? 0 : globalChatItem.ItemId);
                //{itemId}default value : 20642 Len : 4
                packet.WriteByte((byte)(globalChatItem == null ? 0 : globalChatItem.InventoryType));
                //{invNum}default value : 2 Len : 1
                packet.WriteInt16(globalChatItem == null ? 0 : globalChatItem.Slot); //{slot}default value : 5 Len : 2
                packet.WriteInt16(0); //value name : unk10 default value : 0Len : 2
                packet.WriteInt32(globalChatItem == null ? 0 : globalChatItem.Amount);
                //{quantity}default value : 4 Len : 4
                packet.WriteByte(0); //{durability}default value : 0 Len : 1
                packet.WriteInt16(globalChatItem == null ? 0 : globalChatItem.Weight);
                //{weight0}default value : 84 Len : 2
                packet.WriteInt16(-1); //{soul1Id}default value : -1 Len : 2
                packet.WriteInt16(-1); //{soul2Id}default value : -1 Len : 2
                packet.WriteInt16(-1); //{soul3Id}default value : -1 Len : 2
                packet.WriteInt16(-1); //{soul4Id}default value : -1 Len : 2
                packet.WriteInt16(0); //{enchant}default value : 0 Len : 2
                packet.WriteInt16(0); //value name : unk21 default value : 0Len : 2
                packet.WriteByte(0); //value name : unk22 default value : 0Len : 1
                packet.WriteInt16(-1); //{parametr1Type}default value : -1 Len : 2
                packet.WriteInt16(0); //{paramtetr1Value}default value : 0 Len : 2
                packet.WriteInt16(-1); //{parametr2Type}default value : -1 Len : 2
                packet.WriteInt16(0); //{paramtetr2Value}default value : 0 Len : 2
                packet.WriteInt16(-1); //{parametr3Type}default value : -1 Len : 2
                packet.WriteInt16(0); //{paramtetr3Value}default value : 0 Len : 2
                packet.WriteInt16(-1); //{parametr4Type}default value : -1 Len : 2
                packet.WriteInt16(-1); //{paramtetr4Value}default value : -1 Len : 2
                packet.WriteInt16(-1); //{parametr5Type}default value : -1 Len : 2
                packet.WriteInt16(0); //{paramtetr5Value}default value : 0 Len : 2
                packet.WriteByte(0); //value name : unk33 default value : 0Len : 1
                packet.WriteByte(0); //{isDressed}default value : 0 Len : 1
                client.Send(packet, addEnd: false);
            }
        }


        public static void SendGlobalChatWithItemResponseResponse(string sender, string mesage, Color color, Locale locale)
        {
            World.Broadcast(sender, mesage, color, locale);
        }

        public static RealmPacketOut CreateGlobalChatMessage(string sender, string message, Color color, Locale locale)
        {
            var packet = new RealmPacketOut(RealmServerOpCode.GlobalChatWithItemResponse); //6561

            packet.WriteInt32(1); //{mustBeOne}default value : 1 Len : 4
            /*packet.WriteByte(blue);//{blue}default value : 0 Len : 1
            packet.WriteByte(green);//{green}default value : 0 Len : 1
            packet.WriteByte(red);//{red}default value : 255 Len : 1
            packet.WriteByte(alfa);//{alfa}default value : 255 Len : 1*/
            packet.WriteInt32(color.ARGBValue);
            packet.WriteInt32(0); //value name : unk4 default value : 0Len : 4
            packet.WriteFixedAsciiString(sender, 20, locale); //{sender}default value : 
            packet.WriteAsciiString(message, locale); //{msg}default value : 
            return packet;
        }

        #endregion

        #region Messeging

        public static void SendGlobalMessageResponse(string name, Asda2GlobalMessageType type, int itemId = 0,
                                                     short upgradeValue = 0, short mobId = 0)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.GlobalMessage)) //6615
            {
                packet.WriteByte((byte)type); //{type}default value : 3 Len : 1
                packet.WriteInt32(itemId); //{itemId}default value : 0 Len : 4
                packet.WriteInt16(upgradeValue); //{upgradeValue}default value : 0 Len : 2
                packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                packet.WriteInt32(mobId); //{mobId}default value : 0 Len : 4
                packet.WriteFixedAsciiString(name, 20); //{name}default value :  Len : 20
                World.Broadcast(packet, true, Locale.Any);
            }
        }

        public enum Asda2GlobalMessageType
        {
            HasObinedItem = 0,
            HasUpgradeItem = 1,
            Unknown2 = 2,
            HasDefeated = 3,
        }

        #endregion

        #region ChatRoom

        [PacketHandler(RealmServerOpCode.CreateChatRoom)] //6240
        public static void CreateChatRoomRequest(IRealmClient client, RealmPacketIn packet)
        {
            var isPrivate = packet.ReadByte() == 1; //tab35 default : stab35Len : 1
            var maxMemberCount = packet.ReadByte(); //default : 15Len : 1
            var roomName = packet.ReadAsdaString(28, Locale.En); //default : Len : 28
            packet.Position += 2; //nk2 default : 0Len : 2
            var password = packet.ReadAsdaString(8, Locale.En); //default : Len : 8
            if (client.ActiveCharacter.ChatRoom != null)
            {
                SendChatRoomCreatedResponse(client, CreateChatRoomStatus.YouAreAlreadyInChatRoom, client.ActiveCharacter.ChatRoom);
                return;
            }
            if (client.ActiveCharacter.IsAsda2BattlegroundInProgress)
            {
                SendChatRoomCreatedResponse(client, CreateChatRoomStatus.UnableOpenOnBattle, client.ActiveCharacter.ChatRoom);
                return;
            }
            if (isPrivate && string.IsNullOrWhiteSpace(password))
            {
                SendChatRoomCreatedResponse(client, CreateChatRoomStatus.SetPassword, client.ActiveCharacter.ChatRoom);
                return;
            }
            var isPrueEnglish = Asda2EncodingHelper.IsPrueEnglish(roomName);
            if (!isPrueEnglish)
            {
                client.ActiveCharacter.SendOnlyEnglishCharactersAllowed("Room name");
                SendChatRoomCreatedResponse(client, CreateChatRoomStatus.UnableToOpen, client.ActiveCharacter.ChatRoom);
                return;
            }
            isPrueEnglish = Asda2EncodingHelper.IsPrueEnglish(password);
            if (!isPrueEnglish)
            {
                client.ActiveCharacter.SendOnlyEnglishCharactersAllowed("password");
                SendChatRoomCreatedResponse(client, CreateChatRoomStatus.UnableToOpen, client.ActiveCharacter.ChatRoom);
                return;
            }
            if (maxMemberCount > 20 || maxMemberCount < 2)
                maxMemberCount = 20;
            client.ActiveCharacter.ChatRoom = new Asda2Chatroom(client.ActiveCharacter, isPrivate, maxMemberCount, roomName, password);
            SendChatRoomCreatedResponse(client, CreateChatRoomStatus.Ok, client.ActiveCharacter.ChatRoom);
            SendChatRoomVisibleResponse(client.ActiveCharacter, ChatRoomVisibilityStatus.Visible, client.ActiveCharacter.ChatRoom);
        }

        public static void SendChatRoomCreatedResponse(IRealmClient client, CreateChatRoomStatus status,
                                                       Asda2Chatroom room)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ChatRoomCreated)) //6241
            {
                packet.WriteByte((byte)status); //{status}default value : 1 Len : 1
                packet.WriteByte(room == null ? 0 : room.IsPrivate ? 1 : 0);
                //{zeroPublicOnePrivate}default value : 1 Len : 1
                packet.WriteByte(room == null ? 0 : room.MaxMembersCount); //{maxMemberCount}default value : 15 Len : 1
                packet.WriteFixedAsciiString(room == null ? "" : room.Name, 28);
                //{roomName}default value :  Len : 28
                packet.WriteInt16(0); //value name : unk11 default value : 0Len : 2
                client.Send(packet);
            }
        }

        public static void SendChatRoomVisibleResponse(Character owner, ChatRoomVisibilityStatus status, Asda2Chatroom room, Character character = null)
        {
            if (character != null)
            {

                using (var packet = new RealmPacketOut(RealmServerOpCode.ChatRoomVisible)) //6248
                {
                    packet.WriteByte((byte)status); //{status}default value : 2 Len : 1
                    packet.WriteInt32(owner.AccId); //{initerAccId}default value : 361343 Len : 4
                    packet.WriteByte(room == null ? 0 : room.IsPrivate ? 1 : 0);
                    //{zeroPublicOnePrivate}default value : 1 Len : 1
                    packet.WriteInt16(room == null ? 0 : room.MaxMembersCount); //{maxMemberCount}default value : 15 Len : 1
                    packet.WriteFixedAsciiString(room == null ? "" : room.Name, 28);
                    //{roomName}default value :  Len : 28
                    packet.WriteInt16(0); //value name : unk11 default value : 0Len : 2
                    packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                    character.Send(packet, addEnd: true);
                }
                return;
            }
            using (var packet = new RealmPacketOut(RealmServerOpCode.ChatRoomVisible)) //6248
            {
                packet.WriteByte((byte)status); //{status}default value : 2 Len : 1
                packet.WriteInt32(owner.AccId); //{initerAccId}default value : 361343 Len : 4
                packet.WriteByte(room == null ? 0 : room.IsPrivate ? 1 : 0);  //{zeroPublicOnePrivate}default value : 1 Len : 1
                packet.WriteInt16(room == null ? 0 : room.MaxMembersCount); //{maxMemberCount}default value : 15 Len : 1
                packet.WriteFixedAsciiString(room == null ? "" : room.Name, 28);
                //{roomName}default value :  Len : 28
                packet.WriteInt16(0); //value name : unk11 default value : 0Len : 2
                packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                owner.SendPacketToArea(packet);
            }
        }

        [PacketHandler(RealmServerOpCode.EnterChatRoom)] //6242
        public static void EnterChatRoomRequest(IRealmClient client, RealmPacketIn packet)
        {
            var roomSessId = packet.ReadInt16(); //default : 17Len : 2
            var roomAccId = packet.ReadUInt32(); //default : 361343Len : 4
            var password = packet.ReadAsdaString(8, Locale.En); //default : Len : 8
            var targetChr = World.GetCharacterByAccId(roomAccId);
            if (targetChr == null || targetChr.ChatRoom == null)
            {
                SendEnterChatRoomResultResponse(client, EnterChatRoomStatus.WrongChatRoomInfo, null);
                return;
            }
            targetChr.ChatRoom.TryJoin(client.ActiveCharacter, password);
        }
        [PacketHandler(RealmServerOpCode.CloseChatRoom)]//6244
        public static void CloseChatRoomRequest(IRealmClient client, RealmPacketIn packet)
        {
            SendChatRoomClosedResponse(client, ChatRoomClosedStatus.Ok);
            if (client.ActiveCharacter.ChatRoom != null)
            {
                client.ActiveCharacter.ChatRoom.Leave(client.ActiveCharacter);
            }
        }


        public static void SendEnterChatRoomResultResponse(IRealmClient client, EnterChatRoomStatus status,
                                                           Asda2Chatroom room)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.EnterChatRoomResult)) //6243
            {
                packet.WriteByte((byte)status); //{status}default value : 1 Len : 1
                packet.WriteByte(room == null ? 0 : room.IsPrivate ? 1 : 2);
                //{privateOnePublicTwo}default value : 0 Len : 1
                packet.WriteByte(room == null ? 0 : room.MaxMembersCount); //{maxMembers}default value : 15 Len : 1
                packet.WriteByte(room == null ? 0 : room.Members.Count); //{currentMembers}default value : 2 Len : 1
                packet.WriteFixedAsciiString(room == null ? "" : room.Name, 28);
                //{roomName}default value :  Len : 28
                packet.WriteByte(0); //value name : unk10 default value : 0Len : 1
                packet.WriteByte(99); //value name : unk11 default value : 99Len : 1
                var mombers = room == null ? new Character[0] : room.Members.Values.ToArray();
                for (int i = 0; i < 20; i += 1)
                {
                    var chr = mombers.Length <= i ? null : mombers[i];
                    packet.WriteByte(chr == null ? 0 : room != null && (chr == room.Owner) ? 1 : 0);
                    //{isLeader}default value : 1 Len : 1
                    packet.WriteInt32(chr == null ? -1 : (int)chr.AccId); //{accId}default value : 361343 Len : 4
                    packet.WriteInt16(chr == null ? -1 : chr.SessionId); //{sessId}default value : 28 Len : 2
                }
                client.Send(packet, addEnd: true);
            }
        }

        public static void SendChatRoomEventResponse(Asda2Chatroom client, ChatRoomEventType status, Character triggerer)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ChatRoomEvent)) //6249
            {
                packet.WriteByte((byte)status); //{status}default value : 3 Len : 1
                packet.WriteInt16(triggerer == null ? 0 : triggerer.SessionId);
                //{trigererSessId}default value : 79 Len : 2
                packet.WriteInt32(triggerer == null ? 0 : triggerer.AccId);
                //{trigererAccId}default value : 366338 Len : 4
                client.Send(packet, true, Locale.Any);
            }
        }

        public static void SendChatRoomClosedResponse(IRealmClient client, ChatRoomClosedStatus status)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ChatRoomClosed)) //6245
            {
                packet.WriteByte((byte)status); //{status}default value : 1 Len : 1
                client.Send(packet);
            }
        }

        [PacketHandler(RealmServerOpCode.SendRoomChatMessage)]//6246
        public static void SendRoomChatMessageRequest(IRealmClient client, RealmPacketIn packet)
        {
            var color = packet.ReadInt32();//default : -10240Len : 4
            packet.Position += 4;//nk9 default : 0Len : 4
            var message = packet.ReadAsciiString(client.Locale);//default : Len : 0
            if (client.ActiveCharacter.ChatRoom == null)
            {
                client.ActiveCharacter.SendInfoMsg("You are not in chat room.");
                return;
            }
            var locale = Asda2EncodingHelper.MinimumAvailableLocale(client.Locale, message);
            SendRoomChatMsgResponse(client, client.ActiveCharacter.AccId, color, message, locale);
        }

        public static void SendRoomChatMsgResponse(IRealmClient client, uint senderAccId, int color, string msg, Locale locale)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.RoomChatMsg))//6247
            {
                packet.WriteInt32(senderAccId);//{accId}default value : 361343 Len : 4
                packet.WriteInt32(color);//{color}default value : -10240 Len : 4
                packet.WriteInt32(0);//value name : unk7 default value : 0Len : 4
                packet.WriteAsciiString(msg, locale);//{msg}default value :  Len : 0
                client.ActiveCharacter.ChatRoom.Send(packet, true, locale);
            }
        }
        [PacketHandler(RealmServerOpCode.DissmissPlayerFromChatRoom)]//6250
        public static void DissmissPlayerFromChatRoomRequest(IRealmClient client, RealmPacketIn packet)
        {
            var targetAccId = packet.ReadUInt32();//default : 366338Len : 4
            if (client.ActiveCharacter.ChatRoom == null)
            {
                client.ActiveCharacter.SendInfoMsg("You are not in chat room.");
                return;
            }
            client.ActiveCharacter.ChatRoom.Dissmiss(client.ActiveCharacter, targetAccId);
        }

        public static void SendDissmisedFromCharRoomResultResponse(IRealmClient client, DissmissCharacterFromChatRoomResult status)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.DissmisedFromCharRoomResult))//6251
            {
                packet.WriteByte((byte)status);//{status}default value : 1 Len : 1
                client.Send(packet, addEnd: true);
            }
        }

        #endregion

        #endregion

    }

    public enum DissmissCharacterFromChatRoomResult
    {
        Fail = 0,
        Ok = 1
    }

    public class Asda2Chatroom
    {
        public Character Owner { get; set; }
        public Dictionary<uint, Character> Members = new Dictionary<uint, Character>();
        public string Password { get; set; }
        public byte MaxMembersCount { get; set; }
        public bool IsPrivate { get; set; }
        public string Name { get; set; }

        public Asda2Chatroom(Character activeCharacter, bool isPrivate, byte maxMemberCount, string roomName, string password)
        {

            Owner = activeCharacter;
            Members.Add(activeCharacter.AccId, activeCharacter);
            IsPrivate = isPrivate;
            MaxMembersCount = maxMemberCount;
            Name = roomName;
            Password = password;
        }

        public void TryJoin(Character joiner, string password)
        {

            if (MaxMembersCount <= Members.Count)
            {
                ChatMgr.SendEnterChatRoomResultResponse(joiner.Client, EnterChatRoomStatus.RoomisFull, null);
                return;
            }
            if (IsPrivate && password != Password)
            {
                ChatMgr.SendEnterChatRoomResultResponse(joiner.Client, EnterChatRoomStatus.WrongPassword, null);
                return;
            }
            lock (this)
            {
                Members.Add(joiner.AccId, joiner);
            }
            joiner.ChatRoom = this;
            ChatMgr.SendChatRoomEventResponse(this, ChatRoomEventType.Joined, joiner);
            ChatMgr.SendEnterChatRoomResultResponse(joiner.Client, EnterChatRoomStatus.Ok, this);

        }

        public void Leave(Character leaver)
        {
            lock (this)
            {
                Members.Remove(leaver.AccId);
            }
            leaver.ChatRoom = null;
            ChatMgr.SendChatRoomEventResponse(this, ChatRoomEventType.Left, leaver);
            if (Owner == leaver && Members.Count > 0)
            {
                Owner = Members.Values.First();
                ChatMgr.SendChatRoomEventResponse(this, ChatRoomEventType.LeaderChanged, Owner);
                ChatMgr.SendChatRoomVisibleResponse(Owner, ChatRoomVisibilityStatus.Visible, this);
            }
            ChatMgr.SendChatRoomVisibleResponse(leaver, ChatRoomVisibilityStatus.Closed, null);
        }

        public void Dissmiss(Character dissmiser, uint targetAccId)
        {
            lock (this)
            {
                if (dissmiser != Owner || dissmiser.EntityId.Low == targetAccId)
                {
                    dissmiser.SendInfoMsg("You are not chat room owner.");
                    ChatMgr.SendDissmisedFromCharRoomResultResponse(dissmiser.Client, DissmissCharacterFromChatRoomResult.Fail);
                }
                if (!Members.ContainsKey(targetAccId))
                {

                    dissmiser.SendInfoMsg("Target not founded.");
                    ChatMgr.SendDissmisedFromCharRoomResultResponse(dissmiser.Client, DissmissCharacterFromChatRoomResult.Fail);
                }
                var target = Members[targetAccId];
                ChatMgr.SendChatRoomEventResponse(this, ChatRoomEventType.Banned, target);
                target.ChatRoom = null;
                Members.Remove(targetAccId);
                ChatMgr.SendChatRoomClosedResponse(target.Client, ChatRoomClosedStatus.Banned);
                ChatMgr.SendDissmisedFromCharRoomResultResponse(dissmiser.Client, DissmissCharacterFromChatRoomResult.Ok);
            }
        }

        public void Send(RealmPacketOut packet, bool addEnd, Locale locale)
        {
            lock (this)
            {
                foreach (var character in Members.Values)
                {
                    if (locale == Locale.Any || character.Client.Locale == locale)
                        character.Send(packet, addEnd: addEnd);
                }
            }
        }
    }

    public enum CreateChatRoomStatus
    {
        Error = 0,
        Ok = 1,
        UnableToOpen = 2,
        UnableOpenOnBattle = 3,
        YouAreAlreadyInChatRoom = 4,
        CapacityError = 5,
        SetPassword = 6,
        YouCanOnlyOpenChatRoomInTown = 7,
        YouCantOpenChatRoomWhileInHideMode = 8,

    }

    public enum ChatRoomClosedStatus
    {
        Error = 0,
        Ok = 1,
        Banned = 2,
    }
    public enum WishperStatus
    {
        Ok = 1,
        DontAcceptWishper = 2,
        NotFound = 3,
        AnotherFaction = 4

    }
    public enum ChatRoomVisibilityStatus
    {
        Closed = 0,
        Visible = 1,
    }

    public enum EnterChatRoomStatus
    {
        Error = 0,
        Ok = 1,
        WrongChatRoomInfo = 2,
        RoomisFull = 3,
        WrongPassword = 4,

    }

    public enum ChatRoomEventType
    {
        Left = 0,
        Joined = 1,
        LeaderChanged = 2,
        Banned = 3
    }
}