using System;
using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.Util.Commands;
using WCell.RealmServer.Network;
using WCell.Intercommunication.DataTypes;

namespace WCell.RealmServer.Commands
{
    public class faq : RealmServerCommand
    {
        protected faq()
        {
        }
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("faq");
            EnglishParamInfo = "";
            EnglishDescription =
              "";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            Character target = (Character)trigger.Args.Target;
            target.Asda2FactionId = -1;
        }

        public override ObjectTypeCustom TargetTypes
        {
            get { return ObjectTypeCustom.Player; }
        }
    }
}
