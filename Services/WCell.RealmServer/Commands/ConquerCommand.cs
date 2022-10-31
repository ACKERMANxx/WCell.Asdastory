using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants;
using WCell.Constants.Updates;
using WCell.Core.Network;
using WCell.Util.Commands;

namespace WCell.RealmServer.Commands
{
    public class FacCommand : RealmServerCommand
    {
        protected FacCommand() { }

        protected override void Initialize()
        {
            Init("fac");
            EnglishParamInfo = "<soul>";
            EnglishDescription = "Fac something";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var target = trigger.Args.Target;
            var soul = trigger.Text.NextInt();
            WCell.RealmServer.Handlers.Asda2SpellHandler.SendShowSoulGuard(target.CharacterMaster, soul);
        }

        private static readonly byte[] guildCrest = new byte[]
                                                        {
                                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                            0x00, 0x00, 0x00, 0x00, 0x00
                                                        };

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

        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.All;
            }
        }
    }

    

    public class ConquerCommand : RealmServerCommand
    {
        protected ConquerCommand() { }

        protected override void Initialize()
        {
            Init("Conquer", "con");
            EnglishParamInfo = "<f> <s> <t>";
            EnglishDescription = "Conquer something";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            var target = trigger.Args.Target;

            var packet2 = new RealmPacketOut((RealmServerOpCode)6743);

            packet2.WriteAsdaString(target.CharacterMaster.Guild.Name, 20);
            target.CharacterMaster.SendErrorMsg("" + packet2.ContentLength);
            target.CharacterMaster.Send(packet2, true);
        }


        public override ObjectTypeCustom TargetTypes
        {
            get
            {
                return ObjectTypeCustom.All;
            }
        }
    }
}
