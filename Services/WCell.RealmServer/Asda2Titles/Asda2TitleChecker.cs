using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using WCell.Constants;
using WCell.Constants.Achievements;
using WCell.Constants.Items;
using WCell.Constants.NPCs;
using WCell.Constants.World;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Items;
using WCell.RealmServer.Network;
using WCell.Util.Graphics;
using Map = NHibernate.Mapping.Map;

namespace WCell.RealmServer.Asda2Titles
{
    static class Asda2TitleCheckerHelper
    {


        public static void DiscoverTitle(this Character chr, Asda2TitleId titleId)
        {
            if (chr == null || chr.GetedTitles == null || chr.DiscoveredTitles == null)
                return;

            if (chr.DiscoveredTitles.GetBit((int)titleId))
                return;
            chr.DiscoveredTitles.SetBit((int)titleId);
            Asda2TitlesHandler.SendTitleDiscoveredResponse(chr.Client, (short)titleId);
        }

        public static void GainTitle(this Character chr, Asda2TitleId titleId)
        {
            if (chr == null || chr.GetedTitles == null || chr.DiscoveredTitles == null)
                return;

            if (chr.GetedTitles.GetBit((int)titleId))
                return;
            chr.DiscoveredTitles.UnsetBit((int)titleId);
            chr.GetedTitles.SetBit((int)titleId);
            chr.Asda2TitlePoints += Asda2TitleTemplate.Templates[(int)titleId].Points;
            Asda2TitlesHandler.SendYouGetNewTitleResponse(chr, (short)titleId);
            //Asda2TitlesMgr.OnCharacterTitlePointsUpdate(chr);
            //Asda2TitleChecker.OnTitleCountChanged(chr);
        }


        public static void UpdateTitleCounter(this Character chr, Asda2TitleId titleId, int discoverOn, int getOn, int increaceCounterBy = 1)
        {
            /*if (chr == null || chr.TitleProgress == null || chr.DiscoveredTitles == null) return;

            var counter = chr.TitleProgress.IncreaseCounter(titleId, increaceCounterBy);
            //if (counter < getOn)
              //  chr.SendInfoMsg(string.Format("Title {0} [{1} of {2}]", titleId, counter, getOn));
            if (counter >= discoverOn)
                chr.AsdaStoryDiscoverTitle(titleId);
            if (counter >= getOn)
                chr.AsdaStoryGetTitle(titleId);*/
        }

        public static Character CheckTitle(this Character chr, Asda2TitleId titleId, Func<bool> discoverPredicate, Func<bool> getPredicate)
        {
            /*if (chr == null || chr.TitleProgress == null || chr.DiscoveredTitles == null) return chr;

            if (discoverPredicate())
                chr.AsdaStoryDiscoverTitle(titleId);
            if (getPredicate())
                chr.AsdaStoryGetTitle(titleId);*/
            return chr;
        }

        public static Character CheckTitlesCollection(this Character chr, Asda2TitleId titleGained,
            params Asda2TitleId[] requaredTitles)
        {
            /*if (chr == null || chr.TitleProgress == null || chr.DiscoveredTitles == null) return chr;
            var allGetted = true;
            foreach (var requaredTitle in requaredTitles)
            {
                if (!chr.GetedTitles.GetBit((int)requaredTitle))
                {
                    allGetted = false;
                }
                else
                {
                    chr.AsdaStoryDiscoverTitle(titleGained);
                }
            }

            if (allGetted)
                chr.AsdaStoryGetTitle(titleGained);*/
            return chr;
        }
    }
    static class Asda2TitleChecker
    {
        public static void OnLevelChanged(Character character)
        {
            character
                .CheckTitle((Asda2TitleId)6, () => character.Level >= 5, () => character.Level >= 10)
                .CheckTitle((Asda2TitleId)7, () => character.Level >= 15, () => character.Level >= 20)
                .CheckTitle((Asda2TitleId)8, () => character.Level >= 25, () => character.Level >= 30)
                .CheckTitle((Asda2TitleId)9, () => character.Level >= 35, () => character.Level >= 40)
                .CheckTitle((Asda2TitleId)10, () => character.Level >= 45, () => character.Level >= 50);
        }
        public static void OnDig(Character character)
        {
            character
                .UpdateTitleCounter((Asda2TitleId)11, 50, 100);
            character
                .UpdateTitleCounter((Asda2TitleId)12, 300, 1000);
        }
        public static void OnCraftWeaponArmor(Character character)
        {
            character
                .UpdateTitleCounter((Asda2TitleId)27, 25, 50);
            character
                .UpdateTitleCounter((Asda2TitleId)28, 200, 500);
        }
        public static void OnPickUp(Character character)
        {
            character
                .UpdateTitleCounter((Asda2TitleId)30, 50, 100);
            character
                .UpdateTitleCounter((Asda2TitleId)31, 1500, 3000);
        }
        public static void OnUpgradeSucccess(Character character, Asda2Item item)
        {
            if (item.Template.Quality == Asda2ItemQuality.Purple)
                character.UpdateTitleCounter((Asda2TitleId)72, 50, 100);

        }
        public static void OnUpgradeFail(Character character, Asda2Item item)
        {
                character.UpdateTitleCounter((Asda2TitleId)73, 500, 1000);
        }
        public static void OnDissamble(Character character, Asda2Item resultitem)
        {
            if (resultitem.ItemId == 20958)
            {
                character.UpdateTitleCounter((Asda2TitleId)125, 5, 10);
                character.UpdateTitleCounter((Asda2TitleId)126, 15, 30);
                character.UpdateTitleCounter((Asda2TitleId)127, 35, 55);
                character.UpdateTitleCounter((Asda2TitleId)128, 80, 100);
            }
            if (resultitem.ItemId == 20959)
            {
                character.UpdateTitleCounter((Asda2TitleId)121, 5, 10);
                character.UpdateTitleCounter((Asda2TitleId)122, 15, 30);
                character.UpdateTitleCounter((Asda2TitleId)123, 35, 55);
                character.UpdateTitleCounter((Asda2TitleId)124, 80, 100);
            }
        }
        public static void OnTokenItem(Character character, int itemid, int Amount)
        {
            switch (itemid)
            {
                case 22647: character.UpdateTitleCounter((Asda2TitleId)99, 1, 1200, Amount); break;
                case 22648: character.UpdateTitleCounter((Asda2TitleId)100, 1, 1200, Amount); break;
                case 22649: character.UpdateTitleCounter((Asda2TitleId)101, 1, 1200, Amount); break;
                case 22650: character.UpdateTitleCounter((Asda2TitleId)102, 1, 860, Amount); break;
                case 22651: character.UpdateTitleCounter((Asda2TitleId)103, 1, 2500, Amount); break;
                case 22652: character.UpdateTitleCounter((Asda2TitleId)104, 1, 30, Amount); break;
                case 22653: character.UpdateTitleCounter((Asda2TitleId)105, 1, 30, Amount); break;
                case 22654: character.UpdateTitleCounter((Asda2TitleId)106, 1, 75, Amount); break;
                case 22655: character.UpdateTitleCounter((Asda2TitleId)107, 1, 75, Amount); break;
                case 22656: character.UpdateTitleCounter((Asda2TitleId)108, 1, 870, Amount); break;
                case 22657: character.UpdateTitleCounter((Asda2TitleId)109, 1, 870, Amount); break;
                case 22658: character.UpdateTitleCounter((Asda2TitleId)110, 1, 900, Amount); break;
                case 22659: character.UpdateTitleCounter((Asda2TitleId)111, 1, 780, Amount); break;
                case 22660: character.UpdateTitleCounter((Asda2TitleId)112, 1, 780, Amount); break;
                //case 22661: character.UpdateTitleCounter((Asda2TitleId)113, 1, 100); break; // وادي النسيان
                case 22662: character.UpdateTitleCounter((Asda2TitleId)114, 1, 100, Amount); break;
                case 22663: character.UpdateTitleCounter((Asda2TitleId)115, 1, 100, Amount); break;
                case 22664: character.UpdateTitleCounter((Asda2TitleId)116, 1, 700, Amount); break;
                //case 75461: character.UpdateTitleCounter((Asda2TitleId)73, 1, 0); break; // Event Tokens
                //case 75462: character.UpdateTitleCounter((Asda2TitleId)73, 1, 0); break; // Event Tokens
                //case 76312: character.UpdateTitleCounter((Asda2TitleId)73, 1, 0); break; // Event Tokens
                //case 76313: character.UpdateTitleCounter((Asda2TitleId)73, 1, 0); break; // Event Tokens
                default:
                    break;
            }
        }
        public static void OnFishing(Character character, int fishid, int Length)
        {
            switch (fishid)
            {
                case 69577:/*	السمكة كيوري      */ 
                    character.UpdateTitleCounter((Asda2TitleId)147, 1, 200); 
                    character.UpdateTitleCounter((Asda2TitleId)148, 1, 2000);
                    if (Length >= 85)
                        character.UpdateTitleCounter((Asda2TitleId)149, 1, 1);

                    break;
                case 69578:/*	السمكة النحاسية   */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69579:/*	السمكة سيمو       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69580:/*	السمكة الشقراء    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69581:/*	سمكة القوس قزح    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69582:/*	سمك الشبوط        */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69583:/*	سمك الشبوط المنقطة*/ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69584:/*	سمك الشبوط الغاضب */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69585:/*	شبوطة القرصان     */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69586:/*	الشبوط الخضراء    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69587:/*	السمكة مورا       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69588:/*	السمكة فولاذة      */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69589:/*	السمكة نابو       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69590:/*	السمكة محبوبة     */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69591:/*	السمكة روبي       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69592:/*	ثعبان البحر       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69593:/*	حنكوس             */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69594:/*	حنكوس المشتعلة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69595:/*	حرشوف             */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69596:/*	السمكة اللولبي    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69597:/*	السمكة الغنية     */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69598:/*	السمكة اللذيذة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69599:/*	سمكة سولي         */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69600:/*	السمكة الصامدة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69601:/*	السمكة الجذابة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;

                case 69602:/*	السمكة ماكريل     */ 
                case 69603:/*	ماكريل الحساسة    */ 
                case 69604:/*	ماكريل السحرية    */ 
                case 69605:/*	ماكريل الذكية     */ 
                case 69606:/*	ماكريل القوية     */
                    character.UpdateTitleCounter((Asda2TitleId)162, 80, 200);
                    character.UpdateTitleCounter((Asda2TitleId)163, 500, 2000);
                    if (Length >= 95)
                        character.UpdateTitleCounter((Asda2TitleId)164, 1, 1);
                    break;

                case 69607:/*	تونا              */ 
                case 69608:/*	تونا القوية       */
                case 69609:/*	تونا المغمورة     */
                case 69610:/*	تونا السحرية      */
                case 69611:/*	تونا العجوزة      */ 
                    character.UpdateTitleCounter((Asda2TitleId)165, 80, 200);
                    character.UpdateTitleCounter((Asda2TitleId)166, 500, 2000);
                    if (Length >= 165)
                        character.UpdateTitleCounter((Asda2TitleId)167, 1, 1);
                    break;


                case 69612:/*	السمكة الثمينة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69613:/*	خشوبة             */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69614:/*	السمكة النبيلة    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69615:/*	السمكة عضلة       */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                case 69616:/*	السمكة الخضراء    */ character.UpdateTitleCounter((Asda2TitleId)0, 1, 1200); break;
                default:
                    break;
            }
        }



        public class EmoteChecker
        {
            static readonly Dictionary<string, DateTime> EmoteTimers = new Dictionary<string, DateTime>();

            public static void OnEmote(short emote, Character chr)
            {
                if (EmoteTimers.ContainsKey(chr.Name))
                {
                    var lastTimeEmote = EmoteTimers[chr.Name];
                    if (DateTime.Now.Subtract(lastTimeEmote).TotalMilliseconds < 2000)
                        return;
                    EmoteTimers[chr.Name] = DateTime.Now;
                }
                else
                {
                    EmoteTimers.Add(chr.Name, DateTime.Now);
                }
                if (emote == 113 || emote == 112)
                    OnDancing(chr);
                /*if (emote == 105)
                    OnCrazy(chr);
                if (emote == 104)
                    OnGreet(chr);
                if (emote == 106)
                    OnChlng(chr);
                if (emote == 103)
                    OnThank(chr);
                if (emote == 102)
                    OnSad(chr);
                if (emote == 101)
                    OnAngry(chr);
                if (emote == 100)
                    OnHappy(chr);
                if (emote == 115)
                    OnRomanc(chr);
                if (emote == 116)
                    OnClow(chr);
                if (emote == 117)
                    OnArt(chr);*/

            }
        }
        public static void OnDancing(Character character)
        {
            character.UpdateTitleCounter((Asda2TitleId)13, 300, 3000);
        }
        /*public static void OnCrazy(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Naughty, 100, 500);
        }
        public static void OnGreet(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Greeting, 100, 500);
        }
        public static void OnChlng(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Challenger, 100, 500);
        }
        public static void OnThank(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Thankfull, 100, 500);
        }
        public static void OnSad(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.TheSad, 100, 500);
        }
        public static void OnAngry(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Angry, 100, 500);
        }
        public static void OnHappy(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Happy, 100, 500);
        }
        public static void OnRomanc(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Romantic, 100, 500);
        }
        public static void OnClow(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Clown, 100, 500);
        }
        public static void OnArt(Character character)
        {

            character.UpdateTitleCounter(Asda2TitleId.Acrobatic, 100, 500);
        }*/

    }
}