using System;
using System.Collections.Generic;
using WCell.Constants.NPCs;
using WCell.Core.Initialization;
using WCell.RealmServer.Content;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.NPCs.Pets;
using WCell.Util;
using WCell.Util.Data;
using WCell.Util.Variables;

namespace WCell.RealmServer.Formulas
{
    /// <summary>
    /// Takes Target-level and receiver-level and returns the amount of base-experience to be gained
    /// </summary>
    public delegate int BaseExperienceCalculator(int targetLvl, int receiverLvl);

    public delegate int ExperienceCalculator(int receiverLvl, NPC npc);

    /// <summary>
    /// </summary>
    /// <returns>The amount of tequired Experience for that level</returns>
    public delegate int XpCalculator(int level);

    //[DataHolder]
    public class LevelXp : IDataHolder
    {
        public int Level, Xp;

        public uint GetId()
        {
            return (uint)Level;
        }

        public DataHolderState DataHolderState { get; set; }

        public void FinalizeDataHolder()
        {
            // ArrayUtil.Set(ref XpGenerator.XpTableForNexlLvl, (uint) Level, Xp);
        }
    }

    /// <summary>
    /// Static utility class that holds and calculates Level- and Experience-information.
    /// Has exchangable Calculator delegates to allow custom xp-calculations.
    /// </summary>
    public static class XpGenerator
    {
        public static float XpRate = 1f;
        /// <summary>
        /// Change this method in addons to create custom XP calculation
        /// </summary>
        public static Action<Character, INamed, int> CombatXpDistributer = DistributeCombatXp;

        /// <summary>
        /// Distributes the given amount of XP over the group of the given Character (or adds it only to the Char, if not in Group).
        /// </summary>
        /// <remarks>Requires Map-Context.</remarks>
        /// <param name="chr"></param>
        public static void DistributeCombatXp(Character chr, INamed killed, int xp)
        {
            if (chr.SoulmateRecord != null)
            {
                chr.SoulmateRecord.OnExpGained(true);
            }
            var groupMember = chr.GroupMember;
            if (groupMember != null)
            {
                var members = new List<Character>();
                var highestLevel = 0;
                var totalLevels = 0;
                groupMember.IterateMembersInRange(WorldObject.BroadcastRange,
                                                  member =>
                                                  {
                                                      var memberChar = member.Character;
                                                      if (memberChar != null)
                                                      {
                                                          totalLevels += memberChar.Level;
                                                          if (memberChar.Level > highestLevel)
                                                          {
                                                              highestLevel = memberChar.Level;
                                                          }
                                                          members.Add(memberChar);
                                                      }
                                                  });

                foreach (var member in members)
                {
                    var share = MathUtil.Divide(xp * member.Level, totalLevels);
                    member.GainCombatXp(share, killed, true);
                }
            }
            else
            {
                chr.GainCombatXp(xp, killed, true);
            }
        }

        /// <summary>
        /// Gets the amount of xp, required to gain this level (from level-1)
        /// </summary>
        public static int GetXpForlevel(int level)
        {
            if (XpTableForNexlLvl.Length >= level)
            {
                return XpTableForNexlLvl[level - 1];
            }
            return 0;
        }

        public static long GetStartXpForLevel(long lvl) //98 Level MAX
        {
            return XpTableLvl[lvl - 1];
        }

        [NotVariable] /// <summary>
                      /// Array of Xp to be gained per level for default levels.
                      /// Can be set to a different Array.
                      /// </summary>
        public static long[] XpTableLvl = new long[]
                                                           {0,40 ,110 ,270 ,569 ,994 ,1720 ,2692 ,3940 ,5494 ,7294 ,9406 ,11950 ,15010 ,19114 ,24160 ,31200 ,40714 ,53139 ,68947 ,88387 ,112102 ,140542 ,174267 ,214061 ,259736 ,312316 ,374071 ,451711 ,540336 ,640696 ,773999 ,949198 ,1180087 ,1479839 ,1866090 ,2372153 ,3023637 ,3866716 ,4949641 ,6331644 ,8044941 ,10163635 ,12791726 ,16008181 ,19947736 ,24874472 ,31005522 ,38630890 ,48037816 ,59580928 ,73790091 ,91165705 ,112408246 ,138230672 ,169501481 ,208385630 ,256460523 ,315773092 ,388398036 ,588373900 ,794373900 ,1044343730 ,1424343730 ,2024343730 ,2864343730 ,3964343730 ,5344343730 ,7024343730 ,8872343730 ,10905143730 ,13141223730 ,15600911730 ,18306568530 ,21282791010 ,22755132243 ,24404154424 ,26251059266 ,28319592690 ,30636350124 ,33231118451 ,36137258976 ,39392136365 ,43037599041 ,47120517237 ,51693385618 ,56814998203 ,62551204299 ,68975755127 ,76171252054 ,84230208612 ,93256239957 ,103365395064 ,114687648783 ,127368572949 ,141571208014 ,157478159287 ,175293944714 ,195247624391 ,217595745629 ,242625641417 ,267655537204 ,292685432991 ,317715328778 ,342745224565 ,367775120353 ,392805016140 ,417834911927 ,442864807714 ,467894703501 ,492924599289 ,517954495076 ,542984390863 ,568014286650 ,593044182437 ,618074078225 ,643103974012 ,668133869799 ,693163765586 ,718193661373 ,743223557161 ,768253452948 ,793283348735 ,818313244522 ,843343140310 ,868373036097 ,893402931884 ,918432827671 ,943462723458 ,968492619246 ,993522515033 ,1018552410820 ,1043582306607 ,1068612202394 ,1093642098182 ,1118671993969 ,1143701889756 ,1168731785543 ,1193761681330 ,1218791577118 ,1243821472905 ,1268851368692 ,1293881264479 ,1318911160266 ,1343941056054 ,1368970951841 ,1394000847628 ,1419030743415 ,1444060639202 ,1469090534990 ,1494120430777 ,};
        [NotVariable]
        /// <summary>
        /// Array of Xp to be gained per level for default levels.
        /// Can be set to a different Array.
        /// </summary>
        public static int[] XpTableForNexlLvl = new int[]
                                                     {0,40 ,70 ,160 ,299 ,425 ,726 ,972 ,1248 ,1554 ,1800 ,2112 ,2544 ,3060 ,4104 ,5046 ,7040 ,9514 ,12425 ,15808 ,19440 ,23715 ,28440 ,33725 ,39794 ,45675 ,52580 ,61755 ,77640 ,88625 ,100360 ,133303 ,175198 ,230890 ,299752 ,386251 ,506063 ,651484 ,843079 ,1082925 ,1382003 ,1713296 ,2118695 ,2628091 ,3216455 ,3939555 ,4926736 ,6131050 ,7625369 ,9406926 ,11543112 ,14209163 ,17375615 ,21242541 ,25822426 ,31270808 ,38884150 ,48074893 ,59312568 ,72624944 ,199975864 ,206000000 ,249969830 ,380000000 ,600000000 ,840000000 ,1100000000 ,1380000000 ,42000000 ,46200000 ,50820000 ,55902000 ,61492200 ,67641420 ,74405562 ,36808531 ,41225555 ,46172621 ,51713336 ,57918936 ,64869208 ,72653513 ,81371935 ,91136567 ,102072955 ,114321710 ,128040315 ,143405152 ,160613771 ,179887423 ,201473914 ,225650784 ,252728878 ,283056343 ,317023104 ,355065877 ,397673782 ,445394636 ,498841992 ,558703031 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,625747395 ,}; public static int[] BaseExpForLvl = { 0, 2, 5, 9, 15, 19, 23, 26, 30, 34, 37, 41, 46, 55, 60, 65, 70, 75, 80, 86, 92, 98, 105, 112, 120, 128, 136, 145, 154, 168, 186, 204, 221, 246, 274, 309, 350, 395, 445, 522, 609, 702, 802, 908, 1071, 1287, 1548, 1849, 2157, 3007, 3966, 4311, 4623, 4908, 5168, 5086, 5860, 6041, 6223, 6396, 6563, 6725, 6882, 7034, 7182, 7326, 7467, 7605, 7741, 7558, 7807, 8094, 8419, 8783, 9187, 9633, 10123, 10659, 11244, 11881, 12574, 13326, 14143, 15027, 15986, 17024, 18148, 19365, 20682, 21637, 23722, 26064, 28695, 31648, 34964 };
        public static int GetBaseExpForLevel(int level)
        {
            var baseXp = 40000;
            if (BaseExpForLvl.Length > level)
                baseXp = BaseExpForLvl[level];
            return (int)(baseXp * XpRate);
        }

        public static int CalcDefaultXp(int receiverlvl, NPC npc)
        {
            var baseXp = GetBaseExpForLevel(npc.Level);
            var boost = 1f;
            switch (npc.Entry.Rank)
            {
                case CreatureRank.Boss:
                    boost = 30f;
                    break;
                case CreatureRank.Elite:
                    boost = 4f;
                    break;
                case CreatureRank.WorldBoss:
                    boost = 150f;
                    break;
                case CreatureRank.Normal:
                    boost = 1f;
                    break;
            }
            var lvlDiff = receiverlvl - npc.Level;
            switch (lvlDiff)
            {
                case 1:
                    boost *= 0.99f;
                    break;
                case 2:
                    boost *= 0.95f;
                    break;
                case 3:
                    boost *= 0.9f;
                    break;
                case 4:
                    boost *= 0.85f;
                    break;
                case 5:
                    boost *= 0.8f;
                    break;
                case 6:
                    boost *= 0.01f;
                    break;
            }
            return (int)(baseXp * boost);
        }
    }
}