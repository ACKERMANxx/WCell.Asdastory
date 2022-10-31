using System;
using System.Drawing;
using WCell.Constants;
using WCell.Constants.Items;
using WCell.Constants.Spells;
using WCell.Constants.Updates;
using WCell.Constants.World;
using WCell.Core.Network;
using WCell.Intercommunication;
using WCell.Intercommunication.DataTypes;
using WCell.RealmServer.Asda2Style;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Items;
using WCell.RealmServer.Logs;
using WCell.RealmServer.Network;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.RacesClasses;
using WCell.Util.Commands;

namespace WCell.RealmServer.Commands
{
    public class BanChatCommand : RealmServerCommand
    {
        protected BanChatCommand() { }

        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            base.Init("banchat");
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var name = trigger.Text.NextWord();
            var minutes = trigger.Text.NextInt(60);
            var reason = trigger.Text.NextQuotedString();
            var chr = World.GetCharacter(name, false);
            if (chr == null)
            {
                trigger.Reply("character not founded");
                return;
            }
            chr.ChatBanned = true;
            chr.BanChatTill = DateTime.Now.AddMinutes(minutes);
            World.BroadcastMsg("Ban system",string.Format("{0} chat is banned by {1} for {2} minutes. Reason : {3}.",name,trigger.Args.Character.Name,minutes,reason),Util.Graphics.Color.Red);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }

    public class UnBanChatCommand : RealmServerCommand
    {
        protected UnBanChatCommand() { }

        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            base.Init("unbanchat");
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var name = trigger.Text.NextWord();
            var chr = World.GetCharacter(name, false);
            if (chr == null)
            {
                trigger.Reply("character not founded");
                return;
            }
            chr.ChatBanned = false;
            World.BroadcastMsg("Ban system", string.Format("{0} chat is unbanned by {1}.", name, trigger.Args.Character.Name), Util.Graphics.Color.Red);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }

    #region GiveXP
    public class GiveXPCommand : RealmServerCommand
    {
        protected GiveXPCommand() { }

        protected override void Initialize()
        {
            base.Init("GiveXP", "XP", "Exp");
            EnglishParamInfo = "<amount>";
            EnglishDescription = "Gives the given amount of experience.";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = ((Character)trigger.Args.Target);
            var xp = trigger.Text.NextInt(1);

            chr.GainXp(xp, "gm_command");
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    #endregion
    public class SoulmateExpCommand : RealmServerCommand
    {
        protected SoulmateExpCommand() { }

        protected override void Initialize()
        {
            base.Init("smexp");
            EnglishParamInfo = "<amount>";
            EnglishDescription = "Sets the given amount of soulmating experience.";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = ((Character)trigger.Args.Target);
            var xp = trigger.Text.NextInt(1);

            if (chr.SoulmateRecord == null)
                return;
            chr.SoulmateRecord.Expirience = xp;
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }

    public class PetExpCommand : RealmServerCommand
    {
        public static PetExpCommand Instance { get; private set; }

        protected PetExpCommand()
        {
            Instance = this;
        }

        protected override void Initialize()
        {
            Init("PetExp", "pxp");
            EnglishParamInfo = "Pet Exp";
            EnglishDescription = "Pet Exp";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = ((Character)trigger.Args.Target);
            var xp = trigger.Text.NextInt(1);

            chr.ActivePet.Expirience = xp;
        }
    }

    public class AddDonationItemCommand : RealmServerCommand
    {
        protected override void Initialize()
        {
            Init("adi", "AddDonatedItem");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var targetChr = trigger.Args.Target as Character;
            if (targetChr == null)
            {
                trigger.Reply("Wrong target.");
                return;
            }
            var entry = trigger.Text.NextEnum(Asda2ItemId.None);

            var templ = Asda2ItemMgr.GetTemplate(entry);

            if (templ == null)
            {
                trigger.Reply("Invalid ItemId.");
                return;
            }

            var amount = trigger.Text.NextInt(1);
            if (amount <= 0)
            {
                trigger.Reply("Wrong amount.");
                return;
            }
            RealmServer.IOQueue.AddMessage(()=>targetChr.Asda2Inventory.AddDonateItem(templ, amount, trigger.Args.Character.Name));
        }
    }

    public class TransformToPetCommand : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            Init("transform", "TransformToPet", "t");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var targetChr = trigger.Args.Target as Character;
            if (targetChr == null)
            {
                trigger.Reply("Wrong target.");
                return;
            }
            var entry = trigger.Text.NextInt(3);
            if (entry > 810 || entry < -1)
                entry = -1;
            targetChr.TransformationId = (short)entry;
        }
    }

    public class EmoteCommand : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            Init("emote");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = ((Character)trigger.Args.Target);
            var emote = trigger.Text.NextInt(0);
            SendEmoteResponse(chr, (short)emote, 1, (float)0.0596617, (float)-0.9982219);
        }
        static byte _emoteCnt;
        public static void SendEmoteResponse(Character chr, short emote, byte c = 1, float a = 0.0596617f, float b = -0.99822187f)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.EmoteResponse))//4025
            {
                packet.WriteInt16(chr.SessionId);//{sessId}default value : 25 Len : 2
                packet.WriteInt32(chr.AccId);//{sessId}default value : 25 Len : 2
                packet.WriteInt16(emote);//{emote}default value : 113 Len : 2
                packet.WriteByte(c);//{c}default value : 1 Len : 1
                packet.WriteFloat(a);//{a}default value : 0,05966117 Len : 4
                packet.WriteFloat(b);//{b}default value : -0,9982187 Len : 4
                packet.WriteByte(_emoteCnt++);
                chr.SendPacketToArea(packet, false, true);
            }
        }
    }

    public class HairCommand : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            Init("Hair");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = ((Character)trigger.Args.Target);
            var id = trigger.Text.NextInt(0);
            var color = trigger.Text.NextInt(0);
            var template = Asda2StyleMgr.HairTemplates[(short)id];
            var template2 = Asda2StyleMgr.HairTemplates[(short)color];
            chr.HairColor = template2.HairColor;
            chr.HairStyle = (byte)template.Id;
            SendFaceOrHairChangedResponse(chr.Client, true, true, null);
        }

        public static void SendFaceOrHairChangedResponse(IRealmClient client, bool isHair, bool success = false, Asda2Item usedItem = null)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.FaceOrHairChanged))//5471
            {
                packet.WriteByte(success ? 1 : 0);//{status}default value : 1 Len : 1
                packet.WriteInt16(isHair ? 1 : 2);//{hair1face2}default value : 2 Len : 1
                packet.WriteInt32((byte)client.ActiveCharacter.HairStyle);//{hairId}default value : 3 Len : 1
                packet.WriteInt32((byte)client.ActiveCharacter.HairColor);//{hairColor}default value : 3 Len : 1
                client.Send(packet, addEnd: true);
            }
        }
    }

    public class TransportCommand : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.EventManager; }
        }

        protected override void Initialize()
        {
            Init("transport");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var targetChr = trigger.Args.Target as Character;
            if (targetChr == null)
            {
                trigger.Reply("Wrong target.");
                return;
            }
            var entry = trigger.Text.NextInt(3);
            targetChr.TransportItemId = (short)entry;
        }
    }
    public class Warrior : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("WAR");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Character.Archetype.ClassId != ClassId.NoClass|| trigger.Args.Character.Level<5)
                return;
            trigger.Args.Character.SetClass(1, 3);
            Asda2Item a = null;

            trigger.Args.Character.Asda2Inventory.TryAdd(490, 1, false, ref a);
            trigger.Args.Character.Asda2Inventory.TryAdd(503, 1, false, ref a);
        }
    }
    public class Acher : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("ARC");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Character.Archetype.ClassId != ClassId.NoClass|| trigger.Args.Character.Level < 5)
                return;
            trigger.Args.Character.SetClass(1, 5);
            Asda2Item a = null;
            trigger.Args.Character.Asda2Inventory.TryAdd(490, 1, false, ref a);
            trigger.Args.Character.Asda2Inventory.TryAdd(503, 1, false, ref a);
        }
    }
    public class Mage : RealmServerCommand
    {
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("MAG");
        }
        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Character.Archetype.ClassId != ClassId.NoClass || trigger.Args.Character.Level < 5)
                return;
            trigger.Args.Character.SetClass(1, 7);
            Asda2Item a = null;

            trigger.Args.Character.Asda2Inventory.TryAdd(490, 1, false, ref a);
            trigger.Args.Character.Asda2Inventory.TryAdd(503, 1, false, ref a);
        }
    }
    #region Change arhitype
    public class SetProffessionCommand : RealmServerCommand
    {
        protected SetProffessionCommand() { }

        protected override void Initialize()
        {
            base.Init("SetProff", "proff", "setpr");
            EnglishParamInfo = "<proffId,proffLevel>";
            EnglishDescription = "OHS = 1,Spear = 2,THS = 3,Crossbow = 4,Bow = 5,Balista = 6,AtackMage = 7,SupportMage = 8,HealMage = 9,.Ex: setpr 4 1";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("OHS = 1,Spear = 2,THS = 3,Crossbow = 4,Bow = 5,Balista = 6,AtackMage = 7,SupportMage = 8,HealMage = 9 Ex: proff 4 1");
                return;

            }
            var chr = ((Character)trigger.Args.Target);
            var proff = trigger.Text.NextInt(1);
            var proffLevel = trigger.Text.NextInt(1);
            if (proffLevel <= 0 || proffLevel > 4)
            {
                trigger.Reply("OHS = 1,Spear = 2,THS = 3,Crossbow = 4,Bow = 5,Balista = 6,AtackMage = 7,SupportMage = 8,HealMage = 9,.Ex: proff 4 1");
                trigger.Reply("You must select proff level 1 - 4");
                return;
            }
            if (proff < 0 || proff >= (decimal)ClassId.End)
            {
                trigger.Reply("OHS = 1,Spear = 2,THS = 3,Crossbow = 4,Bow = 5,Balista = 6,AtackMage = 7,SupportMage = 8,HealMage = 9,.Ex: proff 4 1");
                trigger.Reply("You must select real proff proffession");
                return;
            }
            chr.SetClass(proffLevel, proff);
        }


        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    #endregion

    #region add stats
    public class AddStrengthCommand : RealmServerCommand
    {
        protected AddStrengthCommand() { }

        protected override void Initialize()
        {
            base.Init("AddStrength", "astr");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat strength";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddStrength 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Strength, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddStaminaCommand : RealmServerCommand
    {
        protected AddStaminaCommand() { }

        protected override void Initialize()
        {
            base.Init("AddStamina", "asta");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat stamina";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddStamina 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Stamina, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddDexterityCommand : RealmServerCommand
    {
        protected AddDexterityCommand() { }

        protected override void Initialize()
        {
            base.Init("AddDexterity", "adex");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat agility";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddAgility 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Dexterity, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddIntellectCommand : RealmServerCommand
    {
        protected AddIntellectCommand() { }

        protected override void Initialize()
        {
            base.Init("AddIntellect", "aint");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat Intellect";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddIntellect 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Intelect, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddSpiritCommand : RealmServerCommand
    {
        protected AddSpiritCommand() { }

        protected override void Initialize()
        {
            base.Init("AddSpirit", "aspi");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat Energy";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddEnergy 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Spirit, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddLuckCommand : RealmServerCommand
    {
        protected AddLuckCommand() { }

        protected override void Initialize()
        {
            base.Init("AddLuck", "aluc");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Adds points to base stat Luck";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (!trigger.Text.HasNext)
            {
                trigger.Reply("Enter the numper of points you want to add. Ex: AddLuck 20");
                return;

            }
            var chr = trigger.Args.Character;
            if (chr == null)
                return;
            var points = trigger.Text.NextInt(0);

            var res = chr.TryAddStatPoints(Asda2StatType.Luck, points);
            trigger.Reply(res);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class HowToAddStatsCommand : RealmServerCommand
    {
        protected HowToAddStatsCommand() { }

        protected override void Initialize()
        {
            base.Init("HowToAddStats");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Info how to add stats";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Character == null)
                return;
            trigger.Reply(string.Format("You have {0} free stat points. Enter the folowint commands to spend it.", trigger.Args.Character.FreeStatPoints));
            trigger.Reply("#AddStrength [numberOfPointsToAdd]");
            trigger.Reply("#AddDexterity [numberOfPointsToAdd]");
            trigger.Reply("#AddIntellect [numberOfPointsToAdd]");
            trigger.Reply("#AddSpirit [numberOfPointsToAdd]");
            trigger.Reply("#AddLuck [numberOfPointsToAdd]");
            trigger.Reply("Ex: #AddStrength 20");
            trigger.Reply("Ex: #AStr 20");
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class AddFreeStatPointsCommand : RealmServerCommand
    {
        protected AddFreeStatPointsCommand() { }

        protected override void Initialize()
        {
            base.Init("AddFreeStatPoints", "addstatpoints", "asp");
            EnglishParamInfo = "<points>";
            EnglishDescription = "Info how to add stats";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Target == null || !(trigger.Args.Target is Character))
                return;
            var points = trigger.Text.NextInt(0);
            trigger.Args.Character.FreeStatPoints += points;
            Log.Create(Log.Types.StatsOperations, LogSourceType.Character, trigger.Args.Character.EntryId)
                                                     .AddAttribute("source", 0, "gm_add_stats")
                                                     .AddAttribute("amount", points)
                                                     .AddAttribute("gm_name", 0, trigger.Args.User.Name)
                                                     .Write();
            trigger.Reply(string.Format("You have {0} free stat points.", trigger.Args.Character.FreeStatPoints));

        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    public class ResetStatsCommand : RealmServerCommand
    {
        protected ResetStatsCommand() { }

        protected override void Initialize()
        {
            base.Init("ResetStats");
            EnglishParamInfo = "";
            EnglishDescription = "Reset stats to 5 and adding free stat points";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            if (trigger.Args.Character == null)
                return;
            var chr = trigger.Args.Target as Character;
            if (chr == null)
            {
                trigger.Reply("Target is not character");
                return;
            }
            chr.ResetStatPoints();
            trigger.Reply(string.Format("You have {0} free stat points.", trigger.Args.Character.FreeStatPoints));

        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    #endregion

    #region Level
    public class LevelCommand : RealmServerCommand
    {
        protected LevelCommand() { }

        protected override void Initialize()
        {
            Init("Level");
            EnglishParamInfo = "[-o] <level>";
            EnglishDescription = "Sets the target's level. Using -o Allows overriding the servers configured Max Level";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var mod = trigger.Text.NextModifiers();
            var unit = trigger.Args.Target;

            var level = trigger.Text.NextInt(unit.Level);

            if (!mod.Contains("o") && level > unit.MaxLevel)
            {
                trigger.Reply("Max Level is {0} use the -o switch if you intended to set above this", unit.MaxLevel);
                return;
            }

            unit.Level = level;
        }

        public override ObjectTypeCustom TargetTypes
        {
            get { return ObjectTypeCustom.Unit; }
        }
    }
    #endregion
    #region AutoLoot
    public class AutoLoot : RealmServerCommand
    {
        protected AutoLoot() { }

        protected override void Initialize()
        {
            Init("AutoLoot", "al");
            EnglishParamInfo = "[-o] <level>";
            EnglishDescription = "Enables or disbles autoloot";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = trigger.Args.Target as Character;
            if (chr == null)
            {
                trigger.Reply("Wrong target.");
                return;
            }
            chr.AutoLoot = !chr.AutoLoot;
            trigger.Reply("Autoloot {0}.", chr.AutoLoot ? "On" : "Off");

        }

        public override ObjectTypeCustom TargetTypes
        {
            get { return ObjectTypeCustom.Unit; }
        }
    }
    #endregion
    #region Bank
    public class BankCommand : RealmServerCommand
    {
        protected BankCommand() { }

        protected override void Initialize()
        {
            base.Init("Bank");
            EnglishParamInfo = "";
            EnglishDescription = "Opens the bank for the target through oneself (if one leaves the target, it won't be allowed to continue using the Bank).";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            ((Character)trigger.Args.Target).OpenBank(trigger.Args.Character);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    #endregion

    #region Exploration
    public class ExploreCommand : RealmServerCommand
    {
        protected ExploreCommand() { }

        protected override void Initialize()
        {
            Init("Explore");
            EnglishParamInfo = "[<zone>]";
            EnglishDescription = "Explores the map. If zone is given it will toggle exploration of that zone, else it will explore all zones.";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var chr = (Character)trigger.Args.Target;
            var zone = trigger.Text.NextEnum(ZoneId.None);
            if (zone == ZoneId.None)
            {
                for (var i = PlayerFields.EXPLORED_ZONES_1;
                    i < (PlayerFields)((uint)PlayerFields.EXPLORED_ZONES_1 + UpdateFieldMgr.ExplorationZoneFieldSize); i++)
                {
                    chr.SetUInt32(i, uint.MaxValue);
                }
            }
            else
            {
                chr.SetZoneExplored(zone, !chr.IsZoneExplored(zone));
            }
        }

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.Player;
            }
        }
    }
    #endregion
}