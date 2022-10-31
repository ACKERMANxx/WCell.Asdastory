using System;
using System.Collections.Generic;
using WCell.Constants;
using WCell.Constants.Updates;
using WCell.Core.Network;
using WCell.RealmServer.Chat;
using WCell.RealmServer.Entities;
using WCell.Util;
using WCell.Util.Commands;
using WCell.Util.Graphics;
namespace WCell.RealmServer.Commands
{
	#region Kill
	public class KillCommand : RealmServerCommand
	{
		protected KillCommand() { }

		protected override void Initialize()
		{
			Init("Kill");
			EnglishDescription = "Kills your current target.";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			if (target == trigger.Args.Character)
			{
				target = target.Target;
				if (target == null)
				{
					trigger.Reply("Invalid Target.");
					return;
				}
			}

			//SpellHandler.SendVisual(target, SpellId.Lightning);
			target.Kill(trigger.Args.Character);
		}

		public override bool RequiresCharacter
		{
			get
			{
				return true;
			}
		}
	}
	#endregion

	#region Resurrect
	public class ResurrectCommand : RealmServerCommand
	{
		protected ResurrectCommand() { }

		protected override void Initialize()
		{
			Init("Resurrect", "Res");
			EnglishDescription = "Resurrects the Unit";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			if (target != null)
			{
				target.Resurrect();
			}
		}

		public override ObjectTypeCustom TargetTypes
		{
			get { return ObjectTypeCustom.Unit; }
		}
	}
	#endregion

	#region Health
	public class HealthCommand : RealmServerCommand
	{
		protected HealthCommand() { }

		protected override void Initialize()
		{
			Init("Health");
			EnglishParamInfo = "<amount>";
			EnglishDescription = "Sets Basehealth to the given value and fills up Health.";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			if (target != null)
			{
				var val = trigger.Text.NextInt(1);
				target.BaseHealth = val;
				target.Heal(target.MaxHealth - target.Health);
			}
		}

		public override ObjectTypeCustom TargetTypes
		{
			get { return ObjectTypeCustom.Unit; }
		}
	}
	#endregion

	#region Resurrect
	public class RaceCommand : RealmServerCommand
	{
		protected RaceCommand() { }

		protected override void Initialize()
		{
			Init("Race", "SetRace");
			EnglishParamInfo = "<race>";
			EnglishDescription = "Sets the Unit's race. Also adds the Race's language.";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var word = trigger.Text.NextWord();
			RaceId race;
			if (EnumUtil.TryParse(word, out race))
			{
				trigger.Args.Target.Race = race;
				if (trigger.Args.Target is Character)
				{
					var desc = LanguageHandler.GetLanguageDescByRace(race);
					((Character)trigger.Args.Target).AddLanguage(desc);
				}
			}
			else
			{
				trigger.Reply("Invalid Race: " + word);
			}
		}

		public override ObjectTypeCustom TargetTypes
		{
			get
			{
				return ObjectTypeCustom.Unit;
			}
		}
	}
	#endregion

	#region Invul
	public class InvulModeCommand : RealmServerCommand
	{
		protected InvulModeCommand() { }

		protected override void Initialize()
		{
			Init("Invul");
			EnglishParamInfo = "[0|1]";
			EnglishDescription = "Toggles Invulnerability";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			var mode = trigger.Text.NextBool(!target.IsInvulnerable);
			target.IsInvulnerable = mode;
			trigger.Reply("{0} is now " + (mode ? "Invulnerable" : "Vulnerable"), target.Name);
		}

		public override ObjectTypeCustom TargetTypes
		{
			get
			{
				return ObjectTypeCustom.Unit;
			}
		}
	}
	#endregion

	#region Talking
	public class TalkCommand : RealmServerCommand
	{
		protected TalkCommand() { }
		public override WCell.Intercommunication.DataTypes.RoleStatus RequiredStatusDefault
		{
			get { return WCell.Intercommunication.DataTypes.RoleStatus.Staff; }
		}
		protected override void Initialize()
		{
			Init("Talk");
			EnglishParamInfo = "<text>";
			EnglishDescription = "say something in the top of the screen";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			var text = trigger.Text.Remainder.Trim();
			var color = Color.Red;
			target.Talk(text, color);
		}

		public override ObjectTypeCustom TargetTypes
		{
			get
			{
				return ObjectTypeCustom.All;
			}
		}
	}

	public class SayCommand : RealmServerCommand
	{
		protected SayCommand() { }

		protected override void Initialize()
		{
			Init("Say");
			EnglishParamInfo = "<text>";
			EnglishDescription = "Say something";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			var text = trigger.Text.Remainder.Trim();
			target.Say(text);
		}

		public override ObjectTypeCustom TargetTypes
		{
			get
			{
				return ObjectTypeCustom.All;
			}
		}
	}

	public class YellCommand : RealmServerCommand
	{
		protected YellCommand() { }

		protected override void Initialize()
		{
			Init("Yell");
			EnglishParamInfo = "<text>";
			EnglishDescription = "Yell something";
		}

		public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
		{
			var target = trigger.Args.Target;
			var text = trigger.Text.Remainder.Trim();
			//target.Yell(text);
			/*if (text == "guard")
			{*/

			//}
			if (text == "testgm")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)5091);
				packet2.WriteInt(2);
				packet2.WriteInt(2);
				packet2.WriteInt(2);
				packet2.WriteInt(2);
				packet2.WriteString("test" ,20);
				packet2.WriteInt(2);
				packet2.WriteInt(2);
				target.CharacterMaster.Send(packet2, true);
				return;
			}
			if (text == "testguild")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteSkip(StrToByteArray("010100030000360150616E676F6C696E730000000000000000060000004279796B6100000000000000000000000000000000010500000000000000004EFFFF00000000000000FF00000000330000000000000000A8FFFF000000000100030000C600496E73616E6500000000000000000000001B0000004B616F6C696E000000000000000000000000000000010B0000000000000000174AFF00000000FFFFFFFF000000002D00000000000000000000FF000000000100090000C600496E73616E6500000000000000000000000F0000004B616F6C696E000000000000000000000000000000010B0000000000000000174AFF00000000FFFFFFFF000000002D00000000000000000000FF00000000FFFF000000FFFF0000000000000000000000000000000000FFFFFFFF00000000000000000000000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")); // Guild Crest (40 bytes)

				target.CharacterMaster.Send(packet2, true);
				return;

			}

			if (text == "testguild2")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteByte(1);                           // tax applies on char? 1 = yes, 0 = no
				for (int i = 0; i < 3; i++)
				{
					packet2.WriteInt16(1);                      // faction
					packet2.WriteInt32(6);                      // tax
					packet2.WriteInt16(0);                       // idk: always is 0
					packet2.WriteAsdaString("a1234567890z", 14);        // Guild name
																		//packet2.WriteInt32(11000201);                      // idk: value is 6 or 27 or 15
					packet2.WriteAsdaString("1234567890123456789012345678901234567890123456789012345678", 58);    // Char name
																												  //packet2.WriteInt32(1);                       // idk
																												  //packet2.WriteByte(1);                       // Have Crest? 1 = yes, 0 = no
					packet2.WriteSkip(StrToByteArray("00000000A8FFFF00000000")); // Guild Crest (40 bytes)
				}
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			if (text == "testguild3")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteByte(1);                           // tax applies on char? 1 = yes, 0 = no
				for (int i = 0; i < 4; i++)
				{
					packet2.WriteInt16(1);                      // faction
					packet2.WriteInt16(3);                      // tax
					packet2.WriteByte(0);                       // idk: always is 0
					packet2.WriteUInt16(12);                    // guildid
					packet2.WriteAsdaString("test", 17);        // Guild name
					packet2.WriteInt32(11000201);                      // idk: value is 6 or 27 or 15
					packet2.WriteAsdaString("Rhaegar", 21);    // Char name
					packet2.WriteByte(1);                       // Have Crest? 1 = yes, 0 = no
					packet2.WriteSkip(StrToByteArray("0500000000000000004EFFFF00000000000000FF00000000330000000000000000A8FFFF00000000")); // Guild Crest (40 bytes)
				}
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			if (text == "testguild4")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteByte(1);                           // tax applies on char? 1 = yes, 0 = no
				for (int i = 0; i < 3; i++)
				{
					packet2.WriteInt16(target.CharacterMaster.Asda2FactionId);                      // faction
					packet2.WriteInt16(1);                      // tax
					packet2.WriteInt32(target.CharacterMaster.Guild.Id);                       // idk: always is 0
					packet2.WriteAsdaString(target.CharacterMaster.Guild.Name, 18);        // Guild name
					packet2.WriteInt32(target.CharacterMaster.Guild.Leader.Character.SessionId);                       // idk: always is 0
					packet2.WriteAsdaString(target.CharacterMaster.Guild.Leader.Name, 20);    // Char name
					packet2.WriteByte(0);                       // Have Crest? 1 = yes, 0 = no
					packet2.WriteSkip(target.CharacterMaster.Guild.ClanCrest); // Guild Crest (40 bytes)
				}
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			if (text == "testguild5")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteByte(1);                           // tax applies on char? 1 = yes, 0 = no
				for (int i = 0; i < 3; i++)
				{
					packet2.WriteInt16(target.CharacterMaster.Asda2FactionId);                      // faction
					packet2.WriteInt16(6);                      // tax
					packet2.WriteByte(0);
					packet2.WriteByte(1);
					packet2.WriteInt16(target.CharacterMaster.Guild.Leader.Character.SessionId);                       // idk: always is 0
					packet2.WriteAsdaString(target.CharacterMaster.Guild.Leader.Name, 17);        // Guild name
					packet2.WriteByte(0);
					packet2.WriteInt32(target.CharacterMaster.Guild.Leader.Character.SessionId);                       // idk: always is 0
					packet2.WriteAsdaString(target.CharacterMaster.Guild.Name, 20);    // Char name
					packet2.WriteByte(1);                       // Have Crest? 1 = yes, 0 = no
					packet2.WriteSkip(target.CharacterMaster.Guild.ClanCrest); // Guild Crest (40 bytes)
				}
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			if (text == "testguild6")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)6743);
				packet2.WriteSkip(StrToByteArray("0100000B00000A0050737963686F7300000000000000000000144000000BCAE3E6DFE500000000000000000000000000000001160000000000000000B1FFFF00000000FFFFFFFF00000000250000000000000000B4FFFF0000000000000F000008004272696768740000000000000000000000E51700000B4D696E7479000000000000000000000000000000010B00000000000000784E06FF00000000F9F9EAFF000000001100000000000000695100FF0000000000000500002A004E69676874526169640000000000000000BE1300000B444A415A4149524900000000000000000000000001040000000000000056A8FFFF00000000030000FF0000000018000000000000004EACFFFF00000000FFFF000000FFFF0000000000000000000000000000000000FFFFFFFF0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000FE"));
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			
			// Candy Gifts
			if (text == "testgift")
			{
				var packet2 = new RealmPacketOut((RealmServerOpCode)8915);
				packet2.WriteSkip(StrToByteArray("D30000725E110000FE"));
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			
			if (text == "quest")
			{
				var status = 0;
				var questidbyfilename = 2092;
				var questid = 90;
				using (var packet = new RealmPacketOut(RealmServerOpCode.AcceptQuestRes))
				{
					packet.WriteByte(status); // status
					packet.WriteInt16(questidbyfilename); // questIdByFileName
					packet.WriteInt32(questid); // questId
					packet.WriteInt32(questidbyfilename); // questIdByFileName
					packet.WriteInt32(questidbyfilename); // questIdByFileName
				target.CharacterMaster.Send(packet, true);
				}
				return;

			}
			if (text == "testquest")
			{
				var packet2 = new RealmPacketOut(RealmServerOpCode.AcceptQuestRes);
				packet2.WriteByte(0); // status
				packet2.WriteInt16(90); // Quest Id value:90
				packet2.WriteInt32(2092); // Quest File Id value:2092
				packet2.WriteByte(4); // idk always : 4
				packet2.WriteInt16(0); // idk value : 0
				packet2.WriteByte(2); // idk always : 4
				packet2.WriteInt32(0); // Next Quest ID ?   value : 91
				
				//packet2.WriteSkip(StrToByteArray("2A3EFFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF0000FFFFFFFF0000"));
				target.CharacterMaster.Send(packet2, true);
				return;

			}
			if (text == "testwar")
			{
				for (int i = 0; i < 2; i++)
				{

					using (var packet = new RealmPacketOut(RealmServerOpCode.WarEnded)) //6732
					{
						packet.WriteInt16(1); //{winingFaction}default value : 1 Len : 2
						packet.WriteByte(1); // Faction id
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						packet.WriteByte(2); 
						/*packet.WriteInt16(2); 
						packet.WriteInt16(1); // points?
						packet.WriteInt16(0); 
						packet.WriteInt16(1); 
						packet.WriteInt16(2);*/ 
											  
						
						if (i == 0)
							Console.WriteLine(packet.ToHexDump());
						else
							target.CharacterMaster.Send(packet, addEnd: true);
					}
				}
				return;

			}

		}
		private static readonly byte[] stab35 = new byte[] { 0x00, 0xFF, 0xFF };

		private static readonly byte[] stab46 = new byte[]
													{0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

		public override ObjectTypeCustom TargetTypes
		{
			get
			{
				return ObjectTypeCustom.All;
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
	}
	#endregion
}