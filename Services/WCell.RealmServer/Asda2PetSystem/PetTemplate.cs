using System.Collections.Generic;
using Castle.ActiveRecord;
using WCell.Util;
using WCell.Util.Data;

namespace WCell.RealmServer.Asda2PetSystem
{
    [DataHolder]
    public class PetTemplate : IDataHolder
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int EvolutionStep { get; set; }

        public int Rank { get; set; }

        public int Rarity { get; set; }

        public int MaxLevel { get; set; }

        public int Bonus1Type { get; set; }

        public int Bonus2Type { get; set; }

        public int Bonus3Type { get; set; }

        [Property]
        [Persistent(Length = 5)]
        public int[] WhiteByRankPetIds { get; set; }
        [Property]
        [Persistent(Length = 5)]
        public int[] YellowByRankPetIds { get; set; }
        [Property]
        [Persistent(Length = 5)]
        public int[] PurpleByRankPetIds { get; set; }
        [Property]
        [Persistent(Length = 5)]
        public int[] GreenByRankPetIds { get; set; }

        public int MinimumUsingLevel
        {
            get {
                switch (Rank)
                {
                    default:
                        return 1;
                    case 1:
                        return 20;
                    case 2:
                        return 40;
                    case 3:
                        return 60;
                    case 4:
                        return 80;
                    case 5:
                        return 100;
                }
            }
        }

        #region Implementation of IDataHolder

        public void FinalizeDataHolder()
        {
            ArrayUtil.SetValue(Asda2PetMgr.PetTemplates,Id,this);
            if(!Asda2PetMgr.PetTemplatesByRankAndRarity.ContainsKey(Rank))
                Asda2PetMgr.PetTemplatesByRankAndRarity.Add(Rank,new Dictionary<int, Dictionary<string, PetTemplate>>());
            if(!Asda2PetMgr.PetTemplatesByRankAndRarity[Rank].ContainsKey(Rarity))
                Asda2PetMgr.PetTemplatesByRankAndRarity[Rank].Add(Rarity,new Dictionary<string, PetTemplate>());
            if(Asda2PetMgr.PetTemplatesByRankAndRarity[Rank][Rarity].ContainsKey(Name))
                return;
            Asda2PetMgr.PetTemplatesByRankAndRarity[Rank][Rarity].Add(Name,this);
        }

        #endregion

        public PetTemplate GetEvolutionTemplate(int rarity, int rank)
        {
            switch (rarity)
            {
                case 1:
                    return Asda2PetMgr.PetTemplates.Get(YellowByRankPetIds[rank]);
                case 2:
                    return Asda2PetMgr.PetTemplates.Get(PurpleByRankPetIds[rank]);
                case 3:
                    return Asda2PetMgr.PetTemplates.Get(GreenByRankPetIds[rank]);
                default:
                    return Asda2PetMgr.PetTemplates.Get(WhiteByRankPetIds[rank]);
            }
        }

        public PetTemplate GetPetTemplateId(int petid)
        {
            switch (petid)
            {
                case 21:
                    return Asda2PetMgr.PetTemplates.Get(21);
                case 22:
                    return Asda2PetMgr.PetTemplates.Get(22);
                case 23:
                    return Asda2PetMgr.PetTemplates.Get(23);
                case 24:
                    return Asda2PetMgr.PetTemplates.Get(24);
                case 25:
                    return Asda2PetMgr.PetTemplates.Get(25);
                case 26:
                    return Asda2PetMgr.PetTemplates.Get(26);
                case 27:
                    return Asda2PetMgr.PetTemplates.Get(27);
                case 28:
                    return Asda2PetMgr.PetTemplates.Get(28);
                case 29:
                    return Asda2PetMgr.PetTemplates.Get(29);
                default:
                    return Asda2PetMgr.PetTemplates.Get(30);
            }
        }
    }
}