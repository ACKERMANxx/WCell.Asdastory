using System;
using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.Util.Commands;
using WCell.RealmServer.Network;
using WCell.Intercommunication.DataTypes;

namespace WCell.RealmServer.Commands
{
    public class OptionCommand : RealmServerCommand
    {
        protected OptionCommand()
        {
        }
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("op");
            EnglishParamInfo = "";
            EnglishDescription =
              "";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            Character target = (Character)trigger.Args.Target;
            var option = trigger.Text.NextInt();
            var item = target.Asda2Inventory.ShopItems[0];
            item.Parametr1Type = (Asda2ItemBonusType)option;
            item.Parametr2Type = (Asda2ItemBonusType)option + 1;
            item.Parametr3Type = (Asda2ItemBonusType)option + 2;
            item.Parametr4Type = (Asda2ItemBonusType)option + 3;
            item.Parametr5Type = (Asda2ItemBonusType)option + 4;
            Handlers.Asda2InventoryHandler.UpdateItemInventoryInfo(target.Client, item);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get { return ObjectTypeCustom.Player; }
        }
    }
}
