using WCell.Constants.Updates;
using WCell.RealmServer.Asda2PetSystem;
using WCell.RealmServer.Entities;
using WCell.Util;
using WCell.Util.Commands;

namespace WCell.RealmServer.Commands
{
	public class PetCommand : RealmServerCommand
	{
		protected override void Initialize()
		{
			Init("Pet");
			EnglishDescription = "A set of commands to manage pets.";
		}

        public class PetAddCommand : SubCommand
        {
            public static PetAddCommand Instance { get; private set; }

            protected PetAddCommand()
            {
                Instance = this;
            }

            protected override void Initialize()
            {
                Init("Add", "A");
                EnglishParamInfo = "petId";
                EnglishDescription = "add pet";
            }

            public override void Process(CmdTrigger<RealmServerCmdArgs> trigger)
            {
                var chr = ((Character)trigger.Args.Target);
                var petid = trigger.Text.NextInt(1);
                PetTemplate pet = Asda2PetMgr.PetTemplates.Get(petid);
                chr.AddAsda2Pet(pet);
            }

        }
    }
}