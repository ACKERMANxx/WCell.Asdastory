using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using WCell.Constants;
using WCell.Constants.Login;
using WCell.Core;
using WCell.Core.Network;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Auth.Accounts;
using WCell.RealmServer.Chat;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Logs;
using WCell.RealmServer.Network;
using WCell.RealmServer.Res;
using WCell.Util.Graphics;
using WCell.Util.NLog;
using WCell.Util.Threading;

namespace WCell.RealmServer.Handlers
{
    public static class Asda2LoginHandler
    {
        /// <summary>
        /// Triggered after an Account logs into the Realm-server
        /// </summary>
        public static event Action<RealmAccount> AccountLogin;

        /// <summary>
        /// Triggered before a client disconnects
        /// </summary>
        public static event Func<IRealmClient, CharacterRecord, CharacterRecord> BeforeLogin;


        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Sends an auth session success response to the client.
        /// </summary>
        /// <param name="client">the client to send to</param>
        public static void InviteToRealm(IRealmClient client)
        {
            var evt = AccountLogin;
            if (evt != null)
            {
                evt(client.Account);
            }
            RealmServer.Instance.OnClientAccepted(null, null);
        }


        #region Login Server

        #region Login
        [ClientPacketHandler(RealmServerOpCode.SelectServerRequest, IsGamePacket = false, RequiresLogin = false)]
        public static void SelectServerRequest(IRealmClient client, RealmPacketIn packet)
        {
            using (var packet2 = new RealmPacketOut(RealmServerOpCode.ChanelInfoResponse))//1013
            {
                packet2.WriteByte(0xF4);// unknown
                packet2.WriteInt16(2049);//unknown
                packet2.WriteInt16(50);//chanel1
                packet2.WriteInt16(-1);//chanel2
                packet2.WriteInt16(-1);//chanel3
                packet2.WriteInt16(-1);//chanel4
                packet2.WriteInt16(-1);//chanel5
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt64(-1);//unknown
                packet2.WriteInt16(-1);//unknown
                client.Send(packet2, addEnd: false);
            }
        }

        [ClientPacketHandler(RealmServerOpCode.DeleteCharacterRequest, IsGamePacket = false, RequiresLogin = false)]
        public static void DeleteCharacter(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 5;
            if (client.Account.AccountId == packet.ReadUInt32())
            {
                byte charNum = packet.ReadByte();
                var charLowId = (uint)(client.Account.AccountId + 1000000 * charNum);
                var ch = client.Account.GetCharacterRecord(charLowId);
                if (ch != null)
                {
                    var ChC = ch.CreateCharacter();
                    ChC.Create(client.Account, ch, client);
                    
                    if (ChC.GuildMember != null)
                    {
                        if (ChC.GuildMember.Rank.Name == "Guild Master")
                        {
                            ResponseOfDeleteCharacter(client,DeleteResponses.LeaderOfGuildError);
                            return;
                        }
                        ResponseOfDeleteCharacter(client, DeleteResponses.MemberOfGuildError);
                        return;
                    }
                    if (ch.TryDelete() == LoginErrorCode.CHAR_DELETE_SUCCESS)
                        ResponseOfDeleteCharacter(client);
                    ChC.Dispose();
                }
            }
            else
                client.Disconnect();

        }
        public static void ResponseOfDeleteCharacter(IRealmClient client,DeleteResponses Response = DeleteResponses.Success)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.DeleteCharacterRespone))
            {
                packet.WriteByte((byte)Response);
                client.Send(packet, addEnd: false);
            }
        }
        /// <summary>
        /// Handles an incoming player login request.
        /// </summary>
        /// <param name="client">the Session the incoming packet belongs to</param>
        /// <param name="packet">the full packet</param>        
        [ClientPacketHandler(RealmServerOpCode.EnterGameRequset, IsGamePacket = false, RequiresLogin = false)]
        public static void PlayerLoginRequestLS(IRealmClient client, RealmPacketIn packet)
        {
            if (!client.IsConnected || client.AuthAccount == null || client.Account == null)
            {
                client.Disconnect();
                return;
            }
            if (client.ActiveCharacter != null && client.ActiveCharacter.IsConnected)
            {
                //it's ok when you press ok your character start login if you press one more time you already connecting we need to wait more
                //log.Warn("Client {0} try's to connect to already connected character", client.AccountName);
                //client.Disconnect();
                return;
            }
             //unknown default : 0
            var charNum = packet.ReadByte(); //default : 10
            if (charNum < 10 || charNum > 12)
            {
                client.Disconnect();
                return;
            }
            //Console.WriteLine("Char ID : " + charNum);
            var charLowId = (uint)(client.Account.AccountId + 1000000 * charNum);
            //Console.WriteLine("Account ID 1: " + client.Account.AccountId);
            if (client.Account.GetCharacterRecord(charLowId) == null)
            {
                client.Disconnect();
                return;
            }
            PreLoginCharacter(client, charLowId, true);
        }


        public static void PreLoginCharacter(IRealmClient client, uint charLowId, bool isLoginStep)
        {
            try
            {
                var chr = World.GetCharacter(charLowId);

                client.Info = new ClientInformation();
                if (chr != null)
                {
                    chr.Client.Disconnect();
                    client.ActiveCharacter = chr;
                    chr.Map.AddMessage(new Message(() =>
                    {
                        if (!chr.IsInContext)
                        {
                            // Character was removed in the meantime -> Login again
                            // enqueue task in IO-Queue to sync with Character.Save()
                            RealmServer.IOQueue.AddMessage(
                                new Message(() => LoginCharacter(client, charLowId, isLoginStep)));
                        }
                        else
                        {
                            // reconnect Client with a logging out Character
                            if (isLoginStep)
                            {
                                chr.IsLoginServerStep = true;
                                chr.IsFirstGameConnection = true;
                            }
                            chr.ReconnectCharacter(client);
                            if (isLoginStep)
                                chr.Client.Disconnect(true);
                        }
                    }));
                }
                else
                {
                    LoginCharacter(client, charLowId, isLoginStep);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
                SendCharacterLoginFail(client, LoginErrorCode.CHAR_LOGIN_FAILED);
            }
        }


        private static void LoginCharacter(IRealmClient client, uint charLowId, bool isLoginStep)
        {
            var acc = client.Account;
            if (acc == null)
            {
                return;
            }

            var record = client.Account.GetCharacterRecord(charLowId);

            if (record == null)
            {
                log.Error(String.Format(WCell_RealmServer.CharacterNotFound, charLowId, acc.Name));

                client.Disconnect();
            }
            else if (client.ActiveCharacter == null)
            {
                Character chr = null;
                try
                {
                    var evt = BeforeLogin;
                    if (evt != null)
                    {
                        record = evt(client, record);
                        if (record == null)
                        {
                            throw new ArgumentNullException("record", "BeforeLogin returned null");
                        }
                    }
                    chr = record.CreateCharacter();
                    if (isLoginStep)
                    {
                        chr.IsLoginServerStep = true;
                        chr.IsFirstGameConnection = true;
                    }
                    chr.Create(acc, record, client);
                    chr.LoadAndLogin();

                }
                catch (Exception ex)
                {
                    LogUtil.ErrorException(ex, "Failed to load Character from Record: " + record);
                    if (chr != null)
                    {
                        // Force client to relog
                        chr.Dispose();
                        client.Disconnect();
                    }
                }
            }
        }

        /// <summary>
        /// Sends a "character login failed" error message to the client.
        /// </summary>
        /// <param name="client">the client to send to</param>
        /// <param name="error">the actual login error</param>
        public static void SendCharacterLoginFail(IPacketReceiver client, LoginErrorCode error)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.SMSG_CHARACTER_LOGIN_FAILED, 1))
            {
                packet.WriteByte((byte)error);
                client.Send(packet, addEnd: false);
            }
        }

        #endregion

        #region enterGame

        public static void SendEnterGameResposeResponse(IRealmClient client)
        {
            if (client.Account == null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.EnterGameRespose))//1017
            {
                var acc = client.Account;
                var ch = acc.ActiveCharacter;
                if(ch==null)
                    return;
                packet.WriteInt32(acc.AccountId);//Not Use Just Junk
                PacketStructureSegments.CharacterInformation(client, packet);
                PacketStructureSegments.CharacterShape(client, packet);
                PacketStructureSegments.CharacterAbilities(client, packet);
                PacketStructureSegments.CharacterUpdate(client, packet);
                client.Send(packet, addEnd: false);
            }
        }


        static readonly byte[] unk51 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public static void SendEnterGameResponseItemsOnCharacterResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.EnterGameResponseItemsOnCharacter)) //1027
            {
                packet.WriteByte(1); //default value : 1                
                packet.WriteInt32(0); //Junk
                PacketStructureSegments.AllItemInformation(client, packet);
                PacketStructureSegments.SettingsFlags(client, packet);
               
                client.Send(packet, addEnd: false);
            }
        }


        public static void SendEnterWorldIpeResponseResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.EnterWorldIpeResponse))//1021
            {
                packet.WriteInt32(-1);//value name : unk4 default value : -1Len : 4
                if(client.AddrTemp.Contains("192.168."))
                    packet.WriteFixedAsciiString(RealmServerConfiguration.ExternalAddress, 20);
                else
                    packet.WriteFixedAsciiString(RealmServerConfiguration.RealExternalAddress, 20);//"127.0.0.1", 20);//"10.8.0.10", 20);//RealmServerConfiguration.ExternalAddress, 20);//{ip}default value :  Len : 20
                packet.WriteUInt16(RealmServerConfiguration.Port);//15001);//RealmServerConfiguration.Port);//{port}default value : 15603 Len : 2
                packet.WriteInt16((short)client.ActiveCharacter.MapId);//{location}default value : 3 Len : 2
                packet.WriteInt16(Convert.ToInt16(client.ActiveCharacter.Position.X));//{x}default value : 0 Len : 2
                packet.WriteInt16(Convert.ToInt16(client.ActiveCharacter.Position.Y));//{y}default value : 0 Len : 2
                var auras = new Spells.Auras.Aura[28];
                var it = 0;
                foreach (var visibleAura in client.ActiveCharacter.Auras.ActiveAuras)
                {
                    if (visibleAura.TicksLeft <= 0)
                        continue;
                    auras[it++] = visibleAura;
                    if (auras.Length <= it)
                        break;
                }
                for (int i = 0; i < 28; i += 1)
                {
                    var spell = auras[i];
                    packet.WriteInt16(spell == null ? -1 : spell.Spell.RealId);//{guid}default value : 0 Len : 2
                    packet.WriteInt16(spell == null ? -1 : spell.Spell.RealId);//{durationSec}default value : 0 Len : 2
                    packet.WriteByte(spell == null ? 0 : 1);//value name : unk1 default value : 0Len : 1
                    packet.WriteByte(0);//value name : unk1 default value : 0Len : 1
                    packet.WriteByte(2);//value name : unk1 default value : 2Len : 1
                    packet.WriteInt16(spell == null ? 0 : spell.Duration / 1000);//{skillId}default value : -1 Len : 2
                    packet.WriteByte(1);//value name : unk14 default value : 1Len : 1
                    packet.WriteInt16(1);//value name : unk2 default value : 1Len : 2

                }
                var pbs = new FunctionItemBuff[15];
                var interator = 0;
                foreach (var functionItemBuff in client.ActiveCharacter.PremiumBuffs)
                {
                    if (functionItemBuff.Value.IsLongTime)
                        continue;
                    pbs[interator++] = functionItemBuff.Value;
                }
                for (int i = 0; i < 15; i += 1)
                {
                    var buff = pbs[i];
                    packet.WriteInt32(-1);//buff == null?-1:buff.ItemId);//{itemId}default value : 0 Len : 4
                    packet.WriteInt32(-1);//(int) (buff == null ? -1 : buff.Duration));//{duration}default value : 0 Len : 4
                    packet.WriteInt16(-1);//buff == null ? -1 : buff.Template.ValueOnUse);//{funcValue}default value : 0 Len : 2
                    packet.WriteInt32(-1);//value name : unk4 default value : -1Len : 4
                    packet.WriteInt32(0);//value name : unk4 default value : 0Len : 4
                    packet.WriteInt16(-1);//value name : unk2 default value : -1Len : 2
                }
                client.Send(packet);
            }
        }
        public static void SendLongTimeBuffsInfoResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.LongTimeBuffsInfo))//6633
            {
                for (int i = 0; i < 20; i += 1)
                {
                    var buff = client.ActiveCharacter.LongTimePremiumBuffs[i];
                    packet.WriteInt16(buff == null ? -1 : buff.Template.PackageId);//{guid}default value : -1 Len : 2
                    packet.WriteInt16(buff == null ? -1 : buff.ItemId);//{itemId}default value : -1 Len : 2
                    packet.WriteInt32((int)(buff == null ? -1 : (long)(buff.EndsDate - DateTime.Now).TotalSeconds));//{duration}default value : 0 Len : 4
                }
                client.Send(packet, addEnd: true);
            }
        }

        #endregion

        #endregion

        #region Game Server
        [PacketHandler(RealmServerOpCode.LocationInit, IsGamePacket = false, RequiresLogin = false)]//20000
        public static void LocationInitRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position += 11;
            var ACCId = packet.ReadUInt32();
            client.ActiveCharacter = World.CharactersByAccId[(ushort)ACCId];
           
#warning Come packet here again
            RealmServer.IOQueue.AddMessage(() =>
                                               {
                                                   using (var p = new RealmPacketOut(RealmServerOpCode.ClientCanLoginToGS))//6004
                                                   {
                                                       p.WriteByte(2);//default value : 2
                                                       client.Send(p, addEnd: false);
                                                   }
                                               });
        }

        [PacketHandler(RealmServerOpCode.CharacterInitOnLogin, IsGamePacket = false, RequiresLogin = false)]//4001
        public static void CharacterInitOnLoginRequest(IRealmClient client, RealmPacketIn packet)
        {
            //var accID = packet.ReadInt32();//default : 0
            //packet.Position += 2;// default : 0
            //var characterNum = packet.ReadInt16();//default : 0
            //if (characterNum < 10 || characterNum > 12)
            //{
            //    client.TcpSocket.Close();
            //    return;
            //}
            //packet.Position += 4;// default : -1
            //var charLowId = (uint)(accID + characterNum * 1000000);
            var accID = client.ActiveCharacter.AccId;
            var charLowId = client.ActiveCharacter.Record.EntityLowId;
            var ip = client.ClientAddress.ToString();

            var acc = AccountMgr.GetAccount(accID);
            if (acc == null || acc.LastIPStr != ip)
            {
                if (acc != null)
                    Log.Create(Log.Types.AccountOperations, LogSourceType.Account, (uint)accID)
                       .AddAttribute("operation", 1, "login_game_server_bad_ip")
                       .AddAttribute("name", 0, acc.Name)
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .AddAttribute("old_ip", 0, acc.LastIPStr)
                       .Write();
                client.Disconnect();
                return;
            }
            var realmAcc = RealmServer.Instance.GetLoggedInAccount(acc.Name);
            if (realmAcc == null || realmAcc.ActiveCharacter == null)
            {
                Log.Create(Log.Types.AccountOperations, LogSourceType.Account, (uint)accID)
                       .AddAttribute("operation", 1, "login_game_server_no_character_selected")
                       .AddAttribute("name", 0, acc.Name)
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .AddAttribute("old_ip", 0, acc.LastIPStr)
                       .Write();
                client.Disconnect();
                return;
            }
            client.IsGameServerConnection = true;
            client.Account = realmAcc;
            Log.Create(Log.Types.AccountOperations, LogSourceType.Account, (uint)accID)
                       .AddAttribute("operation", 1, "login_game_server")
                       .AddAttribute("name", 0, acc.Name)
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .AddAttribute("character", client.Account.ActiveCharacter.EntryId, client.Account.ActiveCharacter.Name)
                       .AddAttribute("chrLowId", charLowId)
                       .Write();
            Log.Create(Log.Types.AccountOperations, LogSourceType.Character, (uint)charLowId)
                       .AddAttribute("operation", 1, "login_game_server")
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .Write();
            PreLoginCharacter(client, charLowId, false);
        }
        [PacketHandler(RealmServerOpCode.CharacterInitOnChanelChange, IsGamePacket = false, RequiresLogin = false)]//5058
        public static void CharacterInitOnChanelChangeRequest(IRealmClient client, RealmPacketIn packet)
        {        
            packet.Position -= 5;
            if (client == null || client.ClientAddress == null)
                return;
           
            var accId = packet.ReadInt32();
            var acc = AccountMgr.GetAccount(accId);
            if (acc == null || acc.LastIPStr != client.ClientAddress.ToString())
            {
                if (acc != null)
                    Log.Create(Log.Types.AccountOperations, LogSourceType.Account, (uint)accId)
                       .AddAttribute("operation", 1, "login_on_map_change_bad_ip")
                       .AddAttribute("name", 0, acc.Name)
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .AddAttribute("old_ip", 0, acc.LastIPStr)
                       .Write();
                client.Disconnect();
                return;
            }
            var realmAcc = RealmServer.Instance.GetLoggedInAccount(acc.Name);
            if (realmAcc == null || realmAcc.ActiveCharacter == null)
            {
                client.Disconnect();
                return;
            }
            client.IsGameServerConnection = true;
            client.Account = realmAcc;
            var accLog = Log.Create(Log.Types.AccountOperations, LogSourceType.Account, (uint)accId)
                       .AddAttribute("operation", 1, "login_on_map_change")
                       .AddAttribute("name", 0, acc.Name)
                       .AddAttribute("chr", realmAcc.ActiveCharacter.EntryId, realmAcc.ActiveCharacter.Name)
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .Write();
            Log.Create(Log.Types.AccountOperations, LogSourceType.Character, realmAcc.ActiveCharacter.EntityId.Low)
                       .AddAttribute("operation", 1, "login_on_map_change")
                       .AddAttribute("ip", 0, client.ClientAddress.ToString())
                       .AddAttribute("chr", realmAcc.ActiveCharacter.EntryId, realmAcc.ActiveCharacter.Name)
                       .AddReference(accLog)
                       .Write();
            PreLoginCharacter(client, realmAcc.ActiveCharacter.EntityId.Low, false);
        }
        public static void SendInventoryInfoResponse(IRealmClient client)
        {
            var inventory = client.ActiveCharacter.Asda2Inventory;
            var inventoryPacks = new List<List<Asda2Item>>();
            var itemIndex = 0;
            var allItems = inventory.RegularItems.Where(it => it != null).ToArray();
            while (itemIndex < allItems.Length)
            {
                inventoryPacks.Add(new List<Asda2Item>(allItems.Skip(itemIndex).Take(9)));
                itemIndex += 9;
            }
            foreach (var inventoryPack in inventoryPacks)
            {
                using (var packet = new RealmPacketOut(RealmServerOpCode.RegularInventoryInfo))//4048
                {
                    for (int i = 0; i < inventoryPack.Count; i += 1)
                    {
                        var item = inventoryPack[i];
                        if (item != null)
                            Asda2InventoryHandler.WriteItemInfoToPacket(packet, item, false);
                        //WriteItemDataToPacket(item, packet, 2);
                    }
                    client.Send(packet, addEnd: false);
                }
            }
            inventoryPacks.Clear();
            itemIndex = 0;
            allItems = inventory.ShopItems.Where(it => it != null).ToArray();
            while (itemIndex < allItems.Length)
            {
                inventoryPacks.Add(new List<Asda2Item>(allItems.Skip(itemIndex).Take(9)));
                itemIndex += 9;
            }
            foreach (var inventoryPack in inventoryPacks)
            {
                using (var packet = new RealmPacketOut(RealmServerOpCode.ShopInventoryInfo))//4045
                {
                    for (int i = 0; i < inventoryPack.Count; i += 1)
                    {
                        var item = inventoryPack[i];
                        if (item != null)
                            Asda2InventoryHandler.WriteItemInfoToPacket(packet, item, false);
                        //WriteItemDataToPacket(item, packet, 1);
                    }
                    client.Send(packet, addEnd: false);
                }
            }

        }


        static readonly byte[] stab31 = new byte[] { 0x00, 0x00, 0x00 };

        #endregion
    }
}
