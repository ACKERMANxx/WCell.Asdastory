#region

using NHibernate;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using WCell.Constants;
using WCell.Constants.Items;
using WCell.RealmServer.Asda2Looting;
using WCell.RealmServer.Asda2PetSystem;
using WCell.RealmServer.Chat;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Items;
using WCell.RealmServer.Logs;
using WCell.Util;
using WCell.Util.Graphics;
using WCell.Util.NLog;
using WCell.RealmServer.Asda2Titles;
#endregion

namespace WCell.RealmServer.Asda2_Items
{
    public class Asda2PlayerInventory
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Asda2ItemCategory, DateTime> _cooldowns =
            new Dictionary<Asda2ItemCategory, DateTime>();

        public Asda2Item[] AvatarWarehouseItems = new Asda2Item[270];
        public Dictionary<int, Asda2DonationItem> DonationItems = new Dictionary<int, Asda2DonationItem>();

        public Asda2Item[] Equipment = new Asda2Item[21];
        public Asda2Item[] RegularItems = new Asda2Item[60];
        public Asda2Item[] ShopItems = new Asda2Item[61];
        public Asda2Item[] WarehouseItems = new Asda2Item[270];
        public Asda2Item[] QuestItems = new Asda2Item[60];


        private Dictionary<byte, Asda2FastItemSlotRecord[]> _fastItemSlotRecords;
        private Character m_owner;

        public Asda2PlayerInventory(Character character)
        {
            Owner = character;
        }

        public Character Owner
        {
            get { return m_owner; }
            private set { m_owner = value; }
        }

        public short Weight
        {
            get
            {
                
                //return 555;
                int w = 0;
                foreach (var item in Equipment.Where(x => x != null && x.Template != null && x.ItemId != 20551))
                    w += (item.Template.Weight * item.Amount);
                foreach (var item in RegularItems.Where(x => x != null && x.Template != null && x.ItemId != 20551))
                    w += (item.Weight * item.Amount);
                foreach (var item in ShopItems.Where(x => x != null && x.Template != null && x.ItemId != 20551))
                    w += (item.Template.Weight * item.Amount);
                //Console.WriteLine("w:" + w + " WeightCapacity:" + Owner.WeightCapacity);
                //Console.WriteLine((w * 1000 / (10000 + Owner.WeightCapacity)));
                return (short)(w * 1000 / (10000 + Owner.WeightCapacity));
            }
        }

        public Dictionary<byte, Asda2FastItemSlotRecord[]> FastItemSlotRecords
        {
            get
            {
                if (_fastItemSlotRecords == null)
                {
                    _fastItemSlotRecords = new Dictionary<byte, Asda2FastItemSlotRecord[]>();
                    for (byte i = 0; i <= 5; i++)
                    {
                        _fastItemSlotRecords.Add(i, new Asda2FastItemSlotRecord[13]);
                    }
                }
                return _fastItemSlotRecords;
            }
        }

        public int FreeRegularSlotsCount
        {
            get { return RegularItems.Count(i => i == null); }
        }

        public int FreeShopSlotsCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < (Owner.InventoryExpanded ? 60 : 30); i++)
                {
                    if (ShopItems[i] == null)
                        r++;
                }
                return r;
            }
        }

        public int FreeWarehouseSlotsCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < (Owner.Record.PremiumWarehouseBagsCount * 30 + 30); i++)
                {
                    if (WarehouseItems[i] == null)
                        r++;
                }
                return r;
            }
        }

        public int FreeAvatarWarehouseSlotsCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < (Owner.Record.PremiumAvatarWarehouseBagsCount * 30 + 30); i++)
                {
                    if (AvatarWarehouseItems[i] == null)
                        r++;
                }
                return r;
            }
        }

        public void SaveAll()
        {
            foreach (Asda2Item asda2Item in Equipment)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (Asda2Item asda2Item in RegularItems)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (Asda2Item asda2Item in ShopItems)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (Asda2Item asda2Item in WarehouseItems)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (Asda2Item asda2Item in AvatarWarehouseItems)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (Asda2Item asda2Item in QuestItems)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.Save();
            }
            foreach (KeyValuePair<byte, Asda2FastItemSlotRecord[]> kvp in FastItemSlotRecords)
            {
                foreach (Asda2FastItemSlotRecord fastItemRec in kvp.Value)
                {
                    if (fastItemRec == null)
                        continue;
                    try
                    {
                        fastItemRec.Save();
                    }
                    catch (StaleStateException) //хз почему просисходит при перерождении но похуй
                    {
                    }
                }
            }
        }

        public void SetEquipment(Asda2Item item, Asda2EquipmentSlots slot)
        {
            Asda2Item oldItem = Equipment[(int)slot];
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    LogUtil.WarnException("{0} trying to equip item {1} witch is deleted.", Owner.Name, item.ItemId);
                    return;
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to equip item {1} witch record is null.", Owner.Name, item.ItemId);
                    return;
                }
                item.Slot = (short)slot;
                item.InventoryType = Asda2InventoryType.Equipment;
                item.OwningCharacter = Owner;
            }
            Equipment[(int)slot] = item;
            if (item != null)
                item.Save();
            if (item == null && slot == Asda2EquipmentSlots.Weapon)
                Owner.MainWeapon = null;
            else if (item != null && (slot == Asda2EquipmentSlots.Weapon && item.IsWeapon))
                Owner.MainWeapon = item;

            if (oldItem != null)
            {
                Asda2InventoryHandler.SendCharacterRemoveEquipmentResponse(Owner, (short)slot, oldItem.ItemId);
                oldItem.OnUnEquip();
            }
            if (item != null)
            {
                Asda2InventoryHandler.SendCharacterAddEquipmentResponse(Owner, (short)slot, item.ItemId);
                item.OnEquip();
            }
        }

        private void SetRegularInventoty(Asda2Item item, short slot, bool silent)
        {
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    throw new InvalidOperationException(
                        string.Format("{0} trying to set regular item {1} witch is deleted.", Owner.Name, item.ItemId));
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to regular item {1} witch record is null.", Owner.Name,
                        item.ItemId);
                    return;
                }
                item.InventoryType = Asda2InventoryType.Regular;
                item.Slot = slot;
                item.OwningCharacter = Owner;
            }
            RegularItems[slot] = item;
            //if (item != null)
            //item.Save();

            if (silent)
                return;
            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, new[] { item });
            Owner.SendMoneyUpdate();
        }

        private void SetWarehouseInventoty(Asda2Item item, short slot, bool silent)
        {
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    throw new InvalidOperationException(string.Format(
                        "{0} trying to set wh item {1} witch is deleted.", Owner.Name, item.ItemId));
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to wh item {1} witch record is null.", Owner.Name, item.ItemId);
                    return;
                }
                item.InventoryType = Asda2InventoryType.Warehouse;
                item.Slot = slot;
                item.OwningCharacter = Owner;
            }
            WarehouseItems[slot] = item;
            //if (item != null)
            //    item.Save();
            if (item == null || silent)
                return;

            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, new[] { item });
            Owner.SendMoneyUpdate();
        }

        private void SetAvatarWarehouseInventoty(Asda2Item item, short slot, bool silent)
        {
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    throw new InvalidOperationException(string.Format(
                        "{0} trying to set awh item {1} witch is deleted.", Owner.Name, item.ItemId));
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to avatar wh item {1} witch record is null.", Owner.Name,
                        item.ItemId);
                    return;
                }
                item.InventoryType = Asda2InventoryType.AvatarWarehouse;
                item.Slot = slot;
                item.OwningCharacter = Owner;
            }
            AvatarWarehouseItems[slot] = item;
            if (item == null || silent)
                return;

            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, new[] { item });
            Owner.SendMoneyUpdate();
        }
        private void SetQuestsInventoty(Asda2Item item, short slot, bool silent)
        {
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    throw new InvalidOperationException(string.Format(
                        "{0} trying to set awh item {1} witch is deleted.", Owner.Name, item.ItemId));
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to avatar wh item {1} witch record is null.", Owner.Name,
                        item.ItemId);
                    return;
                }
                item.InventoryType = Asda2InventoryType.Quests;
                item.Slot = slot;
                item.OwningCharacter = Owner;
            }
            QuestItems[slot] = item;
            if (item == null || silent)
                return;

            //Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, new[] { item });
            Owner.SendMoneyUpdate();
        }

        private void SetShopInventoty(Asda2Item item, short slot, bool silent)
        {
            if (item != null)
            {
                if (item.IsDeleted)
                {
                    throw new InvalidOperationException(
                        string.Format("{0} trying to set shop item {1} witch is deleted.", Owner.Name, item.ItemId));
                }
                if (item.Record == null)
                {
                    LogUtil.WarnException("{0} trying to set shop item {1} witch record is null.", Owner.Name,
                        item.ItemId);
                    return;
                }
                item.InventoryType = Asda2InventoryType.Shop;
                item.Slot = slot;
                item.OwningCharacter = Owner;
            }
            ShopItems[slot] = item;
            // if (item != null)
            //     item.Save();
            if (item == null || silent)
                return;
            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, new[] { item });
            Owner.SendMoneyUpdate();
        }

        private void SetItem(Asda2Item item, short slot, Asda2InventoryType inventoryType, bool silent = true)
        {
            switch (inventoryType)
            {
                case Asda2InventoryType.Regular:
                    SetRegularInventoty(item, slot, silent);
                    break;
                case Asda2InventoryType.Shop:
                    SetShopInventoty(item, slot, silent);
                    break;
                case Asda2InventoryType.Warehouse:
                    SetWarehouseInventoty(item, slot, silent);
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    SetAvatarWarehouseInventoty(item, slot, silent);
                    break;
                case Asda2InventoryType.Quests:
                    SetQuestsInventoty(item, slot, silent);
                    break;
                default:
                    Owner.SendErrorMsg(string.Format("failed to set item. wrong inventory type {0}", inventoryType));
                    return;
            }
        }

        private short FindFreeShopItemsSlot()
        {
            for (short i = 0; i < (Owner.InventoryExpanded ? ShopItems.Length : 30); i++)
            {
                if (ShopItems[i] == null)
                    return i;
            }
            return -1;
        }

        private short FindFreeRegularItemsSlot()
        {
            for (short i = 1; i < RegularItems.Length; i++)
            {
                if (RegularItems[i] == null)
                    return i;
            }
            return -1;
        }

        private short FindFreeWarehouseItemsSlot()
        {
            for (short i = 0; i < WarehouseItems.Length; i++)
            {
                if (WarehouseItems[i] == null)
                    return i;
            }
            return -1;
        }

        private short FindFreeAvatarWarehouseItemsSlot()
        {
            for (short i = 0; i < WarehouseItems.Length; i++)
            {
                if (AvatarWarehouseItems[i] == null)
                    return i;
            }
            return -1;
        }
        private short FindFreeQuestsItemSlot()
        {
            for (short i = 0; i < QuestItems.Length; i++)
            {
                if (QuestItems[i] == null)
                    return i;
            }
            return -1;
        }

        internal void AddOwnedItemsLoadonly()
        {
            Asda2DonationItem[] donItms = Asda2DonationItem.LoadAll(Owner);
            foreach (Asda2DonationItem asda2DonationItem in donItms)
            {
                if (DonationItems.ContainsKey(asda2DonationItem.Guid))
                    continue;
                DonationItems.Add(asda2DonationItem.Guid, asda2DonationItem);
            }
            ICollection<Asda2FastItemSlotRecord> fastItemSlotsRecords = m_owner.Record.GetOrLoadFastItemSlots();
            foreach (Asda2FastItemSlotRecord rec in fastItemSlotsRecords)
            {
                if (rec.PanelNum > 5 || rec.PanelSlot > 11)
                {
                    Log.Warn("Bad fastitemslot record {0}", rec);
                    continue;
                }
                FastItemSlotRecords[rec.PanelNum][rec.PanelSlot] = rec;
            }

            ICollection<Asda2ItemRecord> records = m_owner.Record.GetOrLoadItems();
            if (records == null) return;
            var items = new List<Asda2Item>(records.Count);

            foreach (Asda2ItemRecord record in records)
            {
                //skip load if this itemrecord has been auctioned
                if (record.IsAuctioned)
                    continue;
                Asda2ItemTemplate template = Asda2ItemMgr.Templates.Get(record.ItemId);
                if (template == null)
                {
                    Log.Warn("Item #{0} on {1} could not be loaded because it had an invalid ItemId: {2} ({3})",
                        record.Guid, this, record.ItemId, record.ItemId);
                    continue;
                }

                Asda2Item item = Asda2Item.CreateItem(record, m_owner, template);
                items.Add(item);
            }

            foreach (Asda2Item item in items)
            {
                switch (item.InventoryType)
                {
                    case Asda2InventoryType.Shop:
                        if (item.Slot >= 0 && item.Slot < ShopItems.Length)
                            ShopItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.Regular:
                        if (item.Slot >= 0 && item.Slot < RegularItems.Length)
                            RegularItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.Equipment:
                        Equipment[item.Slot] = item;
                        break;
                    case Asda2InventoryType.Warehouse:
                        if (item.Slot >= 0 && item.Slot < WarehouseItems.Length)
                            WarehouseItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.AvatarWarehouse:
                        if (item.Slot >= 0 && item.Slot < AvatarWarehouseItems.Length)
                            AvatarWarehouseItems[item.Slot] = item;
                        break;
                }
            }
        }
        internal void AddOwnedItems()
        {
            Asda2DonationItem[] donItms = Asda2DonationItem.LoadAll(Owner);
            foreach (Asda2DonationItem asda2DonationItem in donItms)
            {
                if (DonationItems.ContainsKey(asda2DonationItem.Guid))
                    continue;
                DonationItems.Add(asda2DonationItem.Guid, asda2DonationItem);
            }
            ICollection<Asda2FastItemSlotRecord> fastItemSlotsRecords = m_owner.Record.GetOrLoadFastItemSlots();
            foreach (Asda2FastItemSlotRecord rec in fastItemSlotsRecords)
            {
                if (rec.PanelNum > 5 || rec.PanelSlot > 11)
                {
                    Log.Warn("Bad fastitemslot record {0}", rec);
                    continue;
                }
                FastItemSlotRecords[rec.PanelNum][rec.PanelSlot] = rec;
            }

            ICollection<Asda2ItemRecord> records = m_owner.Record.GetOrLoadItems();
            if (records == null) return;
            var items = new List<Asda2Item>(records.Count);

            foreach (Asda2ItemRecord record in records)
            {
                //skip load if this itemrecord has been auctioned
                if (record.IsAuctioned)
                    continue;
                Asda2ItemTemplate template = Asda2ItemMgr.Templates.Get(record.ItemId);
                if (template == null)
                {
                    Log.Warn("Item #{0} on {1} could not be loaded because it had an invalid ItemId: {2} ({3})",
                        record.Guid, this, record.ItemId, record.ItemId);
                    continue;
                }

                Asda2Item item = Asda2Item.CreateItem(record, m_owner, template);
                items.Add(item);
            }

            foreach (Asda2Item item in items)
            {
                switch (item.InventoryType)
                {
                    case Asda2InventoryType.Shop:
                        if (item.Slot >= 0 && item.Slot < ShopItems.Length)
                            ShopItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.Regular:
                        if (item.Slot >= 0 && item.Slot < RegularItems.Length)
                            RegularItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.Equipment:
                        SetEquipment(item, (Asda2EquipmentSlots)item.Slot);
                        break;
                    case Asda2InventoryType.Warehouse:
                        if (item.Slot >= 0 && item.Slot < WarehouseItems.Length)
                            WarehouseItems[item.Slot] = item;
                        break;
                    case Asda2InventoryType.AvatarWarehouse:
                        if (item.Slot >= 0 && item.Slot < AvatarWarehouseItems.Length)
                            AvatarWarehouseItems[item.Slot] = item;
                        break;
                }
            }
        }

        public Asda2InventoryError TrySwap(Asda2InventoryType srcInv, short srcSlot, Asda2InventoryType destInv,
            ref short destSlot, bool forced = false)
        {
            var err = Asda2InventoryError.Ok;
            Asda2Item srcItem = null;
            Asda2Item destItem = null;
            if (srcInv == Asda2InventoryType.Equipment)
            {
                destSlot = FindFreeShopItemsSlot();
                if (destSlot == -1) err = Asda2InventoryError.NoSpace;
            }
            if (forced)
            {
                goto forcedlabel;
            }
            if ((srcInv == Asda2InventoryType.Regular && srcSlot == 0) ||
                (destInv == Asda2InventoryType.Regular && destSlot == 0))
                return Asda2InventoryError.Fail;

            if (srcInv != Asda2InventoryType.Shop && srcInv != Asda2InventoryType.Equipment &&
                srcInv != Asda2InventoryType.Warehouse && srcInv != Asda2InventoryType.AvatarWarehouse &&
                srcInv != Asda2InventoryType.Regular)
            {
                err = Asda2InventoryError.NotInfoAboutItem;
                Owner.YouAreFuckingCheater("Moving items from wrong inventory.", 50);
            }
            else if (srcInv == Asda2InventoryType.Regular && destInv != Asda2InventoryType.Regular &&
                     (destInv == Asda2InventoryType.Shop && destSlot != 10))
            {
                Owner.YouAreFuckingCheater("Moving items from regular to not regular inventory.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if (srcInv == Asda2InventoryType.Shop &&
                     (destInv != Asda2InventoryType.Shop && destInv != Asda2InventoryType.Equipment))
            {
                Owner.YouAreFuckingCheater("Moving items from shop to not shop/equipment inventory.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if (srcInv == Asda2InventoryType.Warehouse && destInv != Asda2InventoryType.Warehouse)
            {
                Owner.YouAreFuckingCheater("Moving items from wh to not wh inventory.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if (srcInv == Asda2InventoryType.AvatarWarehouse &&
                     destInv != Asda2InventoryType.AvatarWarehouse)
            {
                Owner.YouAreFuckingCheater("Moving items from awh to not awh inventory.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if (srcInv == Asda2InventoryType.Equipment &&
                     (destInv != Asda2InventoryType.Shop && destInv != Asda2InventoryType.Regular))
            {
                Owner.YouAreFuckingCheater("Moving items from equipment to not shop inventory.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if ((srcInv == Asda2InventoryType.Shop && (srcSlot < 0 || srcSlot >= ShopItems.Length)) ||
                     (srcInv == Asda2InventoryType.Regular &&
                      (srcSlot < 0 || srcSlot >= RegularItems.Length)) ||
                     (srcInv == Asda2InventoryType.Equipment &&
                      (srcSlot < 0 || srcSlot >= Equipment.Length)) ||
                     (srcInv == Asda2InventoryType.Warehouse &&
                      (srcSlot < 0 || srcSlot >= WarehouseItems.Length)) ||
                     (srcInv == Asda2InventoryType.AvatarWarehouse &&
                      (srcSlot < 0 || srcSlot >= AvatarWarehouseItems.Length)))
            {
                Owner.YouAreFuckingCheater("Moving items from wrong slot.", 50);
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if ((destInv == Asda2InventoryType.Shop &&
                      (destSlot < 0 || destSlot >= (Owner.InventoryExpanded ? ShopItems.Length : 30))) ||
                     (destInv == Asda2InventoryType.Regular &&
                      (destSlot < 0 || destSlot >= RegularItems.Length)) ||
                     (destInv == Asda2InventoryType.Equipment &&
                      (destSlot < 0 || destSlot >= Equipment.Length)) ||
                     (destInv == Asda2InventoryType.Warehouse &&
                      (destSlot < 0 || destSlot >= WarehouseItems.Length)) ||
                     (destInv == Asda2InventoryType.AvatarWarehouse &&
                      (destSlot < 0 || destSlot >= AvatarWarehouseItems.Length)))
            {
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if ((destInv == Asda2InventoryType.Regular) && (destSlot == 0))
            //gold slot cant swaped
            {
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            else if ((srcInv == Asda2InventoryType.Regular) && (srcSlot == 0))
            //gold slot cant swaped
            {
                err = Asda2InventoryError.NotInfoAboutItem;
            }
            if (err != Asda2InventoryError.Ok)
                if (err != Asda2InventoryError.Ok)
                {
                    Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, err);
                    return err;
                }
        forcedlabel:
            switch (srcInv)
            {
                case Asda2InventoryType.Shop:
                    srcItem = ShopItems[srcSlot];
                    break;
                case Asda2InventoryType.Regular:
                    srcItem = RegularItems[srcSlot];
                    break;
                case Asda2InventoryType.Equipment:
                    srcItem = Equipment[srcSlot];
                    break;
                case Asda2InventoryType.Warehouse:
                    srcItem = WarehouseItems[srcSlot];
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    srcItem = AvatarWarehouseItems[srcSlot];
                    break;
            }
            if (srcItem.Template.RequiredProfession != Owner.Profession && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.Weapon && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.Ammo && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.AvatarHead && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.Nackles && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.RightRing && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.LeftRing && srcItem.Template.EquipmentSlot != Asda2EquipmentSlots.Shild
                && destInv == Asda2InventoryType.Equipment)
            {
                err = Asda2InventoryError.Invalid;
                Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, err);
                return err;
            }
            if (srcItem == null)
                err = Asda2InventoryError.NotInfoAboutItem;
            else if (!m_owner.CanInteract)
                err = Asda2InventoryError.NotInfoAboutItem;
            else
            {
                switch (destInv)
                {
                    case Asda2InventoryType.Shop:
                        destItem = ShopItems[destSlot];
                        break;
                    case Asda2InventoryType.Regular:
                        destItem = RegularItems[destSlot];
                        break;
                    case Asda2InventoryType.Equipment:
                        destItem = Equipment[destSlot];
                        break;
                    case Asda2InventoryType.Warehouse:
                        destItem = WarehouseItems[destSlot];
                        break;
                    case Asda2InventoryType.AvatarWarehouse:
                        destItem = AvatarWarehouseItems[destSlot];
                        break;
                }
            }
            if (err != Asda2InventoryError.Ok)
            {
                Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, err);
                return err;
            }
            if (destInv == Asda2InventoryType.Equipment && destSlot == (int)Asda2EquipmentSlots.Weapon)
            {
                if (srcItem != null && srcItem.Template.Category != Asda2ItemCategory.OneHandedSword &&
                    Equipment[(int)Asda2EquipmentSlots.Shild] != null)
                {
                    Owner.SendInfoMsg("You cant use this item with shield.");
                    return Asda2InventoryError.ItemIsNotForEquiping;
                }
            }
            if (destInv == Asda2InventoryType.Equipment && srcItem != null)
            {
                if (srcItem.Template.EquipmentSlot == Asda2EquipmentSlots.LeftRing && destSlot == 6) //rightring
                {
                }
                else
                {
                    if (srcItem.Template.EquipmentSlot != (Asda2EquipmentSlots)destSlot)
                    {
                        Owner.SendInfoMsg("This item is not for equiping.");
                        return Asda2InventoryError.ItemIsNotForEquiping;
                    }
                }
                if (srcItem.RequiredLevel > Owner.Level)
                {
                    Owner.SendInfoMsg("Your's level is not enogth.");
                    return Asda2InventoryError.Fail;
                }
            }
            if (destItem != null && srcInv == Asda2InventoryType.Equipment)
            {
                switch (destInv)
                {
                    case Asda2InventoryType.Shop:
                        destItem = null;
                        short freeSlot = FindFreeShopItemsSlot();
                        if (freeSlot == -1)
                            err = Asda2InventoryError.NoSpace;
                        else
                        {
                            destSlot = freeSlot;
                        }
                        break;
                    case Asda2InventoryType.Regular:
                        destItem = null;
                        freeSlot = FindFreeRegularItemsSlot();
                        if (freeSlot == -1)
                            err = Asda2InventoryError.NoSpace;
                        else
                        {
                            destSlot = freeSlot;
                        }
                        break;
                }
            }
            if (err != Asda2InventoryError.Ok)
            {
                Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, err);
            }
            else
            {
                SwapUnchecked(srcInv, srcSlot, destInv, destSlot);
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "swap")
                    .AddAttribute("srcInv", (double)srcInv, srcInv.ToString())
                    .AddAttribute("destInv", (double)destInv, destInv.ToString())
                    .AddAttribute("srcSlot", srcSlot)
                    .AddAttribute("destSlot", destSlot)
                    .AddItemAttributes(srcItem, "srcItem")
                    .AddItemAttributes(destItem, "destItem")
                    .Write();
                Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, err, srcSlot, (byte)srcInv,
                    srcItem == null ? -1 : srcItem.Amount, (short)(srcItem == null ? 0 : srcItem.Weight), destSlot,
                    (byte)destInv,
                    destItem == null ? -1 : destItem.Amount, (short)(destItem == null ? 0 : srcItem.Weight),
                    destItem == null);
            }

            return err;
        }

        private void SwapUnchecked(Asda2InventoryType srcInv, short srcSlot, Asda2InventoryType destInv, short destSlot)
        {
            Asda2Item srcItem = null;
            Asda2Item destItem = null;
            switch (srcInv)
            {
                case Asda2InventoryType.Shop:
                    srcItem = ShopItems[srcSlot];
                    break;
                case Asda2InventoryType.Regular:
                    srcItem = RegularItems[srcSlot];
                    break;
                case Asda2InventoryType.Equipment:
                    srcItem = Equipment[srcSlot];

                    break;
                case Asda2InventoryType.Warehouse:
                    srcItem = WarehouseItems[srcSlot];
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    srcItem = AvatarWarehouseItems[srcSlot];
                    break;
            }
            switch (destInv)
            {
                case Asda2InventoryType.Shop:
                    destItem = ShopItems[destSlot];
                    break;
                case Asda2InventoryType.Regular:
                    destItem = RegularItems[destSlot];
                    break;
                case Asda2InventoryType.Equipment:
                    destItem = Equipment[destSlot];
                    break;
                case Asda2InventoryType.Warehouse:
                    destItem = WarehouseItems[destSlot];
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    destItem = AvatarWarehouseItems[destSlot];
                    break;
            }
            switch (srcInv)
            {
                case Asda2InventoryType.Shop:
                    SetShopInventoty(destItem, srcSlot, true);
                    break;
                case Asda2InventoryType.Regular:
                    SetRegularInventoty(destItem, srcSlot, true);
                    break;
                case Asda2InventoryType.Equipment:
                    SetEquipment(destItem, (Asda2EquipmentSlots)srcSlot);
                    break;
                case Asda2InventoryType.Warehouse:
                    SetWarehouseInventoty(destItem, srcSlot, true);
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    SetAvatarWarehouseInventoty(destItem, srcSlot, true);
                    break;
            }
            switch (destInv)
            {
                case Asda2InventoryType.Shop:
                    SetShopInventoty(srcItem, destSlot, true);
                    break;
                case Asda2InventoryType.Regular:
                    SetRegularInventoty(srcItem, destSlot, true);
                    break;
                case Asda2InventoryType.Equipment:
                    SetEquipment(srcItem, (Asda2EquipmentSlots)destSlot);
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    SetAvatarWarehouseInventoty(srcItem, destSlot, true);
                    break;
                case Asda2InventoryType.Warehouse:
                    SetWarehouseInventoty(srcItem, destSlot, true);
                    break;
            }
        }

        public void RemoveItemFromInventory(Asda2Item asda2Item)
        {
            if (asda2Item.IsDeleted)
            {
                Owner.SendErrorMsg(string.Format("Cant remove deleted item from inventory. inv {0}.slot {1}. itemId {2}"
                    , asda2Item.InventoryType, asda2Item.Slot, asda2Item.ItemId));
                return;
            }
            if (asda2Item.Slot < 0)
            {
                Owner.SendErrorMsg(
                    string.Format("Cant remove item from inventory with slot < 0. inv {0}.slot {1}. itemId {2}"
                        , asda2Item.InventoryType, asda2Item.Slot, asda2Item.ItemId));
                return;
            }

            switch (asda2Item.InventoryType)
            {
                case Asda2InventoryType.Regular:
                    SetRegularInventoty(null, asda2Item.Slot, true);
                    break;
                case Asda2InventoryType.Shop:
                    SetShopInventoty(null, asda2Item.Slot, true);
                    break;
                case Asda2InventoryType.Equipment:
                    SetEquipment(null, (Asda2EquipmentSlots)asda2Item.Slot);
                    break;
                case Asda2InventoryType.Warehouse:
                    SetWarehouseInventoty(null, asda2Item.Slot, true);
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    SetAvatarWarehouseInventoty(null, asda2Item.Slot, true);
                    break;
            }
        }

        public Asda2InventoryError TryAdd(int itemId, int amount, bool silent, ref Asda2Item item, Asda2InventoryType? requiredInventoryType = null, Asda2Item itemToCopyStats = null)
        {
            Asda2ItemTemplate itemTemplate = Asda2ItemMgr.GetTemplate(itemId);
            if (itemTemplate == null)
            {
                Owner.SendErrorMsg(string.Format("Failed to create and add item {0}. template not founed", itemId));
                return Asda2InventoryError.Fail;
            }
            Asda2InventoryType inventoryType = requiredInventoryType != Asda2InventoryType.Quests ? CalcIntentoryTypeToAdd(requiredInventoryType, itemTemplate) : Asda2InventoryType.Quests;
            short freeSlot = FindFreeSlot(inventoryType);
            if (freeSlot < 0)
            {
                Owner.SendErrorMsg(string.Format("Failed to create and add item {0}. not enough space", itemId));
                return Asda2InventoryError.NoSpace;
            }
            if (itemTemplate.IsStackable)
            {
                item = FindItem(itemTemplate, inventoryType);
                if (item != null)
                {
                    item.Amount += amount;
                    return Asda2InventoryError.Ok;
                }
            }

            item = Asda2Item.CreateItem(itemTemplate, Owner, amount);
            if (itemToCopyStats != null && itemToCopyStats.Record != null)
            {
                item.Enchant = itemToCopyStats.Enchant;
                item.Soul1Id = itemToCopyStats.Soul1Id;
                item.Soul2Id = itemToCopyStats.Soul2Id;
                item.Soul3Id = itemToCopyStats.Soul3Id;
                item.Soul4Id = itemToCopyStats.Soul4Id;
                item.Parametr1Type = itemToCopyStats.Parametr1Type;
                item.Parametr1Value = itemToCopyStats.Parametr1Value;
                item.Parametr2Type = itemToCopyStats.Parametr2Type;
                item.Parametr2Value = itemToCopyStats.Parametr2Value;
                item.Parametr3Type = itemToCopyStats.Parametr3Type;
                item.Parametr3Value = itemToCopyStats.Parametr3Value;
                item.Parametr4Type = itemToCopyStats.Parametr4Type;
                item.Parametr4Value = itemToCopyStats.Parametr4Value;
                item.Parametr5Type = itemToCopyStats.Parametr5Type;
                item.Parametr5Value = itemToCopyStats.Parametr5Value;
                item.Durability = itemToCopyStats.Durability;
                item.EnchantChances = itemToCopyStats.EnchantChances;
                item.EnchantCount = itemToCopyStats.EnchantCount;
                item.EnchantLevel = itemToCopyStats.EnchantLevel;
                item.IsSoulbound = itemToCopyStats.IsSoulbound;
            }
            SetItem(item, freeSlot, inventoryType);
            Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);
            return Asda2InventoryError.Ok;
        }

        private short FindFreeSlot(Asda2InventoryType inventoryType)
        {
            short freeSlot;
            switch (inventoryType)
            {
                case Asda2InventoryType.Regular:
                    freeSlot = FindFreeRegularItemsSlot();
                    break;
                case Asda2InventoryType.Shop:
                    freeSlot = FindFreeShopItemsSlot();
                    break;
                case Asda2InventoryType.Warehouse:
                    freeSlot = FindFreeWarehouseItemsSlot();
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    freeSlot = FindFreeAvatarWarehouseItemsSlot();
                    break;
                case Asda2InventoryType.Quests:
                    freeSlot = FindFreeQuestsItemSlot();
                    break;

                default:
                    Owner.SendErrorMsg(string.Format("wrong inventory type {0}", inventoryType));
                    freeSlot = -1;
                    break;
            }
            return freeSlot;
        }

        private static Asda2InventoryType CalcIntentoryTypeToAdd(Asda2InventoryType? requiredInventoryType,
            Asda2ItemTemplate itemTemplate)
        {
            Asda2InventoryType inventory;
            if (!requiredInventoryType.HasValue)
            {
                inventory = itemTemplate.IsShopInventoryItem ? Asda2InventoryType.Shop : Asda2InventoryType.Regular;
            }
            else
            {
                inventory = requiredInventoryType.Value;
            }
            return inventory;
        }

        public void UseItem(Asda2InventoryType inv, byte slot)
        {
            Asda2Item item;
            switch (inv)
            {
                case Asda2InventoryType.Regular:
                    if (slot >= RegularItems.Length)
                    {
                        Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                        return;
                    }
                    item = RegularItems[slot];
                    break;
                case Asda2InventoryType.Shop:
                    if (slot >= ShopItems.Length)
                    {
                        Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                        return;
                    }
                    item = ShopItems[slot];
                    break;
                default:
                    Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                    return;
            }
            if (item == null)
            {
                Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                return;
            }
            if (item.RequiredLevel > Owner.Level)
            {
                Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.CantUseBacauseOfItemLevel, Owner, null);
                return;
            }
            if (!CheckCooldown(item))
            {
                Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.ItemOnCooldown, Owner, null);
                return;
            }
            if (Owner.IsDead)
            {
                Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                return;
            }
            if (Owner.IsTrading)
            {
                Asda2InventoryHandler.SendCharUsedItemResponse(UseItemResult.Fail, Owner, null);
                return;
            }
            Owner.AddMessage(() => Asda2InventoryHandler.SendCharUsedItemResponse(UseItemUnchecked(item), Owner, item));
        }

        private UseItemResult UseItemUnchecked(Asda2Item item)
        {
            switch (item.Category)
            {
                case Asda2ItemCategory.HealthElixir:
                    Owner.Health +=
                        (int)(item.Template.ValueOnUse * CharacterFormulas.CalcHpPotionBoost(Owner.Asda2Stamina));
                    break;
                case Asda2ItemCategory.HealthPotion:
                    PereodicAction a = null;
                    if (Owner.PereodicActions.ContainsKey(Asda2PereodicActionType.HpRegenPrc))
                        a = Owner.PereodicActions[Asda2PereodicActionType.HpRegenPrc];
                    if (a != null && a.CallsNum >= 10 && a.Value >= item.Template.ValueOnUse)
                        return UseItemResult.ItemOnCooldown;
                    if (Owner.PereodicActions.ContainsKey(Asda2PereodicActionType.HpRegenPrc))
                        Owner.PereodicActions.Remove(Asda2PereodicActionType.HpRegenPrc);
                    a = new PereodicAction(Owner,
                        (int)(item.Template.ValueOnUse * CharacterFormulas.CalcHpPotionBoost(Owner.Asda2Stamina)), 10,
                        3000, Asda2PereodicActionType.HpRegenPrc);
                    Owner.PereodicActions.Add(Asda2PereodicActionType.HpRegenPrc, a);
                    Owner.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)33, 50, 100);

                    break;
                case Asda2ItemCategory.ManaElixir:
                    Owner.Power += item.Template.ValueOnUse;
                    break;
                case Asda2ItemCategory.ManaPotion:
                    a = null;
                    if (Owner.PereodicActions.ContainsKey(Asda2PereodicActionType.MpRegen))
                        a = Owner.PereodicActions[Asda2PereodicActionType.MpRegen];
                    if (a != null && a.CallsNum >= 10 && a.Value >= item.Template.ValueOnUse)
                        return UseItemResult.ItemOnCooldown;
                    if (Owner.PereodicActions.ContainsKey(Asda2PereodicActionType.MpRegen))
                        Owner.PereodicActions.Remove(Asda2PereodicActionType.MpRegen);
                    a = new PereodicAction(Owner, item.Template.ValueOnUse, 10, 3000, Asda2PereodicActionType.MpRegen);
                    Owner.PereodicActions.Add(Asda2PereodicActionType.MpRegen, a);
                    break;
                case Asda2ItemCategory.ReturnScroll:
                    if (Owner.IsInCombat || Owner.IsAsda2BattlegroundInProgress)
                        return UseItemResult.Fail;
                    Owner.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)14, 300, 3000);
                    Owner.TeleportToBindLocation();
                    break;
                case Asda2ItemCategory.PetResurect:
                    return UseItemResult.Fail;
                case Asda2ItemCategory.ResurectScroll:
                    if (!(Owner.Target is Character))
                    {
                        Owner.SendSystemMessage("Select character to resurect.", item.Category);
                        return UseItemResult.Fail;
                    }
                    var target = (Character)Owner.Target;
                    if (target.IsAlive)
                    {
                        Owner.SendSystemMessage("Select character is alive and can't be resurected.", item.Category);
                        return UseItemResult.Fail;
                    }
                    target.Resurrect();
                    Owner.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)32, 300, 3000);

                    break;
                case Asda2ItemCategory.Fish:
                    Owner.Power += item.Template.ValueOnUse;
                    break;
                case Asda2ItemCategory.FishingBook:
                    Owner.SendSystemMessage("Using {0} is not implemented yet.", item.Category);
                    break;
                case Asda2ItemCategory.Incubator:
                    Owner.SendSystemMessage("Using {0} is not implemented yet.", item.Category);
                    break;
                case Asda2ItemCategory.ItemPackage:
                    Owner.SendSystemMessage("Using {0} is not implemented yet.", item.Category);
                    break;
                case Asda2ItemCategory.PartialItem:
                    Owner.SendSystemMessage("Using {0} is not implemented yet.", item.Category);
                    break;
                case Asda2ItemCategory.PetExp:
                    if (Owner.Asda2Pet == null)
                        return UseItemResult.ThereIsNoActivePet;
                    if (!Owner.Asda2Pet.GainXp(item.Template.ValueOnUse / 2)) return UseItemResult.PetIsMature;
                    break;
                case Asda2ItemCategory.Recipe:
                    Owner.YouAreFuckingCheater("Trying to use recipe in wrong way.", 50);
                    return UseItemResult.Fail;

                case Asda2ItemCategory.SoulStone:
                    if (Owner.IsSoulmateSafeHandsEnabled || Owner.SoulmateCharacter == null)
                    {
                        item.SoulStoneExp = Owner.Experience / 100;
                    }
                    Owner.SoulStoneExperience += item.SoulStoneExp;
                    if (!Owner.IsSoulmateSafeHandsEnabled || Owner.SoulmateCharacter != null)
                    {
                        Owner.SoulmateCharacter.Experience += Owner.SoulStoneExperience;
                        item.Destroy();
                    }
                    break;
                case Asda2ItemCategory.SoulShard:
                    Owner.SendSystemMessage("Using {0} is not implemented yet.", item.Category);
                    break;
                case Asda2ItemCategory.Showel:
                    //item.
                    var showel = item;
                    Owner.MainWeapon = showel;
                    Owner.IsDigging = true;
                    Owner.Map.CallDelayed(2000, () => Asda2CharacterHandler.SendEmoteResponse(Owner, 110, 0, 0, 0));
                    Owner.Map.CallDelayed(6000, () => Asda2DigMgr.ProcessDig(Owner.Client));
                    Asda2CharacterHandler.SendEmoteResponse(Owner, 110);
                    Asda2DiggingHandler.SendStartDigResponseResponse(Owner.Client, Asda2DigResult.Ok, showel);

                    break;
                default:
                    Owner.SendSystemMessage(string.Format("Item {0} from category {1} can't be used.",
                        item.Template.Name, item.Category));
                    return UseItemResult.Fail;
            }
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "use")
                .AddItemAttributes(item)
                .Write();
            if (item.Category != Asda2ItemCategory.Showel)
                item.Amount--;
            return UseItemResult.Ok;
        }

        private bool CheckCooldown(Asda2Item item)
        {
            if (!_cooldowns.ContainsKey(item.Template.Category))
                _cooldowns.Add(item.Template.Category, DateTime.Now.AddSeconds(30));
            else
            {
                if (_cooldowns[item.Template.Category] > DateTime.Now)
                    return false;
                _cooldowns[item.Template.Category] = DateTime.Now.AddSeconds(30);
            }
            return true;
        }

        public void RemoveItem(int slot, byte inv, int count)
        {
            if (count == 0) count = 1;
            if (inv != 1 && inv != 2 && inv != 4 || slot < 0 || slot >= 70)
            {
                Owner.SendInfoMsg(string.Format("Failed to removeItem from inventory. slot is {0}. inv is {1}", slot,
                    inv));
                Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, null, DeleteOrSellItemStatus.Fail);
                return;
            }
            Asda2Item item;
            switch ((Asda2InventoryType)inv)
            {
                case Asda2InventoryType.Regular:
                    if (slot >= RegularItems.Length)
                    {
                        Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, null, DeleteOrSellItemStatus.Fail);
                        Owner.SendInfoMsg(string.Format("Failed to removeItem from inventory. slot is {0}. inv is {1}",
                            slot, inv));
                        return;
                    }
                    item = RegularItems[slot];
                    break;
                case Asda2InventoryType.Shop:
                    if (slot >= ShopItems.Length)
                    {
                        Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, null, DeleteOrSellItemStatus.Fail);
                        Owner.SendInfoMsg(string.Format("Failed to removeItem from inventory. slot is {0}. inv is {1}",
                            slot, inv));
                        return;
                    }
                    item = ShopItems[slot];
                    break;
                default:
                    Owner.SendInfoMsg(string.Format("Failed to removeItem from inventory. slot is {0}. inv is {1}", slot,
                        inv));
                    Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, null, DeleteOrSellItemStatus.Fail);
                    return;
            }
            if (item == null || item.ItemId == 20551)
            {
                Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, null, DeleteOrSellItemStatus.Fail);
                Owner.SendInfoMsg(string.Format("Failed to removeItem from inventory item not found or money. slot is {0}. inv is {1}", slot,
                    inv));
                return;
            }
            if (slot != 60)
            {
                Owner.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)15, 1000, 3000);
            }
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "remove_from_inventory")
                .AddItemAttributes(item)
                .AddAttribute("amount", count)
                .Write();
            if (count <= 0)
                item.Destroy();
            else
            {
                item.Amount -= item.Amount < count ? item.Amount : count;
            }
            Asda2InventoryHandler.ItemRemovedFromInventoryResponse(Owner, item, DeleteOrSellItemStatus.Ok);
        }

        public void SellItems(ItemStub[] itemStubs)
        {
            var items = new List<Asda2Item>(5);
            foreach (ItemStub itemStub in itemStubs)
            {
                switch (itemStub.Inv)
                {
                    case Asda2InventoryType.Regular:
                        if (itemStub.Cell >= RegularItems.Length || itemStub.Cell < 0)
                        {
                            items.Add(null);
                            continue;
                        }
                        Asda2Item item = RegularItems[itemStub.Cell];
                        if (item != null)
                            item.CountForNextSell = itemStub.Amount;
                        items.Add(item);
                        break;
                    case Asda2InventoryType.Shop:
                        if (itemStub.Cell >= ShopItems.Length || itemStub.Cell < 0)
                        {
                            items.Add(null);
                            continue;
                        }
                        items.Add(ShopItems[itemStub.Cell]);
                        break;
                    default:
                        items.Add(null);
                        break;
                }
            }
            long goldEarned = 0;
            foreach (Asda2Item asda2Item in items)
            {
                if (asda2Item == null)
                    continue;
                int amountToSell;
                if (asda2Item.Template.IsStackable)
                {
                    if (asda2Item.CountForNextSell <= 0)
                        amountToSell = asda2Item.Amount;
                    else
                        amountToSell = asda2Item.Amount < asda2Item.CountForNextSell
                            ? asda2Item.Amount
                            : asda2Item.CountForNextSell;
                }
                else
                {
                    amountToSell = 1;
                }
                float stackedItemsMultiplier = 1f;
                if (asda2Item.Template.MaxAmount > 1)
                    stackedItemsMultiplier = stackedItemsMultiplier / asda2Item.Template.MaxAmount;
                var gold = (int)(asda2Item.Template.SellPrice * amountToSell *
                                  (1 + Owner.FloatMods[(int)StatModifierFloat.SellingCost]) * stackedItemsMultiplier);
                goldEarned += gold;
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "selling_to_regular_shop")
                    .AddItemAttributes(asda2Item)
                    .AddAttribute("amount_to_sell", amountToSell)
                    .AddAttribute("gold_earned", gold)
                    .Write();
                asda2Item.Amount -= amountToSell;
            }
            if (goldEarned > int.MaxValue || goldEarned < 0)
            {
                Owner.YouAreFuckingCheater("Wrong total gold amount while selling items.", 20);
                Asda2InventoryHandler.SendSellItemResponseResponse(DeleteOrSellItemStatus.Fail, Owner, items);
            }
            else
            {
                Owner.AddMoney((uint)goldEarned);
                Asda2InventoryHandler.SendSellItemResponseResponse(DeleteOrSellItemStatus.Ok, Owner, items);
                Owner.SendMoneyUpdate();
            }
        }

        public void BuyItems(List<ItemStub> itemStubs)
        {
            var items = new Asda2Item[5];
            var templates = new List<Asda2ItemTemplate>(5);
            foreach (ItemStub itemStub in itemStubs)
            {
                if (itemStub.ItemId == 0)
                {
                    templates.Add(null);
                    continue;
                }
                Asda2ItemTemplate t = Asda2ItemMgr.GetTemplate(itemStub.ItemId);
                if (t == null)
                {
                    Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.BadItemId, Owner, items);
                    Owner.YouAreFuckingCheater(string.Format("Trying to buy bad item with id {0}.", itemStub.ItemId), 20);
                    return;

                }
                if (!t.CanBuyInRegularShop && (t.Category != Asda2ItemCategory.Recipe/* || !(itemStub.ItemId >= 66618 && itemStub.ItemId <= 66676)*/))
                {
                    Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.BadItemId, Owner, items);
                    Owner.YouAreFuckingCheater(string.Format("Trying to buy bad item with id {0}.", itemStub.ItemId), 20);
                    return;
                }
                
                if ((t.IsStackable && itemStub.Amount <= 0) || (!t.IsStackable && itemStub.Amount != 1))
                    itemStub.Amount = 1;
                templates.Add(t);
            }
            if (
                !CheckFreeRegularItemsSlots(
                    templates.Count(t => t != null && t.InventoryType == (byte)Asda2InventoryType.Regular)) ||
                !CheckShopItemsSlots(templates.Count(t => t != null && t.InventoryType == (byte)Asda2InventoryType.Shop)))
            {
                Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.NotEnoughSpace, Owner, items);
                return;
            }
            long price = CalculatePrice(templates, itemStubs);
            if (price < 0 || price >= int.MaxValue)
            {
                Owner.YouAreFuckingCheater("Wrong price while buying items", 20);
                Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.NotEnoughGold, Owner, items);
                return;
            }
            if (price >= Owner.Money)
            {
                Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.NotEnoughGold, Owner, items);
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                if (templates[i] == null)
                    continue;
                //TODO : To be chekced v
                int am =/* templates[i].MaxAmount == 0 ? templates[i].MaxAmount :*/ itemStubs[i].Amount;
                Asda2Item item = FindItem(templates[i]);
                if (item != null&&item.Template.IsStackable==true)
                    item.Amount += am;
                else
                {
                    Owner.SendErrorMsg("Amout: " + am);
                    item = Asda2Item.CreateItem(templates[i], Owner, am);

                    Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                        .AddAttribute("source", 0, "buying_from_regular_shop")
                        .AddItemAttributes(item)
                        .Write();

                    if (item.Template.IsShopInventoryItem)
                    {
                        short slot = FindFreeShopItemsSlot();
                        if (slot == -1)
                        {
                            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.NotEnoughSpace, Owner, items);
                            return;
                        }
                        SetShopInventoty(item, slot, true);
                    }
                    else
                    {
                        short slot = FindFreeRegularItemsSlot();
                        if (slot == -1)
                        {
                            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.NotEnoughSpace, Owner, items);
                            return;
                        }
                        SetRegularInventoty(item, slot, true);
                    }
                }
                items[i] = item;
            }
            Owner.SubtractMoney((uint)price);
            Owner.SendMoneyUpdate();
            Asda2InventoryHandler.SendBuyItemResponse(Asda2BuyItemStatus.Ok, Owner, items);
        }

        private Asda2Item FindItem(int itemId, Asda2InventoryType? requiredInventoryType = null)
        {
            Asda2ItemTemplate template = Asda2ItemMgr.GetTemplate(itemId);
            if (template == null)
            {
                Owner.SendErrorMsg(string.Format("failed ti find item .wrong item id {0}", itemId));
                return null;
            }
            return FindItem(template, requiredInventoryType);
        }

        private Asda2Item FindItem(Asda2ItemTemplate asda2ItemTemplate, Asda2InventoryType? requiredInventoryType = null)
        {
            Asda2InventoryType inventoryType = CalcIntentoryTypeToAdd(requiredInventoryType, asda2ItemTemplate);
            Asda2Item item;
            switch (inventoryType)
            {
                case Asda2InventoryType.Regular:
                    item = RegularItems.FirstOrDefault(i => i != null && i.ItemId == (int)asda2ItemTemplate.ItemId);
                    break;
                case Asda2InventoryType.Shop:
                    item = ShopItems.FirstOrDefault(i => i != null && i.ItemId == (int)asda2ItemTemplate.ItemId);
                    break;
                case Asda2InventoryType.Warehouse:
                    item = WarehouseItems.FirstOrDefault(i => i != null && i.ItemId == (int)asda2ItemTemplate.ItemId);
                    break;
                case Asda2InventoryType.AvatarWarehouse:
                    item =
                        AvatarWarehouseItems.FirstOrDefault(i => i != null && i.ItemId == (int)asda2ItemTemplate.ItemId);
                    break;
                case Asda2InventoryType.Quests:
                    item =
                        QuestItems.FirstOrDefault(i => i != null && i.ItemId == (int)asda2ItemTemplate.ItemId);
                    break;
                default:
                    Owner.SendErrorMsg(string.Format("failed ti find item .wrong inventory type {0}", inventoryType));
                    return null;
            }
            return item;
        }

        private long CalculatePrice(List<Asda2ItemTemplate> templates, List<ItemStub> itemStubs)
        {
            long totalPrice = 0;
            for (int i = 0; i < 5; i++)
            {
                Asda2ItemTemplate template = templates[i];
                if (template == null)
                    continue;
                totalPrice += template.BuyPrice * itemStubs[i].Amount;
            }
            return totalPrice;
        }

        private bool CheckShopItemsSlots(int count)
        {
            int cnt = ShopItems.Count(i => i == null);
            if (!Owner.InventoryExpanded)
                cnt -= 30;
            return cnt >= count;
        }

        private bool CheckFreeRegularItemsSlots(int count)
        {
            return RegularItems.Count(i => i == null) >= count;
        }

        public void TryPickUpItem(short x, short y)
        {
            Asda2LootItem lootItem = Owner.Map.TryPickUpItem(x, y);
            if (lootItem == null)
            {
                Asda2InventoryHandler.SendItemPickupedResponse(Asda2PickUpItemStatus.Fail, null, Owner);
                Owner.Map.AddMessage(() => GlobalHandler.SendRemoveItemResponse(Owner.Client, x, y));
                return;
            }
            if (lootItem.Loot.Looters != null && lootItem.Loot.Looters.Count > 0 &&
                lootItem.Loot.SpawnTime.AddSeconds(CharacterFormulas.ForeignLootPickupTimeout) > DateTime.Now &&
                lootItem.Loot.Looters.FirstOrDefault(l => l.Owner == Owner) == null)
            {
                Owner.SendInfoMsg("Stealer!!! It's not your loot!");
                Asda2InventoryHandler.SendItemPickupedResponse(Asda2PickUpItemStatus.Fail, null, Owner);
                Owner.UpdateTitleCounter((Constants.Achievements.Asda2TitleId)41, 50, 100);
                return;
            }
            Owner.Map.ClearLootSlot(x, y);

            Asda2Item item = null;
            Asda2InventoryError status = TryAdd((int)lootItem.Template.ItemId, lootItem.Amount, true, ref item);
            if (item != null)
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "from_loot")
                    .AddAttribute("mob_id", lootItem.Loot.MonstrId.HasValue ? (double)lootItem.Loot.MonstrId : 0)
                    .AddItemAttributes(item)
                    .Write();
            if (status != Asda2InventoryError.Ok)
            {
                Asda2InventoryHandler.SendItemPickupedResponse(Asda2PickUpItemStatus.NoSpace, null, Owner);
                return;
            }
            //Asda2QuestHandler.SendQuestsListResponse(Owner.Client, questRecord);
            Asda2InventoryHandler.SendItemPickupedResponse(Asda2PickUpItemStatus.Ok, item, Owner);
            Asda2TitleChecker.OnPickUp(Owner);
            Owner.Map.AddMessage(() => GlobalHandler.SendRemoveItemResponse(Owner ,x,y));
            if (lootItem.Loot.IsAllItemsTaken)
                lootItem.Loot.Dispose();

        }

        public void SowelItem(short itemCell, short sowelCell, byte sowelSlot)
        {
            Asda2Item item = ShopItems[itemCell];
            if (item == null)
            {
                Asda2InventoryHandler.SendItemSoweledResponse(Owner.Client, Weight, SowelingStatus.EquipmentError, null, null);
                return;
            }
            else if (item.SowelSlots < sowelSlot)
            {
                Asda2InventoryHandler.SendItemSoweledResponse(Owner.Client, Weight, SowelingStatus.MaxSocketSlotError, null, null);
                return;
            }
            Asda2Item sowel = RegularItems[sowelCell];
            if (sowel == null || sowel.Category != Asda2ItemCategory.Sowel)
            {
                Asda2InventoryHandler.SendItemSoweledResponse(Owner.Client, Weight, SowelingStatus.SowelError, null, null);
                return;
            }
            if (sowel.RequiredLevel > Owner.Level)
            {
                Asda2InventoryHandler.SendItemSoweledResponse(Owner.Client, Weight, SowelingStatus.LowLevel, null, null);
                return;
            }
            bool success = SowelItemUnchecked(item, sowel.ItemId, sowelSlot);
            if (success)
            {
                /*using (var packet = new RealmPacketOut(RealmServerOpCode.ItemReplaced)) //5001
                {
                    packet.WriteByte(1); //{status}default value : 1 Len : 1 0 - нет места . 1 - ok, 2 - недостаточно инфы о предмете - 3 - предмен не предназначен для ношения
                    packet.WriteByte(0); //value name : stab7 default value : stab7Len : 1
                    packet.WriteInt16(sowelCell); //{srcCell}default value : 6 Len : 2
                    packet.WriteInt16(-1); //value name : stab10 default value : stab10Len : 2
                    packet.WriteByte(2); //{srcInv}default value : 2 Len : 1
                    packet.WriteInt32(0); //{srcQuant}default value : 500 Len : 4
                    packet.WriteInt16(0); //{srcWeight}default value : 34 Len : 2
                    packet.WriteInt16(sowelCell); //{destCell}default value : 10 Len : 2
                    packet.WriteInt16(0); //value name : stab21 default value : stab21Len : 2
                    packet.WriteByte(2); //{destInv}default value : 3 Len : 1
                    packet.WriteInt32(0); //{destQuant}default value : 300 Len : 4
                    packet.WriteInt16(0); //{destWeight}default value : 23 Len : 2
                    Owner.Client.Send(packet, addEnd: true);
                }*/


                // sowel.Destroy();
                //RegularItems[sowelCell] = null;

                //item.Save();
            }
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("operation", 0, "sowel_item")
                .AddItemAttributes(item)
                .AddAttribute("success", success ? 1 : 0, success ? "yes" : "no")
                .Write();
            if (success)
                sowel.Amount = 0;
            //Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);
            //Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, sowel);
            Asda2InventoryHandler.SendItemSoweledResponse(Owner.Client, Weight, success ? SowelingStatus.Ok : SowelingStatus.Fail, item, sowel);

            Owner.Asda2Inventory.TrySwap(Asda2InventoryType.Regular, 0, Asda2InventoryType.Regular, ref sowelCell, true);
            short goldslot = 0;
            Owner.Asda2Inventory.TrySwap(Asda2InventoryType.Regular, sowelCell, Asda2InventoryType.Regular, ref goldslot, true);
        }

        private bool SowelItemUnchecked(Asda2Item item, int sowelId, byte sowelSlot)
        {
            const int sowelingChance = 100;
            if (sowelingChance > Utility.Random(0, 100))
            {
                switch (sowelSlot)
                {
                    case 0:
                        item.Soul1Id = sowelId;
                        break;
                    case 1:
                        item.Soul2Id = sowelId;
                        break;
                    case 2:
                        item.Soul3Id = sowelId;
                        break;
                    case 3:
                        item.Soul4Id = sowelId;
                        break;
                }
                return true;
            }
            return false;
        }

        public void ExchangeItemOptions(short scrollCell, short itemSlot)
        {
            if (scrollCell < 0 || scrollCell >= ShopItems.Length)
            {
                Owner.SendInfoMsg("Wrong scroll cell " + scrollCell);
                return;
            }
            if (itemSlot < 0 || itemSlot >= ShopItems.Length)
            {
                Owner.SendInfoMsg("Item scroll cell " + itemSlot);
                return;
            }
            Asda2Item scroll = ShopItems[scrollCell];
            if (scroll == null)
            {
                Asda2InventoryHandler.SendExchangeItemOptionResultResponse(Owner.Client,
                    ExchangeOptionResult.ScrollInvalid, null,
                    null);
                return;
            }
            Asda2Item item = ShopItems[itemSlot];
            if (item == null || !item.Template.IsEquipment)
            {
                Asda2InventoryHandler.SendExchangeItemOptionResultResponse(Owner.Client,
                    ExchangeOptionResult.ItemInvalid, null, null);
                return;
            }
            scroll.Amount--;
            Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, scroll);
            LogHelperEntry logScrollDelete =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("operation", 1, "exchange_options_scroll_delete")
                    .AddItemAttributes(scroll)
                    .Write();

            item.GenerateNewOptions();
            Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);
            //item.Save();
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("operation", 0, "exchange_options")
                .AddItemAttributes(item)
                .AddReference(logScrollDelete)
                .Write();
            Asda2InventoryHandler.SendExchangeItemOptionResultResponse(Owner.Client, ExchangeOptionResult.Ok, item,
                scroll);
        }
        private static Random ran = new Random();
        public void UpgradeItem(short itemCell, short stoneCell, short chanceBoostCell, short protectScrollCell)
        {
            Asda2Item item = ShopItems[itemCell];
            Asda2Item stone = RegularItems[stoneCell];
            if (item == null || stone == null)
            {
                Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.UpgradeFail, null, null, null,
                    null, Weight, Owner.Money);
                return;
            }
            bool canUseThisStone = CalcCanUseThisStone(item, stone);
            if (!canUseThisStone)
            {
                Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.YouDidntUseMinmumResets, null, null, null,
                    null, Weight, Owner.Money);
                return;
            }
            var enchPrice = (uint)0;
            if (!Owner.SubtractMoney(enchPrice))
            {
                Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.UpgradeFail, null, null, null,
                    null, Weight, Owner.Money);
                Owner.SendInfoMsg("Not enought money to enchant.");
                return;
            }
            Asda2Item chanceBoostItem = protectScrollCell == -1 ? null : ShopItems[protectScrollCell];
            Asda2Item protectItem = chanceBoostCell == -1 ? null : ShopItems[chanceBoostCell];
            int useChanceBoost = (chanceBoostItem != null &&
                                  chanceBoostItem.Category == Asda2ItemCategory.IncreaceUpgredeChance)
                ? chanceBoostItem.Template.ValueOnUse
                : 0;
            bool useProtect = false;
            bool noEnchantLose = false;
            ItemUpgradeResult result = CharacterFormulas.CalculateItemUpgradeResult(stone.Template.Quality,
                item.Template.Quality, item.Enchant, item.RequiredLevel, Owner.Asda2Luck,
                0, 0,
                useProtect, useChanceBoost, noEnchantLose);
            if (ran.Next(100) > 50)
                result.Status = ItemUpgradeResultStatus.Success;
            else 
                result.Status = ItemUpgradeResultStatus.Fail;
                
            if (Owner.GodMode)
                result.Status = ItemUpgradeResultStatus.Success;
            if (result.Status != ItemUpgradeResultStatus.Success)
            {
                int itemLevelToBreak = 0;
                switch (item.Template.Quality)
                {
                    case Asda2ItemQuality.White:
                        itemLevelToBreak = 5;
                        break;
                    case Asda2ItemQuality.Yello:
                        itemLevelToBreak = 6;
                        break;
                    case Asda2ItemQuality.Purple:
                        itemLevelToBreak = 7;
                        break;
                    case Asda2ItemQuality.Green:
                        itemLevelToBreak = 8;
                        break;
                    case Asda2ItemQuality.Orange:
                        itemLevelToBreak = 9;
                        break;
                    default:
                        itemLevelToBreak = 5;
                        break;
                }

                if (item.EnchantCount >= itemLevelToBreak)
                {
                    if (item.Template.Weight > 2)
                    {
                        result.Status = ItemUpgradeResultStatus.BreakItem;
                    }
                    else
                        result.Status = ItemUpgradeResultStatus.ReduceLevelToZero;
                }
                Asda2TitleChecker.OnUpgradeFail(Owner, item);
            }
            Console.WriteLine(result.Status.ToString());
            LogHelperEntry logMoneyDelete =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("operation", 1, "enchant_remove_money")
                    .AddAttribute("difference_money", enchPrice)
                    .AddAttribute("total_money", Owner.Money)
                    .Write();

            LogHelperEntry logStoneDelete =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("operation", 1, "enchant_remove_stone")
                    .AddItemAttributes(stone)
                    .Write();
            stone.Amount--;
            Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, stone);
            LogHelperEntry logProtectDelete = null;
            LogHelperEntry logChanceBoostDelete = null;
            if (protectItem != null)
            {
                logProtectDelete =
                    Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                        .AddAttribute("operation", 1, "enchant_remove_protect")
                        .AddItemAttributes(protectItem)
                        .Write();
                protectItem.Amount--;
                Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, protectItem);
            }
            if (chanceBoostItem != null)
            {
                logChanceBoostDelete =
                    Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                        .AddAttribute("operation", 1, "enchant_remove_chance_boost")
                        .AddItemAttributes(chanceBoostItem)
                        .Write();
                chanceBoostItem.Amount--;
                Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, chanceBoostItem);
            }

            switch (result.Status)
            {
                case ItemUpgradeResultStatus.ReduceLevelToZero:
                    item.Enchant = 0;
                    item.EnchantChances = item.Template.EnchantChances;
                    item.EnchantCount = 0;
                    item.EnchantLevel = 0;
                    item.FailedEnchants = 0;
                    Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.UpgradeFail, item, stone,
                        chanceBoostItem, protectItem, Weight, Owner.Money);
                    break;
                case ItemUpgradeResultStatus.BreakItem:
                    item.Destroy();
                    //Owner.Asda2Inventory.RemoveItemFromInventory(item);
                    Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);


                    Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.Break, item, stone,
                        chanceBoostItem, protectItem, Weight, Owner.Money);
                    break;
                case ItemUpgradeResultStatus.Fail:
                    Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                        .AddAttribute("operation", 0, "enchant_break_item")
                        .AddAttribute("chance", result.Chance)
                        .AddReference(logProtectDelete)
                        .AddReference(logStoneDelete)
                        .AddReference(logChanceBoostDelete)
                        .AddReference(logMoneyDelete)
                        .AddItemAttributes(item)
                        .Write();

                    --item.EnchantChances;
                    ++item.FailedEnchants;
                    ++item.EnchantCount;
                    Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.Break, item, stone,
                        chanceBoostItem, protectItem, Weight, Owner.Money);
                    break;
                case ItemUpgradeResultStatus.Success:
                    ++item.Enchant;
                    --item.EnchantChances;
                    ++item.EnchantCount;
                    switch (stone.Template.Quality)
                    {
                        case Asda2ItemQuality.White:
                            if (protectItem != null)
                            {
                                item.EnchantLevel += 8;
                            }
                            else
                            {
                                item.EnchantLevel += 3;
                            }
                            break;
                        case Asda2ItemQuality.Yello:
                            if (protectItem != null)
                            {
                                item.EnchantLevel += 11;
                            }
                            else
                            {
                                item.EnchantLevel += 6;
                            }
                            break;
                        case Asda2ItemQuality.Purple:
                            if (protectItem != null)
                            {
                                item.EnchantLevel += 15;
                            }
                            else
                            {
                                item.EnchantLevel += 10;
                            }
                            break;
                        default:
                            ++item.EnchantLevel;
                            break;
                    }
                    Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);
                    Asda2InventoryHandler.SendUpgradeItemResponse(Owner.Client, UpgradeItemStatus.Ok, item, stone,
                        chanceBoostItem, protectItem, Weight, Owner.Money);
                    Asda2TitleChecker.OnUpgradeSucccess(Owner, item);
                    Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                        .AddAttribute("operation", 0, "enchant_success")
                        .AddAttribute("chance", result.Chance)
                        .AddReference(logProtectDelete)
                        .AddReference(logStoneDelete)
                        .AddReference(logChanceBoostDelete)
                        .AddReference(logMoneyDelete)
                        .AddItemAttributes(item)
                        .Write();
                    // item.Save();
                    break;
            }
            Owner.SendMoneyUpdate();
        }

        private bool CalcCanUseThisStone(Asda2Item item, Asda2Item stone)
        {
            switch (stone.Category)
            {
                case Asda2ItemCategory.Enchant100Stone:
                    return true;
                case Asda2ItemCategory.EnchantWeaponStoneA:
                    return item.IsWeapon && item.RequiredLevel <= 80;
                case Asda2ItemCategory.EnchantWeaponStoneB:
                    return item.IsWeapon && item.RequiredLevel <= 60;
                case Asda2ItemCategory.EnchantWeaponStoneC:
                    return item.IsWeapon && item.RequiredLevel <= 40;
                case Asda2ItemCategory.EnchantWeaponStoneD:
                    return item.IsWeapon && item.RequiredLevel <= 20;
                case Asda2ItemCategory.EnchantWeaponStoneS:
                    return item.IsWeapon;
                case Asda2ItemCategory.EnchantArmorStoneA:
                    return item.IsArmor && item.RequiredLevel <= 80;
                case Asda2ItemCategory.EnchantArmorStoneB:
                    return item.IsArmor && item.RequiredLevel <= 60;
                case Asda2ItemCategory.EnchantArmorStoneC:
                    return item.IsArmor && item.RequiredLevel <= 40;
                case Asda2ItemCategory.EnchantArmorStoneD:
                    return item.IsArmor && item.RequiredLevel <= 20;
                case Asda2ItemCategory.EnchantArmorStoneE:
                    return item.IsArmor;
                case Asda2ItemCategory.EnchantArmorStoneS:
                    return item.IsArmor;
                case Asda2ItemCategory.EnchantUniversalStoneA:
                    return (item.IsArmor || item.IsWeapon) && item.RequiredLevel <= 80;
                case Asda2ItemCategory.EnchantUniversalStoneB:
                    return (item.IsArmor || item.IsWeapon) && item.RequiredLevel <= 60;
                case Asda2ItemCategory.EnchantUniversalStoneC:
                    return (item.IsArmor || item.IsWeapon) && item.RequiredLevel <= 40;
                case Asda2ItemCategory.EnchantUniversalStoneD:
                    return (item.IsArmor || item.IsWeapon) && item.RequiredLevel <= 20;
                case Asda2ItemCategory.EnchantUniversalStoneE:
                    return (item.IsArmor || item.IsWeapon);
                case Asda2ItemCategory.EnchantUniversalStoneS:
                    return (item.IsArmor || item.IsWeapon);
                default:
                    return false;
            }
        }

        public OpenBosterStatus OpenBooster(Asda2InventoryType inv, short cell)
        {
            if (inv != Asda2InventoryType.Regular && inv != Asda2InventoryType.Shop)
                return OpenBosterStatus.Fail;
            Asda2Item boosterItem = GetItem(inv, cell);
            if (boosterItem == null || boosterItem.Category != Asda2ItemCategory.Booster)
                return OpenBosterStatus.ItIsNotABooster;
            List<BoosterDrop> boosterRef = Asda2ItemMgr.BoosterDrops[boosterItem.BoosterId];
            if (boosterRef == null)
                return OpenBosterStatus.BoosterError;
            if (!(CheckFreeRegularItemsSlots(1) && CheckShopItemsSlots(1)))
                return OpenBosterStatus.NoSpace;
            var addedItem = new Asda2Item();
            BoosterDrop lastItemInBooster = boosterRef.Last();
            float rnd = Utility.Random(0f, 100f);
            float curChance = 0;
            LogHelperEntry lgDelete =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "open_booster_delete")
                    .AddItemAttributes(boosterItem).Write();
            foreach (BoosterDrop item in boosterRef)
            {
                curChance += item.Chance;
                if (lastItemInBooster != item && rnd > curChance)
                    continue;
                Asda2Item newItem = null;
                TryAdd(item.ItemId, 1, true, ref newItem);
                if (newItem == null)
                {
                    return OpenBosterStatus.NoSpace;
                }
                LogHelperEntry lgCreate = Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character,
                    Owner.EntryId)
                    .AddAttribute("source", 0, "open_booster_create")
                    .AddAttribute("booster_item_id", boosterItem.ItemId)
                    .AddItemAttributes(newItem);
                lgCreate.AddReference(lgDelete);
                lgCreate.Write();
                addedItem = newItem;
                if (newItem.Template.Quality >= Asda2ItemQuality.Green)
                    ChatMgr.SendGlobalMessageResponse(Owner.Name, ChatMgr.Asda2GlobalMessageType.HasObinedItem,
                        item.ItemId);
                break;
            }
            boosterItem.Destroy();
            Asda2InventoryHandler.SendbosterOpenedResponse(Owner.Client, OpenBosterStatus.Ok, addedItem, inv, cell,
                Weight);
            return OpenBosterStatus.Ok;
        }

        private Asda2Item GetItem(Asda2InventoryType inv, short cell)
        {
            switch (inv)
            {
                case Asda2InventoryType.Shop:
                    if (cell < 0 || cell >= ShopItems.Length) return null;
                    return ShopItems[cell];
                case Asda2InventoryType.Regular:
                    if (cell < 0 || cell >= RegularItems.Length) return null;
                    return RegularItems[cell];
                case Asda2InventoryType.Equipment:
                    if (cell < 0 || cell >= Equipment.Length) return null;
                    return Equipment[cell];
                case Asda2InventoryType.Warehouse:
                    if (cell < 0 || cell >= WarehouseItems.Length) return null;
                    return WarehouseItems[cell];
                case Asda2InventoryType.AvatarWarehouse:
                    if (cell < 0 || cell >= AvatarWarehouseItems.Length) return null;
                    return AvatarWarehouseItems[cell];
                default:
                    return null;
            }
        }

        public OpenPackageStatus OpenPackage(Asda2InventoryType packageInv, short packageSlot)
        {
            if (packageInv != Asda2InventoryType.Regular && packageInv != Asda2InventoryType.Shop)
                return OpenPackageStatus.PackageItemError;
            Asda2Item boosterItem = GetItem(packageInv, packageSlot);
            if (boosterItem == null || boosterItem.Category != Asda2ItemCategory.ItemPackage)
                return OpenPackageStatus.PackageItemError;
            List<PackageDrop> packageRef = Asda2ItemMgr.PackageDrops[boosterItem.Template.BoosterId];
            if (packageRef == null)
            {
                Owner.SendErrorMsg(boosterItem.BoosterId.ToString());
                return OpenPackageStatus.PackageItemError;
            }
            Owner.SendErrorMsg(boosterItem.BoosterId.ToString());
            Owner.SendErrorMsg(packageRef[0].ItemId.ToString());
            if (!(CheckFreeRegularItemsSlots(packageRef.Count) && CheckShopItemsSlots(packageRef.Count)))
                return OpenPackageStatus.InfoErrorInEmptyInventry;
            var addedItems = new List<Asda2Item>();

            LogHelperEntry lgDelete =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "open_package_delete")
                    .AddItemAttributes(boosterItem).Write();
            Owner.SendErrorMsg("hhh");
            foreach (PackageDrop item in packageRef)
            {
                Asda2ItemTemplate it = Asda2ItemMgr.GetTemplate(item.ItemId);
                if (it == null)
                    continue;
                Asda2Item newItem = null;
                TryAdd(item.ItemId, it.IsStackable ? it.MaxAmount == 0 ? item.Amount : it.MaxAmount * item.Amount : 1,
                    true,
                    ref newItem);
                if (newItem == null)
                {
                    LogUtil.WarnException("Open package get null item by Try add. Unexpected! {0} {1}",
                        Owner.Account.Name, Owner.Name);
                    return OpenPackageStatus.InfoErrorInEmptyInventry;
                }
                newItem.IsSoulbound = true;
                LogHelperEntry lgCreate = Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character,
                    Owner.EntryId)
                    .AddAttribute("source", 0, "open_package_create")
                    .AddAttribute("package_item_id", boosterItem.ItemId)
                    .AddItemAttributes(newItem);
                lgCreate.AddReference(lgDelete);
                lgCreate.Write();
                addedItems.Add(newItem);
            }
            boosterItem.Destroy();
            Asda2InventoryHandler.SendOpenPackageResponseResponse(Owner.Client, OpenPackageStatus.Ok, addedItems,
                packageInv, packageSlot, Weight);
            return OpenPackageStatus.Ok;
        }

        public DisasembleItemStatus DisasembleItem(Asda2InventoryType invNum, short slot)
        {
            if (invNum != Asda2InventoryType.Shop)
                return DisasembleItemStatus.LackOfMaterialForCraft;
            Asda2Item itemToDisasemble = GetItem(invNum, slot);
            if (itemToDisasemble == null)
                return DisasembleItemStatus.LackOfMaterialForCraft;
            if (!Asda2ItemMgr.DecompositionDrops.ContainsKey(itemToDisasemble.ItemId))
            {
                Owner.SendSystemMessage(
                    string.Format(
                        "Item id {0} can't dissassembled cause need to update dissassemble table. Please report to admin.",
                        itemToDisasemble.ItemId));
                return DisasembleItemStatus.LackOfMaterialForCraft;
            }
            List<DecompositionDrop> boosterRef = Asda2ItemMgr.DecompositionDrops[itemToDisasemble.ItemId];
            if (boosterRef == null)
                return DisasembleItemStatus.LackOfMaterialForCraft;
            if (!(CheckFreeRegularItemsSlots(1) && CheckShopItemsSlots(1)))
                return DisasembleItemStatus.NoEmptySlotInThePlate;
            if (itemToDisasemble.EnchantLevel >= 90)
            {
                int itemid = -1;
                if (itemToDisasemble.IsWeapon)
                    itemid = 401;
                if (itemToDisasemble.IsArmor)
                    itemid = 363;
                if (itemid == -1)
                {
                    Owner.SendSystemMessage(string.Format("Item is not valid to disamble. Please report to admin.", itemToDisasemble.ItemId));
                    return DisasembleItemStatus.LackOfMaterialForCraft;
                }
                Asda2Item newItem = null;
                if (TryAdd(itemid, 1, true, ref newItem) == Asda2InventoryError.Ok)
                {
                    newItem.Enchant = itemToDisasemble.Enchant;
                    newItem.EnchantChances = itemToDisasemble.EnchantChances;
                    newItem.EnchantCount = itemToDisasemble.EnchantCount;
                    newItem.EnchantLevel = itemToDisasemble.EnchantLevel;
                    itemToDisasemble.Destroy();
                    Asda2InventoryHandler.SendEquipmentDisasembledResponse(Owner.Client, DisasembleItemStatus.Ok, Weight, newItem, slot);
                    return DisasembleItemStatus.Ok;
                }
                else
                {
                    return DisasembleItemStatus.NoEmptySlotInThePlate;
                }


            }
            else
            {
                var addedItem = new Asda2Item();
                DecompositionDrop lastItemInBooster = boosterRef.Last();
                foreach (DecompositionDrop item in boosterRef)
                {
                    if (lastItemInBooster != item && Utility.Random(0f, 100f) > item.Chance)
                        continue;
                    Asda2ItemTemplate it = Asda2ItemMgr.GetTemplate(item.ItemId);
                    if (it == null)
                        return DisasembleItemStatus.LackOfMaterialForCraft;
                    Asda2Item newItem = null;
                    TryAdd(item.ItemId, 1, true, ref newItem);
                    if (newItem == null)
                    {
                        LogUtil.ErrorException("Dissassemble item get null item by Try add. Unexpected! {0} {1}",
                            Owner.Account.Name, Owner.Name);
                        return DisasembleItemStatus.CraftingInfoIsInaccurate;
                    }

                    addedItem = newItem;

                    LogHelperEntry lgCreate = Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character,
                        Owner.EntryId)
                        .AddAttribute("source", 0, "disassemble_create")
                        .AddAttribute("disassemble_item_id", itemToDisasemble.ItemId)
                        .AddItemAttributes(newItem);

                    LogHelperEntry lgDelete =
                        Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                            .AddAttribute("source", 0, "disassemble_delete")
                            .AddItemAttributes(itemToDisasemble).Write();
                    lgCreate.AddReference(lgDelete);
                    lgCreate.Write();
                    break;
                }
                itemToDisasemble.Destroy();
                Asda2InventoryHandler.SendEquipmentDisasembledResponse(Owner.Client, DisasembleItemStatus.Ok, Weight,
                    addedItem, slot);
                Asda2TitleChecker.OnDissamble(Owner, addedItem);
                return DisasembleItemStatus.Ok;
            }
        }

        public BuyFromWarShopStatus BuyItemFromWarshop(int internalWarShopId)
        {
            if (!(CheckFreeRegularItemsSlots(1) && CheckShopItemsSlots(1)))
                return BuyFromWarShopStatus.InventoryIsFull;
            WarShopDataRecord templ = Asda2ItemMgr.GetWarshopDataRecord(internalWarShopId);
            if (templ == null)
                return BuyFromWarShopStatus.CantFoundItem;
            Asda2Item moneyItem = FindItem(Asda2ItemMgr.GetTemplate(templ.Money1Type));
            if (moneyItem == null)
                return BuyFromWarShopStatus.NotEnoghtExchangeItems;
            LogHelperEntry lgDelete;
            if (moneyItem.ItemId == 20551)
            {
                //MoneyTypeis gold
                if (!Owner.SubtractMoney((uint)templ.Cost1))
                    return BuyFromWarShopStatus.NonEnoghtGold;
                lgDelete = Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "buyed_from_war_shop_remove_money")
                    .AddAttribute("cost", templ.Cost1)
                    .AddAttribute("total_money", Owner.Money)
                    .Write();
            }
            else
            {
                if (moneyItem.Amount < templ.Cost1)
                    return BuyFromWarShopStatus.NotEnoghtExchangeItems;
                lgDelete = Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "buyed_from_war_shop_remove_money_item")
                    .AddItemAttributes(moneyItem)
                    .AddAttribute("cost", templ.Cost1)
                    .Write();
                moneyItem.Amount -= templ.Cost1;
            }
            Asda2ItemTemplate itemTempl = Asda2ItemMgr.GetTemplate(templ.ItemId);
            if (itemTempl == null)
                return BuyFromWarShopStatus.CantFoundItem;
            Asda2Item addedItem = null;
            if (TryAdd(templ.ItemId, templ.Amount == 0 ? 1 : templ.Amount, true, ref addedItem) !=
                Asda2InventoryError.Ok)
                return BuyFromWarShopStatus.UnableToPurshace;

            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "buyed_from_war_shop")
                .AddReference(lgDelete)
                .AddItemAttributes(addedItem)
                .Write();
            Asda2InventoryHandler.SendItemFromWarshopBuyedResponse(Owner.Client, BuyFromWarShopStatus.Ok, Weight,
                (int)Owner.Money, moneyItem, addedItem);
            World.BroadcastMsg("Donation shop",
                string.Format("Thanks to {0} for buying {1}[{2}] and helping server!", Owner.Name, itemTempl.Name,
                    itemTempl.Id), Color.PaleGreen);
            Owner.SendMoneyUpdate();
            return BuyFromWarShopStatus.Ok;
        }

        public bool UseGlobalChatItem()
        {
            Asda2Item gci = ShopItems.FirstOrDefault(i => i != null && i.Category == Asda2ItemCategory.GlobalChat);
            if (gci == null)
            {
                Owner.SendSystemMessage("You must have global chat item to use this chat.");
                return false;
            }
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "use_global_chat_item")
                .AddItemAttributes(gci)
                .Write();
            gci.Amount--;
            ChatMgr.SendGlobalChatRemoveItemResponse(Owner.Client, true, gci);
            return true;
        }

        public bool UseTeleportScroll()
        {
            Asda2Item teleportItem =
                ShopItems.FirstOrDefault(i => i != null && i.Category == Asda2ItemCategory.TeleportToCharacter);
            if (teleportItem == null)
                return false;
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "use_teleport_scroll_item")
                .AddItemAttributes(teleportItem)
                .Write();
            teleportItem.Amount--;
            Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, teleportItem);
            return true;
        }

        public void AuctionItem(Asda2ItemTradeRef itemRef)
        {
            if (!itemRef.Item.Template.IsStackable)
                itemRef.Amount = itemRef.Item.Amount;
            Asda2Item oldItem = itemRef.Item;
            itemRef.Item = Asda2Item.CreateItem(itemRef.Item.ItemId, itemRef.Item.OwningCharacter, itemRef.Amount);
            oldItem.Amount -= itemRef.Amount;
            itemRef.Item.Slot = oldItem.Slot;
            itemRef.Item.InventoryType = oldItem.InventoryType;
            LogHelperEntry removeLog =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "auctioning_item_left")
                    .AddAttribute("amount", itemRef.Amount)
                    .AddItemAttributes(oldItem)
                    .Write();
            AddToAuction(itemRef, removeLog);
        }

        private void AddToAuction(Asda2ItemTradeRef itemRef, LogHelperEntry removeLog)
        {
            var goldCommision = (uint)(CharacterFormulas.AuctionPushComission * itemRef.Price);
            if (!Owner.SubtractMoney(goldCommision))
            {
                Owner.YouAreFuckingCheater("Auctioning item without money", 100);
                throw new InvalidOperationException("unexpected behavior");
            }
            itemRef.Item.AuctionPrice = itemRef.Price;
            itemRef.Item.Record.AuctionEndTime = DateTime.Now + TimeSpan.FromHours(itemRef.AucTime);
            Asda2AuctionMgr.RegisterItem(itemRef.Item.Record);
            itemRef.AucTime = itemRef.AucTime;
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "auctioning_item")
                .AddAttribute("commission", goldCommision)
                .AddAttribute("price", itemRef.Price)
                .AddItemAttributes(itemRef.Item)
                .AddAttribute("tolal_money", Owner.Money)
                .AddReference(removeLog)
                .Write();
            itemRef.Item.Save();
            Owner.SendAuctionMsg(string.Format("[Reg] {0} for {1} gold. [Cms] {2} gold.", itemRef.Item.Template.Name,
                itemRef.Price, goldCommision));
        }

        public void LearnRecipe(short slot)
        {
            if (slot < 1 || slot >= RegularItems.Length)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.YouAreFuckingCheater("Trying to learn not existing recipe.Bad SLOT.", 50);
                return;
            }
            Asda2Item item = RegularItems[slot];
            if (item == null)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.YouAreFuckingCheater("Trying to learn not existing recipe.");
                return;
            }
            if (item.Category != Asda2ItemCategory.Recipe)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.YouAreFuckingCheater("Trying to learn not recipe item.", 50);
                return;
            }
            int recipeId = item.Template.ValueOnUse;
            Asda2RecipeTemplate recipe = Asda2CraftMgr.GetRecipeTemplate(recipeId);
            if (recipe == null)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.SendCraftingMsg("Can't find recipe info. Recipe id is " + recipeId);
                return;
            }
            if (Owner.Record.CraftingLevel < recipe.CraftingLevel)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.SendCraftingMsg("Trying to learn recipe with level higher than you have.");
                return;
            }
            try
            {
                if (Owner.LearnedRecipes.GetBit(recipeId))
                {
                    Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                    Owner.SendCraftingMsg("Recipe already learned.");
                    return;
                }
            }
            catch (Exception)
            {
                Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, false, 0, null);
                Owner.SendCraftingMsg("Wrond recipe id " + recipeId);
                return;
            }
            Owner.LearnedRecipes.SetBit(recipeId);
            Owner.LearnedRecipesCount++;
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "learn_recipe")
                .AddItemAttributes(item)
                .Write();
            item.Amount--;
            Asda2CraftingHandler.SendRecipeLeadnedResponse(Owner.Client, true, (short)recipeId, item);
        }

        public Asda2Item FindRegularItem(int requredItemId)
        {
            return RegularItems.FirstOrDefault(i => i != null && i.ItemId == requredItemId);
        }

        public Asda2Item FindShopItem(int requredItemId)
        {
            return ShopItems.FirstOrDefault(i => i != null && i.ItemId == requredItemId);
        }

        public Asda2Item FindQuestItem(int requredItemId)
        {
            return QuestItems.FirstOrDefault(i =>
            {
                if (i != null)
                    return i.ItemId == requredItemId;
                return false;
            });
        }

        public Asda2Item GetRegularItem(short slotInq)
        {
            if (slotInq < 0 || slotInq >= RegularItems.Length)
                return null;
            return RegularItems[slotInq];
        }

        public Asda2Item GetShopShopItem(short slotInq)
        {
            if (slotInq < 0 || slotInq >= ShopItems.Length)
                return null;
            return ShopItems[slotInq];
        }

        public Asda2Item GetWarehouseItem(short slotInq)
        {
            if (slotInq < 0 || slotInq >= WarehouseItems.Length)
                return null;
            return WarehouseItems[slotInq];
        }

        public Asda2Item GetAvatarWarehouseItem(short slotInq)
        {
            if (slotInq < 0 || slotInq >= AvatarWarehouseItems.Length)
                return null;
            return AvatarWarehouseItems[slotInq];
        }

        public HatchEggStatus HatchEgg(short slotInq, short slotEgg, short slotSupl, byte invNumInq, byte invEgg)
        {
            if (slotInq < 0 || slotEgg < 0 || slotInq > RegularItems.Length || slotEgg > RegularItems.Length ||
                slotSupl > ShopItems.Length)
            {
                Owner.YouAreFuckingCheater("Sending wrong inventory info when hatching egg.", 50);
                return HatchEggStatus.Fail;
            }
            Asda2Item inq = (invNumInq == 2) ? RegularItems[slotInq] : ShopItems[slotInq];
            Asda2Item egg = (invEgg == 2) ? RegularItems[slotEgg] : ShopItems[slotEgg];
            Asda2Item supl = slotSupl < 0 ? null : ShopItems[slotSupl];
            if (inq == null || egg == null)
            {
                Owner.YouAreFuckingCheater("Egg or iqubator not exist when hatching egg.");
                return HatchEggStatus.Fail;
            }
            if (!(inq.Category == Asda2ItemCategory.Incubator || inq.Category == Asda2ItemCategory.UltimateIncubatorWith40PrcBoost))
            {
                Owner.YouAreFuckingCheater(string.Format("Trying to use {0} as incubator :)", inq.Name), 50);
                return HatchEggStatus.Fail;
            }
            if (!(egg.Category == Asda2ItemCategory.Egg || egg.Category == Asda2ItemCategory.PremiumPetEgg))
            {
                Owner.YouAreFuckingCheater(string.Format("Trying to use {0} as egg :)", egg.Name), 50);
                return HatchEggStatus.Fail;
            }
            if (egg.RequiredLevel > Owner.Level)
            {
                Owner.YouAreFuckingCheater(
                    string.Format("Trying to hatch egg with required level {0} that higher than his level {1} :)",
                        egg.RequiredLevel, Owner.Level), 50);
                return HatchEggStatus.Fail;
            }
            if (Owner.OwnedPets.Count >= (6 + 6 * Owner.Record.PetBoxEnchants))
            {
                Owner.SendInfoMsg("You already have max pet count.");
                return HatchEggStatus.Fail;
            }
            bool success = (inq.Template.ValueOnUse + (supl == null ? 0 : 30000) + (inq.Category == Asda2ItemCategory.UltimateIncubatorWith40PrcBoost ? 40000 : 0)) >= Utility.Random(0, 100000);
            if (egg.Category == Asda2ItemCategory.PremiumPetEgg)
                success = true;
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "learn_recipe")
                .AddItemAttributes(inq, "inqubator")
                .AddItemAttributes(egg, "egg")
                .AddItemAttributes(supl, "supl")
                .AddAttribute("success", success ? 1 : 0, success ? "yes" : "no")
                .Write();
            if (inq.Category != Asda2ItemCategory.UltimateIncubatorWith40PrcBoost)
                inq.Destroy();
            egg.Destroy();
            if (supl != null)
                supl.ModAmount(-1);
            if (!success)
            {
                return HatchEggStatus.PetHatchingFailed;
            }
            PetTemplate petTemplate = Asda2PetMgr.PetTemplates.Get(egg.Template.ValueOnUse);
            if (petTemplate == null)
            {
                Owner.YouAreFuckingCheater(
                    string.Format("Error on hatching egg {0} cant find template {1}.", egg, egg.Template.ValueOnUse), 0);
                return HatchEggStatus.NoEgg;
            }
            Owner.AddAsda2Pet(petTemplate);
            return HatchEggStatus.Ok;
        }

        public void OnDeath()
        {
            foreach (Asda2Item asda2Item in Equipment)
            {
                if (asda2Item == null)
                    continue;
                asda2Item.DecreaseDurability((byte)(asda2Item.MaxDurability / 10));
            }
        }

        public Asda2DonationItem AddDonateItem(Asda2ItemTemplate templ, int amount, string initializer,
            bool isSoulBound = false)
        {
            var newDiRec = new Asda2DonationItem(Owner.EntityId.Low, (int)templ.Id, amount, initializer, isSoulBound);
            newDiRec.Create();
            Owner.Asda2Inventory.DonationItems.Add(newDiRec.Guid, newDiRec);
            Asda2InventoryHandler.SendSomeNewItemRecivedResponse(Owner.Client, newDiRec.ItemId, 102);
            return newDiRec;
        }

        public void DropItems(List<Asda2Item> itemsToDrop)
        {
            Asda2NPCLoot loot = new Asda2NPCLoot();
            loot.Items = itemsToDrop.Select(asda2Item =>
              new Asda2LootItem(asda2Item.Template, 1, 0U)
              {
                  Loot = (WCell.RealmServer.Looting.Asda2Loot)loot
              }).ToArray();
            loot.Lootable = Owner;
            loot.MonstrId = 22222;
            Owner.Map.SpawnLoot(loot);
            foreach (Asda2Item asda2Item in itemsToDrop)
            {
                switch (asda2Item.InventoryType)
                {
                    case Asda2InventoryType.Shop:
                        if (asda2Item.IsWeapon || asda2Item.IsArmor ||
                           asda2Item.Category == Asda2ItemCategory.ItemPackage)
                        {
                            Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, Asda2InventoryError.Ok,
                              60, 1, -1, 0, asda2Item.Slot,
                              (byte)asda2Item.InventoryType, asda2Item.Amount, 0, false);
                            SwapUnchecked(asda2Item.InventoryType, asda2Item.Slot, Asda2InventoryType.Shop,
                              60);
                            RemoveItem(60, 1, asda2Item.Amount);
                        }

                        continue;
                    case Asda2InventoryType.Regular:
                        Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, Asda2InventoryError.Ok,
                          60, 1, -1, 0, asda2Item.Slot, (byte)asda2Item.InventoryType,
                          asda2Item.Amount, 0, false);
                        SwapUnchecked(asda2Item.InventoryType, asda2Item.Slot, Asda2InventoryType.Shop,
                          60);
                        RemoveItem(60, 1, asda2Item.Amount);
                        continue;
                    case Asda2InventoryType.Equipment:
                        Asda2InventoryHandler.SendItemReplacedResponse(Owner.Client, Asda2InventoryError.Ok,
                          60, 1, -1, 0, asda2Item.Slot, (byte)asda2Item.InventoryType,
                          asda2Item.Amount, 0, false);
                        SwapUnchecked(asda2Item.InventoryType, asda2Item.Slot, Asda2InventoryType.Shop,
                          60);
                        RemoveItem(60, 1, asda2Item.Amount);
                        continue;
                    default:
                        continue;
                }
            }
        }

        public void FillOnCharacterCreate()
        {
            SetEquipment(Asda2Item.CreateItem(20748, Owner, 1), Asda2EquipmentSlots.Shirt);
            SetEquipment(Asda2Item.CreateItem(20749, Owner, 1), Asda2EquipmentSlots.Gloves);
            SetEquipment(Asda2Item.CreateItem(20750, Owner, 1), Asda2EquipmentSlots.Pans);
            SetEquipment(Asda2Item.CreateItem(20751, Owner, 1), Asda2EquipmentSlots.Boots);
            SetRegularInventoty(Asda2Item.CreateItem(20551, Owner, 1), 0, true); //gold
            SetRegularInventoty(Asda2Item.CreateItem(20594, Owner, 10), 1, true); // HP Potion
            SetRegularInventoty(Asda2Item.CreateItem(20599, Owner, 10), 2, true); // MP Potion
            SetRegularInventoty(Asda2Item.CreateItem(20593, Owner, 10), 3, true); // Return Scroll
            SetEquipment(Asda2Item.CreateItem(22818, Owner, 1), Asda2EquipmentSlots.Weapon); 
        }

        public void CombineItems(short comtinationId)
        {
            ItemCombineDataRecord rec = Asda2ItemMgr.ItemCombineRecords[comtinationId];
            if (rec == null)
            {
                Owner.SendInfoMsg(
                    string.Format("Can't combine items cause record №{0} not founded.", comtinationId));
                return;
            }
            if (FreeRegularSlotsCount < 1 ||
                FreeShopSlotsCount < 1)
            {
                Owner.SendInfoMsg("Not enought space.");
                return;
            }
            var resourceItems = new List<Asda2Item>();
            for (int i = 0; i < 5; i++)
            {
                int id = rec.RequiredItems[i];
                if (id == -1)
                    break;
                Asda2Item item = FindItem(id, Asda2InventoryType.Regular);
                int amount = rec.Amounts[i];
                if (item == null || item.Amount < amount)
                {
                    Owner.SendInfoMsg(
                        string.Format(
                            "Can't combine items cause not enought resources. Item Id {0} amount {1}.", id,
                            amount));
                    return;
                }
                resourceItems.Add(item);
            }
            for (int i = 0; i < resourceItems.Count; i++)
            {
                Asda2Item item = resourceItems[i];
                item.Amount -= rec.Amounts[i];
            }
            Asda2Item resultItem = null;
            TryAdd(rec.ResultItem, 1, true, ref resultItem);
            LogHelperEntry resLog =
                Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                    .AddAttribute("source", 0, "combine_items")
                    .AddItemAttributes(resultItem, "result")
                    .Write();
            int w = 0;
            foreach (Asda2Item resourceItem in resourceItems)
            {
                resLog.AddItemAttributes(resourceItem, "resource_item_" + w++);
            }
            Asda2InventoryHandler.SendItemCombinedResponse(Owner.Client, resultItem, resourceItems);
        }

        public Asda2Item TryCraftItem(short recId, out List<Asda2Item> materials)
        {
            materials = new List<Asda2Item>();
            if (!Owner.LearnedRecipes.GetBit(recId))
            {
                Owner.SendErrorMsg("Trying craft not learned recipe. " + recId);
                return null;
            }
            Asda2RecipeTemplate recipe = Asda2CraftMgr.GetRecipeTemplate(recId);
            if (recipe == null)
            {
                Owner.SendErrorMsg("Can't find recipe template. " + recId);
                return null;
            }
            if (FreeRegularSlotsCount < 1 || FreeRegularSlotsCount < 1)
            {
                Owner.SendCraftingMsg("Not enought space.");
                return null;
            }
            int i = 0;
            foreach (int requredItemId in recipe.RequredItemIds)
            {
                if (requredItemId == -1)
                    break;
                Asda2Item item = FindItem(requredItemId, Asda2InventoryType.Regular);
                if (item == null || item.Amount < recipe.ReqiredItemAmounts[i])
                {
                    Owner.SendErrorMsg("Not enought materials to craft.");
                    return null;
                }
                materials.Add(item);
                item.Amount -= recipe.ReqiredItemAmounts[i];
                i++;
            }
            byte rarity = CharacterFormulas.GetCraftedRarity();
            if (rarity == 0)
            {
                return null;
            }
            if (rarity > recipe.MaximumPosibleRarity)
                rarity = recipe.MaximumPosibleRarity;

            int craftedItemId = recipe.ResultItemIds[rarity - 1];
            short craftedItemAmount = recipe.ResultItemAmounts[rarity - 1];
            if (craftedItemAmount <= 0)
            {
                Owner.SendErrorMsg("Crafted amount error.");
                return null;
            }
            Asda2Item resultItem = null;
            if (
                TryAdd(craftedItemId, craftedItemAmount, false, ref resultItem) != Asda2InventoryError.Ok)
            {
                Owner.SendErrorMsg("Cant add crafted item.");
                return null;
            }
            Logs.Log.Create(Logs.Log.Types.ItemOperations, LogSourceType.Character, Owner.EntryId)
                .AddAttribute("source", 0, "craft_create")
                .AddItemAttributes(resultItem)
                .AddAttribute("recipe_id", recipe.Id)
                .Write();
            //Calc craft exp as character formulas
            int diffLvl = Owner.Record.CraftingLevel - recipe.CraftingLevel;
            float exp = CharacterFormulas.CalcCraftingExp(diffLvl, Owner.Record.CraftingLevel);
            if (diffLvl > 0)
                Owner.GuildPoints += CharacterFormulas.CraftingGuildPointsPerLevel * diffLvl;

            Owner.Record.CraftingExp += exp;
            if (Owner.Record.CraftingExp >= 100)
            {
                Owner.Record.CraftingLevel++;
                Owner.Record.CraftingExp = 0;
            }
            int expForCharacter = CharacterFormulas.CalcExpForCrafting(diffLvl,
                Owner.Record
                    .CraftingLevel,
                (byte)Owner.Level);
            Owner.GainXp(expForCharacter, "craft");
            resultItem.Record.IsCrafted = true;
            //resultItem.GenerateOptionsByCraft();
            return resultItem;
        }

        #region warehouse

        public void PushItemsToWh(IEnumerable<Asda2WhItemStub> itemStubs)
        {
            if (!IsItemsExists(itemStubs)
                || !IsInventorySpaceEnough(itemStubs, false, false))
            {
                Asda2InventoryHandler.SendItemsPushedToWarehouseResponse(Owner.Client, PushItemToWhStatus.ItemNotFounded);
                return;
            }
            var soureItems = new List<Asda2WhItemStub>();
            var destItems = new List<Asda2WhItemStub>();
            foreach (Asda2WhItemStub asda2ItemStub in itemStubs)
            {
                Asda2Item item = GetItem(asda2ItemStub.Invtentory, asda2ItemStub.Slot);
                Asda2Item addedItem = null;
                TryAdd(item.ItemId, asda2ItemStub.Amount, true, ref addedItem, Asda2InventoryType.Warehouse, item);
                item.Amount -= asda2ItemStub.Amount;
                soureItems.Add(new Asda2WhItemStub { Amount = item.Amount, Invtentory = item.InventoryType, Slot = item.Slot });
                destItems.Add(new Asda2WhItemStub { Amount = addedItem.Amount, Invtentory = addedItem.InventoryType, Slot = addedItem.Slot });
            }
            Asda2InventoryHandler.SendItemsPushedToWarehouseResponse(Owner.Client, PushItemToWhStatus.Ok, soureItems, destItems);
        }

        public void TakeItemsFromWh(IEnumerable<Asda2WhItemStub> itemStubs)
        {
            if (IsWarehouseLocked()
                || !IsItemsExists(itemStubs)
                || !IsInventorySpaceEnough(itemStubs, true, false)
                || !GetCommissionForTake(itemStubs.Count()))
            {
                Asda2InventoryHandler.SendItemsTakedFromWarehouseResponse(Owner.Client,
                    PushItemToWhStatus.ItemNotFounded);
                return;
            }
            var soureItems = new List<Asda2WhItemStub>();
            var destItems = new List<Asda2WhItemStub>();
            foreach (Asda2WhItemStub asda2ItemStub in itemStubs)
            {
                Asda2Item item = GetItem(asda2ItemStub.Invtentory, asda2ItemStub.Slot);
                Asda2Item addedItem = null;
                TryAdd(item.ItemId, asda2ItemStub.Amount, true, ref addedItem, null, item);
                item.Amount -= asda2ItemStub.Amount;
                soureItems.Add(new Asda2WhItemStub { Amount = item.Amount, Invtentory = item.InventoryType, Slot = item.Slot });
                destItems.Add(new Asda2WhItemStub { Amount = addedItem.Amount, Invtentory = addedItem.InventoryType, Slot = addedItem.Slot });
                Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, item);
                Asda2InventoryHandler.UpdateItemInventoryInfo(Owner.Client, addedItem);
            }
            Asda2InventoryHandler.SendItemsTakedFromWarehouseResponse(Owner.Client, PushItemToWhStatus.Ok, soureItems, destItems);
        }

        private bool GetCommissionForTake(int count)
        {
            return (Owner.SubtractMoney((uint)(count * 30)));
        }

        private bool IsInventorySpaceEnough(IEnumerable<Asda2WhItemStub> itemStubs, bool pop, bool isAvatar)
        {
            if (pop) //достаем со склада
            {
                if (FreeShopSlotsCount < itemStubs.Count())
                {
                    Owner.SendInfoMsg("Not enought space in shop inventory.");
                    return false;
                }
                if (FreeRegularSlotsCount < itemStubs.Count())
                {
                    Owner.SendInfoMsg("Not enought space in regular inventory.");
                    return false;
                }
            }
            else // кладем на склад
            {
                {
                    if (FreeWarehouseSlotsCount < itemStubs.Count())
                    {
                        Owner.SendInfoMsg("Not enought space in warehouse.");
                        return false;
                    }
                }
            }
            return true;
        }


        private bool IsItemsExists(IEnumerable<Asda2WhItemStub> itemStubs)
        {
            foreach (Asda2WhItemStub itemStub in itemStubs)
            {
                Asda2Item item = GetItem(itemStub.Invtentory, itemStub.Slot);
                if (item != null)
                {
                    if (item.IsDeleted)
                    {
                        Owner.SendErrorMsg(string.Format("Item is deleted. inv {0}, slot {1}.", itemStub.Invtentory,
                            itemStub.Slot));
                        return false;
                    }
                    if (item.Amount < itemStub.Amount || itemStub.Amount == 0)
                    {
                        Owner.SendErrorMsg(
                            string.Format("Item amount is {0} but required {1}. inv {2}, slot {3}.",
                                item.Amount, itemStub.Amount, itemStub.Invtentory, itemStub.Slot));
                        return false;
                    }
                    if (item.ItemId == 20551)
                    {
                        Owner.SendErrorMsg(string.Format("You cant put gold to warehouse. inv {0}, slot {1}.",
                            itemStub.Invtentory, itemStub.Slot));
                        return false;
                    }
                }
                else
                {
                    Owner.SendErrorMsg(string.Format("Item not found. inv {0}, slot {1}.", itemStub.Invtentory,
                        itemStub.Slot));
                    return false;
                }
            }
            return true;
        }

        private bool IsWarehouseLocked()
        {
            if (!Owner.IsWarehouseLocked)
                return false;
            Owner.SendInfoMsg(
                "Your warehouse is locked. Use <#Warehouse unlock [pass]> command to unlock it. Or use char manager.");
            return true;
        }

        #endregion
    }
}