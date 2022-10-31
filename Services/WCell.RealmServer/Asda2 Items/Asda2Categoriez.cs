using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCell.RealmServer.Asda2_Items
{
    public enum Catz
    {
        Gold = 13,
        HealthStar = 1,
        HealthPosition = 0,
        PowerPosition = 2,
        PowerStar = 3,
        Zaka2Position = 4,
        F3alPosition = 5,
        ReturnScroll = 16,
        ReviveScroll = 15,
        PremiumShavor = 34,
        Unkowen = 17,
        SummonTool = 150,
        TheCoin = 202,
        DragonJwelwer = 200,
        ThePremCoin = 66,
        BetterFlyPaper = 67,
        HelmetCotton = 68,
        Sowel = 65,
        mkbadelsief = 62,
        partofsowrd = 61,
        Dora = 64,
        SandrelaShoe = 60,
        EleidCards = 14,
        NormalUpgradeD = 70,
        NormalUpgradeC = 71,
        NormalUpgradeB = 72,
        NormalUpgradeA = 73,
        NormalUpgradeS = 74,
        SuperUpgradeD = 75,
        SuperUpgradeC = 76,
        SuperUpgradeB = 77,
        SuperUpgradeA = 78,
        SuperUpgradeS = 79,
        ANormalUpgradeD = 80,
        ANormalUpgradeC = 81,
        ANormalUpgradeB = 82,
        ANormalUpgradeA = 83,
        ANormalUpgradeS = 84,
        ASuperUpgradeD = 85,
        ASuperUpgradeC = 86,
        ASuperUpgradeB = 87,
        ASuperUpgradeA = 88,
        ASuperUpgradeS = 89,
        Unkowen1 = 91,
        InvBallet = 63,
        Shovel = 33,
        Sowel1 = 31,
        Sowel2 = 32,
        PetEgg = 109,
        Hatcher = 107,
        PetFood = 106,
        PetRevive = 105,
        PetXP = 110,
        Unkowen2 = 111,
        XpTool = 149,
        SowelWater = 44,
        SowelWaterMid = 45,
        SowelWaterHigh = 46,
        SowelWaterR = 47,
        Water1 = 48,
        Water2 = 9,
        Water3 = 49,
        Water4 = 50,
        Water5 = 51,
        Water6 = 52,
        Water7 = 53,
        APowerUpgradeA = 101,
        Unkowen3 = 201,
        Fish = 205,
        snara = 92,
        PowerUpgradeD = 93,
        PowerUpgradeC = 94,
        PowerUpgradeB = 95,
        PowerUpgradeA = 96,
        PowerUpgradeS = 97,
        APowerUpgradeD = 98,
        APowerUpgradeC = 99,
        APowerUpgradeB = 100,
        FishBook = 210,
        snara2 = 208,
        snara3 = 209,
        mlabsradi2a = 0,
        Glove = 0,
        bntal = 0,
        Shoe = 0,
        Helmet = 0,
        BoyHelmet = 2,
        Unkowen4 = 1,
        Arrow = 1,
        CrossArrow = 2,
        BallistaArrow = 3,
        Fisho = 204,
        Unkowen5 = 36,
        qlada = 0,
        Ring = 5,
        qarorared = 13,
        IceGlovers = 1,
        Bow = 10,
        TwoHandSword = 3,
        Spear = 2,
        qasba = 5,
        CrossBow = 9,
        Ballista = 11,
        Unkowen6 = 66,
        GuildGift = 0,
        Ters = 0,
        Unkowen7 = 101

    }
    public class Asda2Categoriez
    {
        public static Dictionary<int, List<Asda2Categoriez>> Categoriez = new Dictionary<int, List<Asda2Categoriez>>();
        public int Slot;
        public Catz Category;
        public Asda2Categoriez(int _slot , Catz _cat)
        {
            Slot = _slot;
            Category = _cat;
            if (Categoriez.ContainsKey(_slot))
            {
                Categoriez[_slot].Add(this);
            }
            else {
                Categoriez.Add(_slot, new List<Asda2Categoriez>());
                Categoriez[_slot].Add(this);
            }
        }
        public static Asda2Categoriez HealthStar = new Asda2Categoriez(11, Catz.HealthStar);
        public static Asda2Categoriez Gold = new Asda2Categoriez(11, Catz.Gold);
        public static Asda2Categoriez HealthPosition = new Asda2Categoriez(11, Catz.HealthPosition);
        public static Asda2Categoriez PowerPosition = new Asda2Categoriez(11, Catz.PowerPosition);
        public static Asda2Categoriez Zaka2Position = new Asda2Categoriez(11, Catz.Zaka2Position);
        public static Asda2Categoriez F3alPosition = new Asda2Categoriez(11, Catz.F3alPosition);
        public static Asda2Categoriez ReturnScroll = new Asda2Categoriez(11, Catz.ReturnScroll);
        public static Asda2Categoriez ReviveScroll = new Asda2Categoriez(11, Catz.ReviveScroll);
        public static Asda2Categoriez PremiumShavor = new Asda2Categoriez(11, Catz.PremiumShavor);
        public static Asda2Categoriez Unkowen = new Asda2Categoriez(11, Catz.Unkowen);
        public static Asda2Categoriez SummonTool = new Asda2Categoriez(11, Catz.SummonTool);
        public static Asda2Categoriez TheCoin = new Asda2Categoriez(11, Catz.TheCoin);
        public static Asda2Categoriez DragonJwelwer = new Asda2Categoriez(11, Catz.DragonJwelwer);
        public static Asda2Categoriez ThePremCoin = new Asda2Categoriez(11, Catz.ThePremCoin);
        public static Asda2Categoriez BetterFlyPaper = new Asda2Categoriez(11, Catz.BetterFlyPaper);
        public static Asda2Categoriez HelmetCotton = new Asda2Categoriez(11, Catz.HelmetCotton);
        public static Asda2Categoriez Sowel = new Asda2Categoriez(11, Catz.Sowel);
        public static Asda2Categoriez mkabadelseif = new Asda2Categoriez(11, Catz.mkbadelsief);
        public static Asda2Categoriez partofsword = new Asda2Categoriez(11, Catz.partofsowrd);
        public static Asda2Categoriez Dora = new Asda2Categoriez(11, Catz.Dora);
        public static Asda2Categoriez SandrelaShoe = new Asda2Categoriez(11, Catz.SandrelaShoe);
        public static Asda2Categoriez EleidCard = new Asda2Categoriez(11, Catz.EleidCards);
        public static Asda2Categoriez NormalUpgradeD = new Asda2Categoriez(11, Catz.NormalUpgradeD);
        public static Asda2Categoriez NormalUpgradeC = new Asda2Categoriez(11, Catz.NormalUpgradeC);
        public static Asda2Categoriez NormalUpgradeB = new Asda2Categoriez(11, Catz.NormalUpgradeB);
        public static Asda2Categoriez NormalUpgradeA = new Asda2Categoriez(11, Catz.NormalUpgradeA);
        public static Asda2Categoriez NormalUpgradeS = new Asda2Categoriez(11, Catz.NormalUpgradeS);
        public static Asda2Categoriez SuperUpgradeD = new Asda2Categoriez(11, Catz.SuperUpgradeD);
        public static Asda2Categoriez SuperUpgradeC = new Asda2Categoriez(11, Catz.SuperUpgradeC);
        public static Asda2Categoriez SuperUpgradeB = new Asda2Categoriez(11, Catz.SuperUpgradeB);
        public static Asda2Categoriez SuperUpgradeA = new Asda2Categoriez(11, Catz.SuperUpgradeA);
        public static Asda2Categoriez SuperUpgradeS = new Asda2Categoriez(11, Catz.SuperUpgradeS);
        public static Asda2Categoriez ANormalUpgradeD = new Asda2Categoriez(11, Catz.ANormalUpgradeD);
        public static Asda2Categoriez ANormalUpgradeC = new Asda2Categoriez(11, Catz.ANormalUpgradeC);
        public static Asda2Categoriez ANormalUpgradeB = new Asda2Categoriez(11, Catz.ANormalUpgradeB);
        public static Asda2Categoriez ANormalUpgradeA = new Asda2Categoriez(11, Catz.ANormalUpgradeA);
        public static Asda2Categoriez ANormalUpgradeS = new Asda2Categoriez(11, Catz.ANormalUpgradeS);
        public static Asda2Categoriez ASuperUpgradeD = new Asda2Categoriez(11, Catz.ASuperUpgradeD);
        public static Asda2Categoriez ASuperUpgradeC = new Asda2Categoriez(11, Catz.ASuperUpgradeC);
        public static Asda2Categoriez ASuperUpgradeB = new Asda2Categoriez(11, Catz.ASuperUpgradeB);
        public static Asda2Categoriez ASuperUpgradeA = new Asda2Categoriez(11, Catz.ASuperUpgradeA);
        public static Asda2Categoriez ASuperUpgradeS = new Asda2Categoriez(11, Catz.ASuperUpgradeS);
        public static Asda2Categoriez Unkowen1 = new Asda2Categoriez(11, Catz.Unkowen1);
        public static Asda2Categoriez InvBallet = new Asda2Categoriez(11, Catz.InvBallet);
        public static Asda2Categoriez Shovel = new Asda2Categoriez(11, Catz.Shovel);
        public static Asda2Categoriez Sowel1 = new Asda2Categoriez(11, Catz.Sowel1);
        public static Asda2Categoriez Sowel2 = new Asda2Categoriez(11, Catz.Sowel2);
        public static Asda2Categoriez PetEgg = new Asda2Categoriez(11, Catz.PetEgg);
        public static Asda2Categoriez Hatcher = new Asda2Categoriez(11, Catz.Hatcher);
        public static Asda2Categoriez PetFood = new Asda2Categoriez(11, Catz.PetFood);
        public static Asda2Categoriez PetRevive = new Asda2Categoriez(11, Catz.PetRevive);
        public static Asda2Categoriez PetXP = new Asda2Categoriez(11, Catz.PetXP);
        public static Asda2Categoriez Unkowen2 = new Asda2Categoriez(11, Catz.Unkowen2);
        public static Asda2Categoriez XPTool = new Asda2Categoriez(11, Catz.XpTool);
        public static Asda2Categoriez SowelWater = new Asda2Categoriez(11, Catz.SowelWater);
        public static Asda2Categoriez SowelWaterMid = new Asda2Categoriez(11, Catz.SowelWaterMid);
        public static Asda2Categoriez SowelWaterHigh = new Asda2Categoriez(11, Catz.SowelWaterHigh);
        public static Asda2Categoriez SowelWaterR = new Asda2Categoriez(11, Catz.SowelWaterR);
        public static Asda2Categoriez Water1 = new Asda2Categoriez(11, Catz.Water1);
        public static Asda2Categoriez Water2 = new Asda2Categoriez(11, Catz.Water2);
        public static Asda2Categoriez Water3 = new Asda2Categoriez(11, Catz.Water3);
        public static Asda2Categoriez Water4 = new Asda2Categoriez(11, Catz.Water4);
        public static Asda2Categoriez Water5 = new Asda2Categoriez(11, Catz.Water5);
        public static Asda2Categoriez Water6 = new Asda2Categoriez(11, Catz.Water6);
        public static Asda2Categoriez Water7 = new Asda2Categoriez(11, Catz.Water7);
        public static Asda2Categoriez APowerUpgradeA = new Asda2Categoriez(11, Catz.APowerUpgradeA);
        public static Asda2Categoriez Unkowen3 = new Asda2Categoriez(11, Catz.Unkowen3);
        public static Asda2Categoriez Fish = new Asda2Categoriez(11, Catz.Fish);
        public static Asda2Categoriez Snara = new Asda2Categoriez(11, Catz.snara);
        public static Asda2Categoriez PowerUpgradeD = new Asda2Categoriez(11, Catz.PowerUpgradeD);
        public static Asda2Categoriez PowerUpgradeC = new Asda2Categoriez(11, Catz.PowerUpgradeC);
        public static Asda2Categoriez PowerUpgradeB = new Asda2Categoriez(11, Catz.PowerUpgradeB);
        public static Asda2Categoriez PowerUpgradeA = new Asda2Categoriez(11, Catz.PowerUpgradeA);
        public static Asda2Categoriez PowerUpgradeS = new Asda2Categoriez(11, Catz.PowerUpgradeS);
        public static Asda2Categoriez APowerUpgradeD = new Asda2Categoriez(11, Catz.APowerUpgradeD);
        public static Asda2Categoriez APowerUpgradeC = new Asda2Categoriez(11, Catz.APowerUpgradeC);
        public static Asda2Categoriez APowerUpgradeB = new Asda2Categoriez(11, Catz.APowerUpgradeB);
        public static Asda2Categoriez FishBook = new Asda2Categoriez(11, Catz.FishBook);
        public static Asda2Categoriez Snara2 = new Asda2Categoriez(11, Catz.snara2);
        public static Asda2Categoriez Snara3 = new Asda2Categoriez(11, Catz.snara3);
        public static Asda2Categoriez Mlabs = new Asda2Categoriez(1, Catz.mlabsradi2a);
        public static Asda2Categoriez Glove = new Asda2Categoriez(4, Catz.Glove);
        public static Asda2Categoriez bntal = new Asda2Categoriez(2, Catz.bntal);
        public static Asda2Categoriez Shoe = new Asda2Categoriez(3, Catz.Shoe);
        public static Asda2Categoriez Helemet = new Asda2Categoriez(0, Catz.Helmet);
        public static Asda2Categoriez BHelemet = new Asda2Categoriez(0, Catz.BoyHelmet);
        public static Asda2Categoriez Unkowen4 = new Asda2Categoriez(0, Catz.Unkowen4);
        public static Asda2Categoriez Arrow = new Asda2Categoriez(10, Catz.Arrow);
        public static Asda2Categoriez CArrow = new Asda2Categoriez(10, Catz.CrossArrow);
        public static Asda2Categoriez BArrow = new Asda2Categoriez(10, Catz.BallistaArrow);
        public static Asda2Categoriez Fisho = new Asda2Categoriez(10, Catz.Fisho);
        public static Asda2Categoriez Unkowen5 = new Asda2Categoriez(10, Catz.Unkowen5);
        public static Asda2Categoriez qlada = new Asda2Categoriez(7, Catz.qlada);
        public static Asda2Categoriez Ring = new Asda2Categoriez(5, Catz.Ring);
        public static Asda2Categoriez qarora = new Asda2Categoriez(13, Catz.qarorared);
        public static Asda2Categoriez Ice = new Asda2Categoriez(9, Catz.IceGlovers);
        public static Asda2Categoriez Bow = new Asda2Categoriez(9, Catz.Bow);
        public static Asda2Categoriez TwoHand = new Asda2Categoriez(9, Catz.TwoHandSword);
        public static Asda2Categoriez Spea = new Asda2Categoriez(9, Catz.Spear);
        public static Asda2Categoriez qasba = new Asda2Categoriez(9, Catz.qasba);
        public static Asda2Categoriez Cross = new Asda2Categoriez(9, Catz.CrossBow);
        public static Asda2Categoriez Ballis = new Asda2Categoriez(9, Catz.Ballista);
        public static Asda2Categoriez Unkowen6 = new Asda2Categoriez(9, Catz.Unkowen6);
        public static Asda2Categoriez GuildGift = new Asda2Categoriez(9, Catz.GuildGift);
        public static Asda2Categoriez Ters = new Asda2Categoriez(8, Catz.Ters);
        public static Asda2Categoriez Unkowen7 = new Asda2Categoriez(100, Catz.Unkowen7);

    }
}
