using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Network;

namespace WCell.RealmServer.Handlers
{
    public class PacketStructureSegments
    {
        public static void CharacterInformation(IRealmClient client, RealmPacketOut packet,CharacterRecord ChNew = null)
        {
            var acc = client.Account;
            CharacterRecord ch = null;
            Character CharacterBackup = null;
            if (ChNew == null)
            {
                ch = acc.ActiveCharacter.Record;
                CharacterBackup = acc.ActiveCharacter;
            }
            else
            {
                ch = ChNew;
                CharacterBackup = ch.CreateCharacter();
                CharacterBackup.LoadSometypes(client.Account, ch, client);
            }
            //Part1
            packet.WriteInt32(acc.AccountId);
            packet.WriteFixedAsciiString(ch.Name, 20);
            packet.WriteByte(ch.CharNum);
            packet.WriteByte(0);//Unknown
            packet.WriteByte(ch.Zodiac);
            packet.WriteByte((byte)ch.Gender);//{gender}default value : 1
            packet.WriteByte(ch.ProfessionLevel);//value name : unk9 default value : 0
            packet.WriteByte((byte)ch.Class);
            packet.WriteByte((byte)ch.Level);
            packet.WriteUInt32(ch.Xp);//{expCount}default value : 0
            packet.WriteInt32(ch.Xp);//value name : unk13 default value : 0
            packet.WriteInt16(CharacterBackup.Spells.AvalibleSkillPoints);//value name : unk14 
            packet.WriteByte(CharacterBackup.RealProffLevel);
            packet.WriteInt16(15000);//{weightMax}default value : 15000
            packet.WriteInt16(1000);//{weightMin}default value : 2011
            packet.WriteInt16(0);
            packet.WriteInt32(0);
            packet.WriteInt32(0);
            packet.WriteByte(0);
            packet.WriteByte(0);
            packet.WriteInt16((ch.PremiumWarehouseBagsCount + 1) * 30); //value name : unk22 default value : 30Len : 1
            packet.WriteInt16(CharacterBackup.Asda2Inventory.WarehouseItems.Count(i => i != null)); //value name : wh zanyato default value : 4Len : 2
            packet.WriteByte(0);
            packet.WriteByte(0);
            //Part2
            packet.WriteInt16(0);
            packet.WriteByte(0);
            packet.WriteByte(0);
            //Part3
            packet.WriteByte(ch.Zodiac);         
            ///////////////////////////////////////////////////
            //packet.WriteInt16((client.ActiveCharacter.Record.PremiumAvatarWarehouseBagsCount + 1) * 30); //value name : unk26 default value : 30Len : 1
            //packet.WriteInt16(client.ActiveCharacter.Asda2Inventory.AvatarWarehouseItems.Count(i => i != null)); //value name : wh zanyato default value : 4Len : 2

        }
        public static void CharacterShape(IRealmClient client, RealmPacketOut packet, CharacterRecord ChNew = null)
        {
            var acc = client.Account;
            CharacterRecord ch = null;
       
            if (ChNew == null)
            {
                ch = acc.ActiveCharacter.Record;
               
            }
            else
            {
                ch = ChNew;
             
            }

            packet.WriteByte(ch.HairStyle);//{hairStyle}default value : 4
            packet.WriteByte(ch.HairColor);//{hairColor}default value : 5
            packet.WriteByte(ch.Face);//{face0}default value : 27
            packet.WriteInt32(0);
        }
        public static void CharacterAbilities(IRealmClient client, RealmPacketOut packet, CharacterRecord ChNew = null)
        {
            var acc = client.Account;
            CharacterRecord ch = null;
            Character CharacterBackup = null;
            if (ChNew == null)
            {
                ch = acc.ActiveCharacter.Record;
                CharacterBackup = acc.ActiveCharacter;
            }
            else
            {
                ch = ChNew;
                CharacterBackup = ch.CreateCharacter();
                CharacterBackup.LoadSometypes(client.Account, ch, client);
            }
            packet.WriteInt32(ch.Health);//{maxHp}default value : 100
            packet.WriteInt16(ch.Power);//{maxMp}default value : 100
            packet.WriteInt32(ch.BaseHealth);//{curHp}default value : 100
            packet.WriteInt16(ch.BasePower);//{curMp}default value : 100
            packet.WriteInt16((short)CharacterBackup.MinDamage);//{minAtk}default value : 4
            packet.WriteInt16((short)CharacterBackup.MaxDamage);//{maxAtk}default value : 5
            packet.WriteInt16((short)CharacterBackup.RangedAttackPower);//{minMAtk}default value : 1
            packet.WriteInt16((short)CharacterBackup.RangedAttackPower);//{maxMAtk}default value : 1
            packet.WriteInt16(CharacterBackup.ArcaneResist);//{mDef}default value : 0
            short Defense = (CharacterBackup.Level != 0 ? (short)CharacterBackup.Defense : (short)(5 * ch.Level));
            packet.WriteInt16(Defense);//{minDef}default value : 17
            packet.WriteInt16(Defense);//{maxDef}default value : 28            
            packet.WriteByte(15); //value name : unk44 default value : 15Len : 2
            packet.WriteByte(0);
            packet.WriteInt16(7); //value name : unk45 default value : 7Len : 2
            packet.WriteInt16(4); //value name : unk46 default value : 4Len : 2
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
            packet.WriteInt16(0);
        }
        public static void CharacterUpdate(IRealmClient client, RealmPacketOut packet, CharacterRecord ChNew = null)
        {
            var acc = client.Account;
            Character ch = null;
            if (ChNew == null)
                ch = acc.ActiveCharacter;
            else
            {
                ch = ChNew.CreateCharacter();
                ch.Create(acc, ChNew, client);
            }
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Strength)); //{strength}default value : 166 Len : 2
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Agility)); //{dex}default value : 74 Len : 2
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Stamina)); //{stamina}default value : 120 Len : 2
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Spirit)); //{spirit}default value : 48 Len : 2
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Intellect)); //{int}default value : 74 Len : 2
            packet.WriteInt16(Asda2CharacterHandler.ProcessOwerFlow(client.ActiveCharacter.Asda2Luck)); //{skill5Id}default value : 48 Len : 2

        }
        public static void AllItemInformation(IRealmClient client, RealmPacketOut packet)
        {
            for (int i = 0; i < 11; i++)
            {
                ItemInformation(client, packet, client.ActiveCharacter.Asda2Inventory.Equipment[i]);
            }
        }
        public static void ItemInformation(IRealmClient client, RealmPacketOut packet,Asda2Item item, bool oldWay = false)
        {            
            /*if (oldWay)
            {
                packet.WriteInt32(item == null ? 0 : item.ItemId); //default value : 0 4
            }
            else
            {

            if (item != null && !item.IsDeleted)
            packet.WriteInt32(item == null ? 0 : item.ItemId); //default value : 0 4
            else
            packet.WriteInt32(0); //default value : 0 4
            }*/
            packet.WriteInt32(item == null ? 0 : item.ItemId); //default value : 0 4

            packet.WriteByte(item == null ? 0 : (byte)item.InventoryType); //value name : _ 1
            packet.WriteInt16(item == null ? -1 : item.Slot); //value name : _slot 2
            packet.WriteInt16(item == null ? -1 : item.IsDeleted ? -1 : 0);//unknown 2
            packet.WriteItemAmount(item); //value name : _ 4
            packet.WriteByte(item == null ? 0 : item.Durability); //default value : 100 1
            packet.WriteInt16(item == null ? 0 : item.Template.Weight); //default value : 500 weight 2
            packet.WriteInt16(item == null ? -1 : item.Soul1Id); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Soul2Id); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Soul3Id); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Soul4Id); //default value : -1
            packet.WriteByte(item == null ? 0 : item.Enchant); //default value : 0
            packet.WriteByte(item == null ? 0 : item.EnchantCount);
            packet.WriteByte(item == null ? 0 : item.Template.EnchantChances);//  UpgradeChances
            packet.WriteInt16(item == null ? 0 : item.EnchantLevel); //value name : UpgradeValue
            packet.WriteInt16(item == null ? -1 : (short)item.Parametr1Type); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Parametr1Value); //default value : -1
            packet.WriteInt16(item == null ? -1 : (short)item.Parametr2Type); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Parametr2Value); //default value : 0
            packet.WriteInt16(item == null ? -1 : (short)item.Parametr3Type); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Parametr3Value); //default value : 0
            packet.WriteInt16(item == null ? -1 : (short)item.Parametr4Type); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Parametr4Value); //default value : 0
            packet.WriteInt16(item == null ? -1 : (short)item.Parametr5Type); //default value : -1
            packet.WriteInt16(item == null ? -1 : item.Parametr5Value); //default value : 0
            packet.WriteByte(0); //value name : _
            packet.WriteByte(item == null ? -1 : item.IsSoulbound ? 1 : 0); //default value : 0
            packet.WriteInt16(0); //value name : _
            packet.WriteInt16(0); //value name : _
            packet.WriteInt16(0); //value name : _

        }
        public static void SettingsFlags(IRealmClient client, RealmPacketOut packet)
        {
            if (client.ActiveCharacter == null)
                return;
            for (int i = 0; i < 19; i += 1)
            {
                if (i < client.ActiveCharacter.SettingsFlags.Length)
                    packet.WriteByte(client.ActiveCharacter.SettingsFlags[i]);
                else
                    packet.WriteByte(0);


            }
        }
    }
}
