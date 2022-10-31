using System;
using WCell.Constants.Updates;
using WCell.RealmServer.Entities;
using WCell.Util.Commands;
using WCell.RealmServer.Network;
using WCell.Intercommunication.DataTypes;

namespace WCell.RealmServer.Commands
{
    public class EnchantCommand : RealmServerCommand
    {
        protected EnchantCommand()
        {
        }
        public override RoleStatus RequiredStatusDefault
        {
            get { return RoleStatus.Player; }
        }

        protected override void Initialize()
        {
            Init("enchant", "enc", "ench", "upgrade", "up");
            EnglishParamInfo = "";
            EnglishDescription =
              "";
        }

        public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
        {
            Character target = (Character)trigger.Args.Target;
            var enchant = trigger.Text.NextInt();
            var enclv = trigger.Text.NextInt();
            var item = target.Asda2Inventory.ShopItems[29];
            item.Enchant = (byte)enchant;
            item.EnchantLevel = (byte)enclv;
            Handlers.Asda2InventoryHandler.UpdateItemInventoryInfo(target.Client, item);
        }

        public override ObjectTypeCustom TargetTypes
        {
            get { return ObjectTypeCustom.Player; }
        }
    }
}
