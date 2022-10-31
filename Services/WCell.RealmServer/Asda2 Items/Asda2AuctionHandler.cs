using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Items;
using WCell.RealmServer.Network;

namespace WCell.RealmServer.Asda2_Items
{
    internal class Asda2AuctionHandler
    {
        [PacketHandler(RealmServerOpCode.RegisterItemToAuk)] //9901
        public static void RegisterItemToAukRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position += 10;
            var chr = client.ActiveCharacter;
            var inv = chr.Asda2Inventory;
            var totalPrice = 0;
            var items = new List<Asda2ItemTradeRef>();
            for (int i = 0; i < 5; i += 1)
            {
                if (packet.RemainingLength < 50)
                {
                    break;
                }
                var invNum = packet.ReadByte(); //default : 1Len : 1
                var cell = packet.ReadInt16(); //default : 1Len : 2
                packet.Position += 12; //tab57 default : stab57Len : 12
                var amount = packet.ReadInt32(); //default : 56Len : 4
                packet.Position += 34; //tab73 default : stab73Len : 34
                var price = packet.ReadInt32(); //default : 8400Len : 4
                var time = packet.ReadByte(); //default : 8400Len : 4
                Asda2Item asda2Item = chr.Asda2Inventory.ShopItems[cell];


                client.ActiveCharacter.SendErrorMsg("PP = " + packet.Position);

                if (price < 0 || price > 100000000)
                {
                    client.ActiveCharacter.YouAreFuckingCheater(
                        "Tried to use wrong price while registering auk items : " + cell);
                    SendRegisterItemToAukCancelWindowResponse(client);
                    return;
                }
                if (cell < 0 || cell > 70)
                {
                    client.ActiveCharacter.YouAreFuckingCheater(
                        "Tried to use wrong cell while registering auk items : " + cell);
                    SendRegisterItemToAukCancelWindowResponse(client);
                    return;
                }
                Asda2Item item = null;
                switch ((Asda2InventoryType)invNum)
                {
                    case Asda2InventoryType.Regular:
                        item = inv.RegularItems[cell];
                        break;
                    case Asda2InventoryType.Shop:
                        item = inv.ShopItems[cell];
                        break;
                    default:
                        client.ActiveCharacter.YouAreFuckingCheater(
                            "Tried to use wrong inventory while registering auk items : " + invNum);
                        SendRegisterItemToAukCancelWindowResponse(client);
                        return;
                }
                items.Add(new Asda2ItemTradeRef { Item = item, Amount = amount, Price = price, AucTime = time});
                totalPrice += price;
            }
            if (client.ActiveCharacter.Money <= totalPrice * CharacterFormulas.AuctionPushComission)
            {
                client.ActiveCharacter.SendAuctionMsg("Not enought money to register items.");
                SendRegisterItemToAukCancelWindowResponse(client);
                return;
            }
            SendRegisterItemToAukCancelWindowResponse(client, items);
            foreach (var itemRef in items)
            {
                if (itemRef.Item == null)
                {
                    chr.SendAuctionMsg("Failed to register item cause not founded.");
                    SendRegisterItemToAukCancelWindowResponse(client);
                    return;
                }
                if (itemRef.Amount < 0 || itemRef.Amount > itemRef.Item.Amount)
                {
                    client.ActiveCharacter.YouAreFuckingCheater(
                        "Tried to use wrong item amount while registering auk items : " + itemRef.Amount);
                    SendRegisterItemToAukCancelWindowResponse(client);
                    return;
                }
                if (itemRef.Item.IsSoulbound)
                {
                    client.ActiveCharacter.YouAreFuckingCheater(
                        "Tried to use soulbounded item while registering auk items : " + itemRef.Amount);
                    SendRegisterItemToAukCancelWindowResponse(client);
                    return;
                }
                inv.AuctionItem(itemRef);
            }
            chr.SendAuctionMsg("You have success with registering auction items.");
            chr.SendMoneyUpdate();
        }

        public static void SendRegisterItemToAukCancelWindowResponse(IRealmClient client, List<Asda2ItemTradeRef> items = null)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.RegisterItemToAukCancelWindow))//9902
            {
                if (items != null)
                    foreach (var item in items)
                    {
                        packet.WriteByte(0);//{status}default value : 0 Len : 1
                        packet.WriteInt32(item.Item.ItemId);//{itemId}default value : 31855 Len : 4
                        packet.WriteByte((byte)item.Item.InventoryType);//{invNum}default value : 2 Len : 1
                        packet.WriteInt16(item.Item.Slot);//{cell}default value : 5 Len : 2
                        packet.WriteSkip(stab15);//value name : stab15 default value : stab15Len : 12
                        packet.WriteInt32(item.Amount);//{registeredAmount}default value : 250 Len : 4
                        packet.WriteInt32(item.Item.Amount);//{beforeAmount}default value : 250 Len : 4
                        packet.WriteInt16(item.Item.Weight);//{weight}default value : 0 Len : 2
                        packet.WriteSkip(stab37);//value name : stab37 default value : stab37Len : 21
                        packet.WriteInt32(client.ActiveCharacter.Asda2Inventory.Weight);//{invWeight}default value : 315 Len : 4
                        packet.WriteInt32(client.ActiveCharacter.Money);//{money}default value : 8503216 Len : 4
                        packet.WriteInt64(-1);//value name : unk8 default value : -1Len : 8
                    }
                client.Send(packet);
            }
        }
        static readonly byte[] stab15 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        static readonly byte[] stab37 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };


        [PacketHandler(RealmServerOpCode.ShowMyAukItems)] //9907
        public static void ShowMyAukItemsRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position += 6; //tab35 default : stab35Len : 1
            var inv = packet.ReadByte();
            if (inv == 1)
            {
                //regualr items
                SendMyAukItemsInfoResponse(client,
                                           Asda2AuctionMgr.GetCharacterRegularItems(
                                               (uint)client.ActiveCharacter.Record.Guid));
            }
            else if (inv == 2)
            {
                //equip items
                SendMyAukItemsInfoResponse(client,
                                           Asda2AuctionMgr.GetCharacterShopItems(
                                               (uint)client.ActiveCharacter.Record.Guid));
            }
            client.ActiveCharacter.SendErrorMsg("PP = " + packet.Position);
            client.ActiveCharacter.SendErrorMsg("PP = " + inv);

        }

        public static void SendMyAukItemsInfoResponse(IRealmClient client, List<Asda2ItemRecord> items)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.MyAukItemsInfo)) //9908
            {
                var i = 0;
                foreach (var item in items)
                {
                    if (i == 8)
                        break;
                    packet.WriteInt32(item.AuctionId); //{aukId}default value : 1179 Len : 4
                    packet.WriteByte(0); //value name : unk6 default value : 0Len : 1
                    packet.WriteInt32(item.ItemId); //{itemId}default value : 20579 Len : 4
                    packet.WriteInt32(item.Amount); //{amount}default value : 93 Len : 4
                    packet.WriteByte(item.Durability); //{durability}default value : 0 Len : 1
                    packet.WriteByte(item.Enchant); //{enchant}default value : 0 Len : 1
                    packet.WriteInt32(0); //value name : unk11 default value : 0Len : 4
                    packet.WriteInt32(526300); //value name : unk12 default value : 526300Len : 4
                    packet.WriteInt16(13); //value name : unk2 default value : 13Len : 2
                    packet.WriteInt16(item.Parametr1Type);
                    packet.WriteInt16(item.Parametr1Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr2Type);
                    packet.WriteInt16(item.Parametr2Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr3Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr3Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr4Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr4Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr5Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr5Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteByte(0); //value name : unk23 default value : 0Len : 1

                    packet.WriteInt32((int)(item.AuctionEndTime - DateTime.Now).TotalMilliseconds);
                    //{timeToEnd}default value : 604735000 Len : 4
                    packet.WriteInt32(item.AuctionPrice); //{price}default value : 13950 Len : 4
                    packet.WriteInt16(item.Soul1Id); //{soul1Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul2Id); //{soul2id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul3Id); //{soul3Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul4Id); //{soul4Id}default value : -1 Len : 2
                    i++;
                }


                client.Send(packet);
            }
        }


        [PacketHandler(RealmServerOpCode.LoadDataFromAukPage)] //9903
        public static void LoadDataFromAukPageRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position += 6; //tab35 default : stab35Len : 5
            var category = (AucionCategoties)packet.ReadInt16(); //default : 267Len : 2
            packet.Position += 1; //tab52 default : stab52Len : 1
            var option1 = packet.ReadInt16(); //default : 100Len : 2
            var option2 = packet.ReadByte(); //default : 0Len : 1
            var option3 = packet.ReadByte(); //default : 2Len : 1
            var pageNum = packet.ReadByte(); //default : 0Len : 1
            client.ActiveCharacter.SendErrorMsg(string.Format("category = {0}, Option1 = {1}", category, option1));
            client.ActiveCharacter.SendErrorMsg(string.Format("Option2 = {0}, Option3 = {1}", option2, option3));
            client.ActiveCharacter.SendErrorMsg(string.Format("PageNum = {0}", pageNum));

            try
            {
                AuctionLevelCriterion requiredLevelCriterion;
                Asda2ItemAuctionCategory reqCategory = CalcCategory(category, option1, option2, option3,
                                                                    out requiredLevelCriterion);
                var col = Asda2AuctionMgr.CategorizedItemsById[reqCategory][requiredLevelCriterion];
                SendItemsOnAukInfoResponse(client, col.Skip(pageNum * 8).Take(8), (byte)((col.Count - 1) / 8), pageNum);
            }
            catch
            {
                client.ActiveCharacter.YouAreFuckingCheater("Sends wrong auction show items request.");
            }
            client.ActiveCharacter.SendErrorMsg("PP = " + packet.Position);
        }

        private static Asda2ItemAuctionCategory CalcCategory(AucionCategoties category, short option1, byte option2,
                                                             byte option3,
                                                             out AuctionLevelCriterion requiredLevelCriterion)
        {
            requiredLevelCriterion = AuctionLevelCriterion.All;
            switch (category)
            {
                case AucionCategoties.Rings:
                    requiredLevelCriterion = (AuctionLevelCriterion)option1;
                    return Asda2ItemAuctionCategory.Ring;
                case AucionCategoties.Nackless:
                    requiredLevelCriterion = (AuctionLevelCriterion)option1;
                    return Asda2ItemAuctionCategory.Nackless;
                case AucionCategoties.Sowel:
                    requiredLevelCriterion = (AuctionLevelCriterion)option1;
                    switch ((Asda2SowelTypes)option3)
                    {
                        case Asda2SowelTypes.Main:
                            switch((Asda2SowelsTypes)option2)
                            {
                                case Asda2SowelsTypes.Brave:
                                    return Asda2ItemAuctionCategory.SowelAttack;
                                case Asda2SowelsTypes.Focus:
                                    return Asda2ItemAuctionCategory.SowelFocus;
                                case Asda2SowelsTypes.Wisdom:
                                    return Asda2ItemAuctionCategory.SowelWisdom;
                                case Asda2SowelsTypes.Patint:
                                    return Asda2ItemAuctionCategory.SowelDefence;
                                case Asda2SowelsTypes.Protect:
                                    return Asda2ItemAuctionCategory.SowelBlock;
                                default:
                                    return Asda2ItemAuctionCategory.SowelMisc;
                            }
                        case Asda2SowelTypes.Secondery:
                            switch((Asda2SowelsTypes)option2)
                            {
                                case Asda2SowelsTypes.Strength:
                                    return Asda2ItemAuctionCategory.SowelStrengs;
                                case Asda2SowelsTypes.Dexterity:
                                    return Asda2ItemAuctionCategory.SowelDexterity;
                                case Asda2SowelsTypes.Stamina:
                                    return Asda2ItemAuctionCategory.SowelStamina;
                                case Asda2SowelsTypes.Spirit:
                                    return Asda2ItemAuctionCategory.SowelSpirit;
                                case Asda2SowelsTypes.Intellect:
                                    return Asda2ItemAuctionCategory.SowelIntellect;
                                case Asda2SowelsTypes.Luck:
                                    return Asda2ItemAuctionCategory.SowelLuck;
                                case Asda2SowelsTypes.Misc:
                                    return Asda2ItemAuctionCategory.SowelMisc;
                                case Asda2SowelsTypes.Resest:
                                    return Asda2ItemAuctionCategory.SowelMisc;
                                default:
                                    return Asda2ItemAuctionCategory.SowelMisc;
                            }
                        default:
                            return Asda2ItemAuctionCategory.Misc;
                            
                    }
                case AucionCategoties.HollyWater:
                    return Asda2ItemAuctionCategory.Misc;
                case AucionCategoties.Scroll:
                    return Asda2ItemAuctionCategory.UpgradeScroll;
                case AucionCategoties.Potion:
                    return Asda2ItemAuctionCategory.Potion;
                case AucionCategoties.Crafting:
                    return Asda2ItemAuctionCategory.Crafting;
                case AucionCategoties.Premium:
                    return Asda2ItemAuctionCategory.Premium;
                case AucionCategoties.Shield:
                    return Asda2ItemAuctionCategory.Shield;
                case AucionCategoties.Weapon:
                    requiredLevelCriterion = (AuctionLevelCriterion)option1;
                    switch ((Asda2WeaponCategory)option2)
                    {
                        case Asda2WeaponCategory.Staff:
                            return Asda2ItemAuctionCategory.WeaponStaff;
                        case Asda2WeaponCategory.Bow:
                            return Asda2ItemAuctionCategory.WeaponBow;
                        case Asda2WeaponCategory.Crossbow:
                            return Asda2ItemAuctionCategory.WeaponCrossbow;
                        case Asda2WeaponCategory.Ballista:
                            return Asda2ItemAuctionCategory.WeaponBallista;
                        case Asda2WeaponCategory.OHS:
                            return Asda2ItemAuctionCategory.WeaponOhs;
                        case Asda2WeaponCategory.Spear:
                            return Asda2ItemAuctionCategory.WeaponSpear;
                        case Asda2WeaponCategory.THS:
                            return Asda2ItemAuctionCategory.WeaponThs;
                        case Asda2WeaponCategory.ShortSword:
                            return Asda2ItemAuctionCategory.WeaponShortSword;
                        case Asda2WeaponCategory.ShortBow:
                            return Asda2ItemAuctionCategory.WeaponShortBow;
                        default:
                            return Asda2ItemAuctionCategory.WeaponShortSword;
                    }
                case AucionCategoties.Helmet:
                    return Asda2ItemAuctionCategory.Helmet;
                case AucionCategoties.Armor:
                    return Asda2ItemAuctionCategory.Armor;
                case AucionCategoties.Pants:
                    return Asda2ItemAuctionCategory.Pants;
                case AucionCategoties.Gloves:
                    return Asda2ItemAuctionCategory.Gloves;
                case AucionCategoties.Shoes:
                    return Asda2ItemAuctionCategory.Boots;
                default:
                    return Asda2ItemAuctionCategory.Misc;
            }
        }

        public static void SendItemsOnAukInfoResponse(IRealmClient client, IEnumerable<Asda2ItemRecord> items,
                                                      byte pagesCount, byte curPage)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ItemsOnAukInfo)) //9904
            {
                foreach (var item in items)
                {
                    packet.WriteInt32(item.AuctionId); //{aukId}default value : 945 Len : 4
                    packet.WriteByte(0); //value name : unk6 default value : 0Len : 1
                    packet.WriteInt32(item.ItemId); //{itemId}default value : 23802 Len : 4
                    packet.WriteInt32(item.Amount); //{amount}default value : 0 Len : 4
                    packet.WriteByte(item.Durability); //{durability}default value : 90 Len : 1
                    packet.WriteByte(item.Enchant); //{enchant}default value : 10 Len : 1
                    packet.WriteInt32(16777216); //value name : unk11 default value : 16777216Len : 4
                    packet.WriteInt32(0); //value name : unk12 default value : 0Len : 4
                    packet.WriteInt16(0); //value name : unk13 default value : 0Len : 2
                    packet.WriteInt16(item.Parametr1Type);
                    packet.WriteInt16(item.Parametr1Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr2Type);
                    packet.WriteInt16(item.Parametr2Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr3Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr3Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr4Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr4Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Parametr5Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr5Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteByte(0); //value name : unk24 default value : unk24Len : 2
                    packet.WriteByte(curPage);
                    packet.WriteByte(pagesCount); //{pagesCount}default value : 0 Len : 1
                    packet.WriteInt32(item.AuctionPrice); //{money}default value : 850000 Len : 4
                    packet.WriteFixedAsciiString(item.OwnerName, 20); //{name}default value :  Len : 20
                    packet.WriteInt16(item.Soul1Id); //{soul1Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul2Id); //{soul2id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul3Id); //{soul3Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul4Id); //{soul4Id}default value : -1 Len : 2
                }

                client.Send(packet);
            }
        }

        private static readonly byte[] unk24 = new byte[] { 0x00, 0x00 };

        [PacketHandler(RealmServerOpCode.BuyFromAuk)] //9905
        public static void BuyFromAukRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 4; //tab35 default : stab35Len : 11
            var aucIds = new List<int>();
            for (int i = 0; i < 8; i += 1)
            {
                packet.Position += 1;
                if (packet.RemainingLength <= 0)
                    break;
                packet.Position += 9;
                var aukId = packet.ReadInt32(); //default : 924Len : 4
                aucIds.Add(aukId);
                packet.Position += 41; //default : stub8Len : 41
                client.ActiveCharacter.SendErrorMsg(string.Format("Id = {0}", aukId));

            }
            client.ActiveCharacter.SendErrorMsg(string.Format("PP = {0}", packet.Position));

            RealmServer.IOQueue.AddMessage(() => Asda2AuctionMgr.TryBuy(aucIds, client.ActiveCharacter));
        }

        public static void SendItemsBuyedFromAukResponse(IRealmClient client, List<Asda2ItemTradeRef> items)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ItemsBuyedFromAuk)) //9906
            {
                var i = 0;
                foreach (var itemRef in items)
                {
                    var item = itemRef.Item;
                    if (i >= 8)
                        break;
                    packet.WriteInt32(itemRef.Price); //{aukId}default value : 1179 Len : 4
                    packet.WriteSkip(stub4); //{stub4}default value : stub4 Len : 3
                    packet.WriteInt32(item.ItemId); //{itemId%}default value : 0 Len : 4
                    packet.WriteInt32(itemRef.Amount); //{quantity}default value : 0 Len : 4
                    packet.WriteByte((byte)item.InventoryType); //{invNum}default value : 0 Len : 1
                    packet.WriteInt16(item.Slot); //{slot%}default value : -1 Len : 2
                    packet.WriteInt16(item.Weight); //{weight%}default value : 0 Len : 2
                    packet.WriteByte(item.Durability); //{durability%}default value : 0 Len : 1
                    packet.WriteInt32(item.Enchant); //{enchant}default value : 0 Len : 4
                    packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                    packet.WriteInt32(0); //value name : unk4 default value : 0Len : 4
                    packet.WriteInt16(0); //value name : unk2 default value : 0Len : 2
                    packet.WriteInt16(item.Record.Parametr1Type);
                    packet.WriteInt16(item.Parametr1Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr2Type);
                    packet.WriteInt16(item.Parametr2Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr3Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr3Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr4Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr4Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr5Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr5Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                    packet.WriteInt32(item.AuctionPrice); //{price}default value : 0 Len : 4
                    packet.WriteFixedAsciiString(client.ActiveCharacter.Name, 20); //{name}default value :  Len : 20
                    packet.WriteInt16(item.Soul1Id); //{soul1Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul2Id); //{soul2id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul3Id); //{soul3Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul4Id); //{soul4Id}default value : -1 Len : 2
                    i++;
                }

                client.Send(packet);
            }
        }


        private static readonly byte[] stub4 = new byte[] { 0x00, 0x00, 0x00 };

        [PacketHandler(RealmServerOpCode.RemoveFromAuk)] //9909
        public static void RemoveFromAukRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 4;
            var items = new List<int>();
            for (int i = 0; i < 8; i += 1)
            {
                packet.Position += 19; //nk7 default : unk7Len : 19
                if (packet.RemainingLength <= 4)
                    break;
                packet.Position -= 9;
                var aukId = packet.ReadInt32(); //default : 1179Len : 4
                packet.Position += 26;
                items.Add(aukId);
                client.ActiveCharacter.SendErrorMsg(string.Format("Id = {0}", aukId));
            }
            client.ActiveCharacter.SendErrorMsg(string.Format("PP = {0}", packet.Position));

            RealmServer.IOQueue.AddMessage(() =>
                Asda2AuctionMgr.TryRemoveItems(client.ActiveCharacter, items));
        }

        private static readonly byte[] unk7 = new byte[]
            {
                0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0x68, 0x81, 0x05, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00, 0x68, 0x81, 0x05,
                0x00
            };

        private static readonly byte[] stub23 = new byte[]
            {
                0x63, 0x50, 0x00, 0x00, 0x5D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };

        public static void SendItemFromAukRemovedResponse(IRealmClient client, List<Asda2ItemTradeRef> asda2Items)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ItemFromAukRemoved)) //9910
            {
                var i = 0;
                foreach (var itemRef in asda2Items)
                {
                    var item = itemRef.Item;
                    if (i >= 8)
                        break;
                    packet.WriteInt32(itemRef.Price); //{aukId}default value : 1179 Len : 4
                    packet.WriteSkip(stub4); //{stub4}default value : stub4 Len : 3
                    packet.WriteInt32(item.ItemId); //{itemId%}default value : 0 Len : 4
                    packet.WriteInt32(itemRef.Amount); //{quantity}default value : 0 Len : 4
                    packet.WriteByte((byte)item.InventoryType); //{invNum}default value : 0 Len : 1
                    packet.WriteInt16(item.Slot); //{slot%}default value : -1 Len : 2
                    packet.WriteInt16(item.Weight); //{weight%}default value : 0 Len : 2
                    packet.WriteByte(item.Durability); //{durability%}default value : 0 Len : 1
                    packet.WriteInt32(item.Enchant); //{enchant}default value : 0 Len : 4
                    packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                    packet.WriteInt32(0); //value name : unk4 default value : 0Len : 4
                    packet.WriteInt16(0); //value name : unk2 default value : 0Len : 2
                    packet.WriteInt16(item.Record.Parametr1Type);
                    packet.WriteInt16(item.Parametr1Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr2Type);
                    packet.WriteInt16(item.Parametr2Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr3Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr3Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr4Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr4Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteInt16(item.Record.Parametr5Type); //{stat1Type}default value : 1 Len : 2
                    packet.WriteInt16(item.Parametr5Value); //{stat1Value}default value : 9 Len : 2
                    packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                    packet.WriteInt16(item.Soul1Id); //{soul1Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul2Id); //{soul2id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul3Id); //{soul3Id}default value : -1 Len : 2
                    packet.WriteInt16(item.Soul4Id); //{soul4Id}default value : -1 Len : 2
                    i++;
                }
                client.Send(packet);
            }
        }
    }

    public enum AucionCategoties
    {
        //1-10 lvls per 10
        Rings = 5,
        //1-10 lvls per 10
        Nackless = 7,
        //1-10 lvls per 10
        Sowel = 1547,
        //D C B A S = 0 1 2 3 4 
        HollyWater = 523,
        // always 0
        Potion = 267,
        //reviepe level 1 - 10
        Scroll = 1035,
        //
        Crafting = 779,
        //D C B A S = 0 1 2 3 4
        Shield = 8,
        Weapon = 9,
        Helmet = 0,
        Armor = 1,
        Pants = 2,
        Gloves = 4,
        Shoes = 3,
        Premium = 12
    }
    public enum Asda2ArmorCategory
    {
        Helmet = 0,
        Armor,
        Pants,
        Boots,
        Gloves
    }
    public enum Asda2WeaponCategory
    {
        ShortSword = 0,
        OHS,
        Spear,
        THS,
        Crossbow,
        Bow,
        Ballista,
        Staff,
        ShortBow
    }
    public enum Asda2OtherItemTypes
    {
        Booster = 0,
        Misc = 2
    }
    public enum Asda2CraftItemTypes
    {
        Recipe = 0,
        Materials = 1
    }
    public enum Asda2PotionTypes
    {
        Hp = 0,
        Mp = 1,
        Fish = 2,
    }
    public enum Asda2UpgradeTypes
    {
        Weapon = 0,
        Armor = 1
    }

    public enum Asda2SowelsTypes
    {
        Brave = 1,
        Focus,
        Wisdom,
        Patint,
        Protect,
        Strength,
        Dexterity,
        Stamina,
        Spirit,
        Intellect,
        Luck,
        Misc,
        Resest
    }

    public enum Asda2SowelTypes
    {
        Main = 0,
        Secondery = 1
    }
}
