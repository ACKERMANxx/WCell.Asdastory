using System;
using System.Collections.Generic;
using System.Linq;
using WCell.Constants;
using WCell.Constants.Achievements;
using WCell.Constants.Login;
using WCell.Constants.Spells;
using WCell.Constants.Updates;
using WCell.Core;
using WCell.Core.Timers;
using WCell.Intercommunication;
using WCell.RealmServer.Achievements;
using WCell.RealmServer.Asda2BattleGround;
using WCell.RealmServer.Asda2Fishing;
using WCell.RealmServer.Asda2Mail;
using WCell.RealmServer.Asda2PetSystem;
using WCell.RealmServer.Asda2Titles;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Auth.Accounts;
using WCell.RealmServer.Battlegrounds;
using WCell.RealmServer.Chat;
using WCell.RealmServer.Commands;
using WCell.RealmServer.Database;
using WCell.RealmServer.Factions;
using WCell.RealmServer.Formulas;
using WCell.RealmServer.Global;
using WCell.RealmServer.Groups;
using WCell.RealmServer.Guilds;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Help.Tickets;
using WCell.RealmServer.Instances;
using WCell.RealmServer.Interaction;
using WCell.RealmServer.Items;
using WCell.RealmServer.Logs;
using WCell.RealmServer.Mail;
using WCell.RealmServer.Misc;
using WCell.RealmServer.Modifiers;
using WCell.RealmServer.Mounts;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.Network;
using WCell.RealmServer.Quests;
using WCell.RealmServer.RacesClasses;
using WCell.RealmServer.Skills;
using WCell.RealmServer.Social;
using WCell.RealmServer.Spells;
using WCell.RealmServer.Spells.Auras;
using WCell.RealmServer.Talents;
using WCell.RealmServer.Taxi;
using WCell.RealmServer.UpdateFields;
using WCell.Util;
using WCell.Util.Graphics;
using WCell.Util.NLog;
using WCell.Util.Threading;
using WCell.RealmServer.Asda2Quests;
using MySql.Data.MySqlClient;

namespace WCell.RealmServer.Entities
{
    // Anything related to creation/login/logout/saving/loading is in this file
    public enum Asda2PereodicActionType
    {
        HpRegen,
        MpRegen,
        HpRegenPrc
    }

    public class PereodicAction
    {
        public Character Chr { get; set; }
        public int Value { get; set; }
        public int CallsNum { get; set; }
        public int Delay { get; set; }
        public int CurrentDelay { get; set; }
        public Asda2PereodicActionType Type { get; set; }

        public int RemainingHeal { get { return CallsNum * Value; } }

        public PereodicAction(Character chr, int value, int callsNum, int delay, Asda2PereodicActionType type)
        {
            Chr = chr;
            Value = value;
            CallsNum = callsNum;
            Delay = delay;
            Type = type;
        }
        public void Update(int dt)
        {
            CurrentDelay -= dt;
            if (CurrentDelay > 0)
                return;
            var times = 1 + (int)((float)-CurrentDelay / Delay);
            CurrentDelay += times * Delay;
            if (times > CallsNum)
                times = CallsNum;
            for (int i = 0; i < times; i++)
            {
                Process();
            }
            CallsNum -= times;
        }
        void Process()
        {
            switch (Type)
            {
                case Asda2PereodicActionType.HpRegen:
                    Chr.Heal(Value);
                    return;
                case Asda2PereodicActionType.HpRegenPrc:
                    Chr.HealPercent(Value);
                    return;
                case Asda2PereodicActionType.MpRegen:
                    Chr.Power += Value;
                    return;
            }
        }
    }

    public partial class Character
    {
        private bool _isPlayerLogout;

        public Dictionary<Asda2PereodicActionType, PereodicAction> PereodicActions =
            new Dictionary<Asda2PereodicActionType, PereodicAction>();

        protected internal void LoadSometypes(RealmAccount acc, CharacterRecord record, IRealmClient client)
        {
            Account = acc;
            m_client = client;
            m_record = record;
            EntityId = EntityId.GetPlayerId(m_record.EntityLowId & 0xFFFFFF);
            m_archetype = ArchetypeMgr.GetArchetype(record.Race, record.Class);
            Race = record.Race;
            Class = record.Class;
            m_spells = PlayerSpellCollection.Obtain(this);
            SpecProfiles = SpecProfile.LoadAllOfCharacter(this);
            if (SpecProfiles.Length == 0)
            {
                SpecProfiles = new[] { SpecProfile.NewSpecProfile(this, 0) };
            }
            if (m_record.CurrentSpecIndex >= SpecProfiles.Length)
            {
                m_record.CurrentSpecIndex = 0;
            }
            SetInt32(UnitFields.LEVEL, m_record.Level);
            m_talents = new PlayerTalentCollection(this);
            ((PlayerSpellCollection)m_spells).LoadSpellsAndTalents();
            _asda2Inventory = new Asda2PlayerInventory(this);
            InitItemsFirst();
            //this.UpdateAllDamages();
            //this.UpdateRangedAttackPower();

            //if (record.JustCreated)
            //{
            //    ModStatsForLevel(m_record.Level);
            //    //BasePower = RegenerationFormulas.GetPowerForLevel(this);
            //    Asda2BaseAgility = CharacterFormulas.StatOnCreation;
            //    Asda2BaseIntellect = CharacterFormulas.StatOnCreation;
            //    Asda2BaseLuck = CharacterFormulas.StatOnCreation;
            //    Asda2BaseSpirit = CharacterFormulas.StatOnCreation;
            //    Asda2BaseStamina = CharacterFormulas.StatOnCreation;
            //    Asda2BaseStrength = CharacterFormulas.StatOnCreation;
            //}
            //else
            //{
            //    BaseHealth = m_record.BaseHealth;
            //    SetBasePowerDontUpdate(m_record.BasePower);
            //    Asda2Strength = m_record.BaseStrength;
            //    Asda2Intellect = m_record.BaseIntellect;
            //    Asda2Agility = m_record.BaseAgility;
            //    Asda2Stamina = m_record.BaseStamina;
            //    Asda2Luck = m_record.BaseLuck;
            //    Asda2Spirit = m_record.BaseSpirit;
            //    /*Asda2BaseSpirit = lvlStats.Spirit;
            //    Asda2BaseStamina = lvlStats.Stamina;
            //    Asda2BaseStrength = lvlStats.Strength;
            //    Asda2BaseIntellect = lvlStats.Intellect;
            //    Asda2BaseAgility = lvlStats.Agility;
            //    Asda2BaseLuck = GetBaseLuck();*/

            //    /*SetBaseStat(StatType.Strength, m_record.BaseStrength);
            //    SetBaseStat(StatType.Stamina, m_record.BaseStamina);
            //    SetBaseStat(StatType.Spirit, m_record.BaseSpirit);
            //    SetBaseStat(StatType.Intellect, m_record.BaseIntellect);
            //    SetBaseStat(StatType.Agility, m_record.BaseAgility);*/

            //    Power = m_record.Power;
            //    SetInt32(UnitFields.HEALTH, m_record.Health);
            //}
            //UpdateAsda2Agility();
            //UpdateAsda2Stamina();
            //UpdateAsda2Luck();
            //UpdateAsda2Spirit();
            //UpdateAsda2Intellect();
            //UpdateAsda2Strength();

        }
        #region Creation

        /// <summary>
        /// Creates a new character and loads all required character data from the database
        /// </summary>
        /// <param name="acc">The account the character is associated with</param>
        /// <param name="record">The name of the character to load</param>
        /// <param name="client">The client to associate with this character</param>
        protected internal void Create(RealmAccount acc, CharacterRecord record, IRealmClient client)
        {
            client.ActiveCharacter = this;
            acc.ActiveCharacter = this;

            Type |= ObjectTypes.Player;
            ChatChannels = new List<ChatChannel>(5);

            m_logoutTimer = new TimerEntry(0, DefaultLogoutDelayMillis, totalTime => FinishLogout());

            Account = acc;
            m_client = client;

            m_record = record;
            EntityId = EntityId.GetPlayerId(m_record.EntityLowId & 0xFFFFFF);
            m_name = m_record.Name;

            Archetype = ArchetypeMgr.GetArchetype(record.Race, record.Class);
            MainWeapon = GenericWeapon.Fists;
            PowerType = m_archetype.Class.DefaultPowerType;

            StandState = StandState.Sit;

            Money = (uint)m_record.Money;
            Outfit = m_record.Outfit;
            //ScaleX = m_archetype.Race.Scale;
            ScaleX = 1;
            Gender = m_record.Gender;
            Skin = m_record.Skin;
            Facial = m_record.Face;
            HairStyle = m_record.HairStyle;
            HairColor = m_record.HairColor;
            FacialHair = m_record.FacialHair;
            UnitFlags = UnitFlags.PlayerControlled;
            Experience = m_record.Xp;
            RestXp = m_record.RestXp;

            SetInt32(UnitFields.LEVEL, m_record.Level);
            // cannot use Level property, since it will trigger certain events that we don't want triggered
            NextLevelXP = XpGenerator.GetXpForlevel(m_record.Level + 1);
            MaxLevel = RealmServerConfiguration.MaxCharacterLevel;

            RestState = RestState.Normal;

            Orientation = m_record.Orientation;

            m_bindLocation = new WorldZoneLocation(
                m_record.BindMap,
                new Vector3(m_record.BindX, m_record.BindY, m_record.BindZ),
                m_record.BindZone);

            PvPRank = 1;
            YieldsXpOrHonor = true;

            foreach (var school in SpellConstants.AllDamageSchools)
            {
                SetFloat(PlayerFields.MOD_DAMAGE_DONE_PCT + (int)school, 1);
            }
            SetFloat(PlayerFields.DODGE_PERCENTAGE, 1.0f);

            // Auras
            m_auras = new PlayerAuraCollection(this);

            // spells
            m_spells = PlayerSpellCollection.Obtain(this);

            // factions
            WatchedFaction = m_record.WatchedFaction;
            Faction = NPCMgr.DefaultFaction; //.ByRace[(uint)record.Race];
            m_reputations = new ReputationCollection(this);

            // skills
            m_skills = new SkillCollection(this);

            // talents
            m_talents = new PlayerTalentCollection(this);

            // achievements
            m_achievements = new AchievementCollection(this);

            // Items
            //m_inventory = new PlayerInventory(this);
            _asda2Inventory = new Asda2PlayerInventory(this);

            m_mailAccount = new MailAccount(this);

            m_questLog = new QuestLog(this);

            // tutorial flags
            TutorialFlags = new TutorialFlags(m_record.TutorialFlags);

            // Make sure client and internal state is updated with combat base values
            UnitUpdates.UpdateSpellCritChance(this);

            // Mask of activated TaxiNodes
            m_taxiNodeMask = new TaxiNodeMask();

            PowerCostMultiplier = 1f;

            m_lastPlayTimeUpdate = DateTime.Now;

            MoveControl.Mover = this;
            MoveControl.CanControl = true;

            IncMeleePermissionCounter();

            SpeedFactor = DefaultSpeedFactor;

            // basic setup
            if (record.JustCreated)
            {
                ModStatsForLevel(m_record.Level);
                //BasePower = RegenerationFormulas.GetPowerForLevel(this);
                Asda2BaseAgility = CharacterFormulas.StatOnCreation;
                Asda2BaseIntellect = CharacterFormulas.StatOnCreation;
                Asda2BaseLuck = CharacterFormulas.StatOnCreation;
                Asda2BaseSpirit = CharacterFormulas.StatOnCreation;
                Asda2BaseStamina = CharacterFormulas.StatOnCreation;
                Asda2BaseStrength = CharacterFormulas.StatOnCreation;
            }
            else
            {
                BaseHealth = m_record.BaseHealth;
                SetBasePowerDontUpdate(m_record.BasePower);
                Asda2Strength = m_record.BaseStrength;
                Asda2Intellect = m_record.BaseIntellect;
                Asda2Agility = m_record.BaseAgility;
                Asda2Stamina = m_record.BaseStamina;
                Asda2Luck = m_record.BaseLuck;
                Asda2Spirit = m_record.BaseSpirit;
                /*Asda2BaseSpirit = lvlStats.Spirit;
                Asda2BaseStamina = lvlStats.Stamina;
                Asda2BaseStrength = lvlStats.Strength;
                Asda2BaseIntellect = lvlStats.Intellect;
                Asda2BaseAgility = lvlStats.Agility;
                Asda2BaseLuck = GetBaseLuck();*/

                /*SetBaseStat(StatType.Strength, m_record.BaseStrength);
                SetBaseStat(StatType.Stamina, m_record.BaseStamina);
                SetBaseStat(StatType.Spirit, m_record.BaseSpirit);
                SetBaseStat(StatType.Intellect, m_record.BaseIntellect);
                SetBaseStat(StatType.Agility, m_record.BaseAgility);*/

                Power = m_record.Power;
                SetInt32(UnitFields.HEALTH, m_record.Health);
            }
            UpdateAsda2Agility();
            UpdateAsda2Stamina();
            UpdateAsda2Luck();
            UpdateAsda2Spirit();
            UpdateAsda2Intellect();
            UpdateAsda2Strength();
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads this Character from DB when logging in.
        /// </summary>
        /// <remarks>Requires IO-Context.</remarks>
        protected internal void Load()
        {
            var nativeModel = m_archetype.Race.GetModel(m_record.Gender);
            NativeDisplayId = nativeModel.DisplayId;
            var model = nativeModel;
            /*if (m_record.DisplayId != model.DisplayId)
            {
                model = UnitMgr.GetModelInfo(m_record.DisplayId) ?? nativeModel;
            }*/
            Model = UnitMgr.DefaultModel; // UnitMgr.GetModelInfo(m_record.DisplayId);// model;

            // set FreeTalentPoints silently
            UpdateFreeTalentPointsSilently(0);
            if (m_record.JustCreated)
            {
                // newly created Character
                SpecProfiles = new[] { SpecProfile.NewSpecProfile(this, 0) };
                /*

                                if (m_zone != null)
                                {
                                    SetZoneExplored(m_zone.Template, true);
                                }
                */

                //m_record.FreeTalentPoints = 0;

                // Honor and Arena
                m_record.KillsTotal = 0u;
                m_record.HonorToday = 0u;
                m_record.HonorYesterday = 0u;
                m_record.LifetimeHonorableKills = 0u;
                m_record.HonorPoints = 0u;
                m_record.ArenaPoints = 0u;
                m_record.TitlePoints = 0u;
                m_record.Rank = (int)0u;
            }
            else
            {
                // existing Character
                try
                {
                    Asda2BaseAgility = Record.BaseAgility;
                    Asda2BaseIntellect = Record.BaseIntellect;
                    Asda2BaseStrength = Record.BaseStrength;
                    Asda2BaseLuck = Record.BaseLuck;
                    Asda2BaseSpirit = Record.BaseSpirit;
                    Asda2BaseStamina = Record.BaseStamina;
                    UpdateAsda2Agility();
                    UpdateAsda2Intellect();
                    UpdateAsda2Luck();
                    UpdateAsda2Spirit();
                    UpdateAsda2Stamina();
                    UpdateAsda2Strength();
                    //Set Playerfields for glyphs on load
                    InitGlyphsForLevel();
                    // load & validate SpecProfiles
                    SpecProfiles = SpecProfile.LoadAllOfCharacter(this);
                    if (SpecProfiles.Length == 0)
                    {
                        SpecProfiles = new[] { SpecProfile.NewSpecProfile(this, 0) };
                    }
                    if (m_record.CurrentSpecIndex >= SpecProfiles.Length)
                    {
                        m_record.CurrentSpecIndex = 0;
                    }

                    // load all the rest
                    try
                    {
                        m_achievements.Load();
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load achievements, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }
                    /*try
                    {
                        if (string.IsNullOrEmpty(Record.TitleProgress))
                        {
                            TitleProgress = new Asda2TitleProgress();
                            TitleProgress.InitNew();
                        }
                        else
                        {
                            TitleProgress = JsonHelper.Deserialize<Asda2TitleProgress>(Record.TitleProgress);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load title progress, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }*/
                    try
                    {
                        ((PlayerSpellCollection)m_spells).LoadSpellsAndTalents();
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load LoadSpellsAndTalents, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }
                    try
                    {
                        ((PlayerSpellCollection)m_spells).LoadCooldowns();
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load LoadCooldowns, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    } /*
                    m_skills.Load();
                    m_mailAccount.Load();
                    m_reputations.Load();*/
                    try
                    {
                        var auras = AuraRecord.LoadAuraRecords(EntityId.Low);
                        AddPostUpdateMessage(() => m_auras.InitializeAuras(auras));
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load LoadAuraRecords, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }
                    try
                    {

                        var books = Asda2FishingBook.LoadAll(this);
                        foreach (var asda2FishingBook in books)
                        {
                            if (RegisteredFishingBooks.ContainsKey(asda2FishingBook.Num))
                                asda2FishingBook.DeleteLater();
                            else
                                RegisteredFishingBooks.Add(asda2FishingBook.Num, asda2FishingBook);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load fishing books, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }
                    try
                    {
                        var mailMsgs = Asda2MailMessage.LoadAll(this);
                        foreach (var asda2MailMessage in mailMsgs)
                        {
                            MailMessages.Add(asda2MailMessage.Guid, asda2MailMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load mail messages, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }/*

                    if (QuestMgr.Loaded)
                    {
                        LoadQuests();
                    }
*/
                    try
                    {
                        var premBuffs = FunctionItemBuff.LoadAll(this);
                        foreach (var functionItemBuff in premBuffs)
                        {
                            if (!PremiumBuffs.ContainsKey(functionItemBuff.Template.Category) &&
                                ((functionItemBuff.Duration > 0 && !functionItemBuff.IsLongTime) ||
                                 (functionItemBuff.EndsDate > DateTime.Now && functionItemBuff.IsLongTime)))
                            {

                                if (functionItemBuff.IsLongTime)
                                {
                                    if (
                                        LongTimePremiumBuffs.Count(
                                            l => l != null && l.Template.Category == functionItemBuff.Template.Category) >
                                        0)
                                    {
                                        functionItemBuff.DeleteLater();
                                        continue;
                                    }
                                    LongTimePremiumBuffs.AddElement(functionItemBuff);
                                    //if (functionItemBuff.Template.Category == Catz.)//rozma
                                    //    Asda2WingsItemId = (short)functionItemBuff.Template.Id;
                                }
                                else
                                {
                                    PremiumBuffs.Add(functionItemBuff.Template.Category, functionItemBuff);
                                }

                                ProcessFunctionalItemEffect(functionItemBuff, true);
                            }
                            else
                            {
                                functionItemBuff.DeleteLater();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.ErrorException(ex, string.Format("failed to load premium buffs, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                    }/*
                    if (m_record.FinishedQuests != null)
                    {
                        m_questLog.FinishedQuests.AddRange(m_record.FinishedQuests);
                    }*/
                }
                catch (Exception e)
                {
                    RealmDBMgr.OnDBError(e);
                    return;
                }
                /*

                                SetExploredZones();

                                //Add existing talents to the character
                                ((PlayerSpellCollection) m_spells).PlayerInitialize();

                                // calculate amount of spent talent points per tree
                                m_talents.CalcSpentTalentPoints();
                */
                /*

                                // update RestState
                                if (m_record.RestTriggerId != 0 &&
                                    (m_restTrigger = AreaTriggerMgr.GetTrigger((uint) m_record.RestTriggerId)) != null)
                                {
                                    RestState = RestState.Resting;
                                }
                */
                try
                {
                    if (m_record.LastLogout != null)
                    {
                        var now = DateTime.Now;
                        RestXp += RestGenerator.GetRestXp(now - m_record.LastLogout.Value, this);

                        m_lastRestUpdate = now;
                    }
                    else
                    {
                        m_lastRestUpdate = DateTime.Now;
                    }
                    /*

                                    m_taxiNodeMask.Mask = m_record.TaxiMask;
                    */

                    // Honor and Arena
                    KillsTotal = m_record.KillsTotal;
                    HonorToday = m_record.HonorToday;
                    HonorYesterday = m_record.HonorYesterday;
                    LifetimeHonorableKills = m_record.LifetimeHonorableKills;
                    HonorPoints = m_record.HonorPoints;
                    ArenaPoints = m_record.ArenaPoints;
                    Asda2TitlePoints = (int)m_record.TitlePoints;
                    Asda2Rank = m_record.Rank;
                    Health = m_record.Health;
                    Power = m_record.Power;
                    RecalculateFactionRank(true);
                }
                catch (Exception ex)
                {
                    LogUtil.ErrorException(ex, string.Format("failed to load last load init, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
            }

            // Set FreeTalentPoints, after SpecProfile was loaded

            //var freePointsForLevel = m_talents.GetFreeTalentPointsForLevel(m_record.Level);
            //m_talents.UpdateFreeTalentPointsSilently(freePointsForLevel);

            // Load pets (if any)
            LoadPets();

            //foreach (var skill in m_skills)
            //{
            //    if (skill.SkillLine.Category == SkillCategory.ArmorProficiency) {
            //        CharacterHandler.SendProfiency(m_client, ItemClass.Armor, (uint)skill.SkillLine.Id);
            //    }
            //}			

            // this prevents a the Char from re-sending a value update when being pushed to world AFTER creation
            ResetUpdateInfo();
            /*if (TitleProgress == null)
            {
                TitleProgress = new Asda2TitleProgress();
                TitleProgress.InitNew();
            }*/

        }

        /// <summary>
        /// Ensure correct size of array of explored zones and  copy explored zones to UpdateValues array
        /// </summary>
        private unsafe void SetExploredZones()
        {
            if (m_record.ExploredZones.Length != UpdateFieldMgr.ExplorationZoneFieldSize * 4)
            {
                var zones = m_record.ExploredZones;
                Array.Resize(ref zones, UpdateFieldMgr.ExplorationZoneFieldSize * 4);
                m_record.ExploredZones = zones;
            }

            fixed (byte* ptr = m_record.ExploredZones)
            {
                int index = 0;
                for (var field = PlayerFields.EXPLORED_ZONES_1;
                     field < PlayerFields.EXPLORED_ZONES_1 + UpdateFieldMgr.ExplorationZoneFieldSize;
                     field++)
                {
                    SetUInt32(field, *(uint*)(&ptr[index]));
                    index += 4;
                }
            }
        }

        internal void LoadQuests()
        {
            m_questLog.Load();
        }

        private void LoadEquipmentState()
        {
            if (m_record.CharacterFlags.HasFlag(CharEnumFlags.HideCloak))
            {
                PlayerFlags |= PlayerFlags.HideCloak;
            }
            if (m_record.CharacterFlags.HasFlag(CharEnumFlags.HideHelm))
            {
                PlayerFlags |= PlayerFlags.HideHelm;
            }
        }

        private void LoadDeathState()
        {
            if (m_record.CorpseX != null)
            {
                // we were dead and released the corpse
                var map = World.GetNonInstancedMap(m_record.CorpseMap);
                if (map != null)
                {
                    m_corpse = SpawnCorpse(false, false, map,
                                           new Vector3(m_record.CorpseX.Value, m_record.CorpseY, m_record.CorpseZ),
                                           m_record.CorpseO);
                    BecomeGhost();
                }
                else
                {
                    // can't spawn corpse -> revive
                    if (log.IsWarnEnabled)
                    {
                        log.Warn("Player {0}'s Corpse was spawned in invalid map: {1}", this, m_record.CorpseMap);
                    }
                }
            }
            else if (m_record.Health == 0)
            {
                // we were dead and did not release yet
                var diff = DateTime.Now.Subtract(m_record.LastDeathTime).ToMilliSecondsInt() + Corpse.AutoReleaseDelay;
                m_corpseReleaseTimer = new TimerEntry(dt => ReleaseCorpse());

                if (diff > 0)
                {
                    // mark dead and start release timer
                    MarkDead();
                    m_corpseReleaseTimer.Start(diff, 0);
                }
                else
                {
                    // auto release
                    ReleaseCorpse();
                }
            }
            else
            {
                // we are alive and kicking
            }
        }

        #endregion

        public void SetClass(int proffLevel, int proff)
        {
            var chr = this;
            if (chr.Archetype.ClassId != ClassId.NoClass)
            {
                switch ((byte)chr.Archetype.ClassId)
                {
                    case 1:
                        chr.Spells.Remove(SpellId.FistofIronRank1);
                        chr.Spells.Remove(SpellId.FistofDestructionRank1);
                        chr.Spells.Remove(SpellId.FistofSplinterRank1);
                        break;
                    case 2:
                        chr.Spells.Remove(SpellId.RaidofThiefRank1);
                        chr.Spells.Remove(SpellId.RaidofBurglarRank1);
                        chr.Spells.Remove(SpellId.RaidofTraitorRank1);
                        break;
                    case 3:
                        chr.Spells.Remove(SpellId.SlicetheLandRank1);
                        chr.Spells.Remove(SpellId.SlicetheOceanRank1);
                        chr.Spells.Remove(SpellId.SlicetheSkyRank1);
                        break;
                    case 4:
                        chr.Spells.Remove(SpellId.SilencingShotRank1);
                        chr.Spells.Remove(SpellId.DestructiveShotRank1);
                        chr.Spells.Remove(SpellId.DarkShotRank1);
                        break;
                    case 5:
                        chr.Spells.Remove(SpellId.ExplosiveShotRank1);
                        chr.Spells.Remove(SpellId.DefeatingShotRank1);
                        chr.Spells.Remove(SpellId.SlicingShotRank1);
                        break;
                    case 6:
                        chr.Spells.Remove(SpellId.FirePiercingShotRank1);
                        chr.Spells.Remove(SpellId.IronPiercingShotRank1);
                        chr.Spells.Remove(SpellId.ImmortalPiercingShotRank1);
                        break;
                    case 7:
                        chr.Spells.Remove(SpellId.FlameofSinRank1);
                        chr.Spells.Remove(SpellId.FlameofPunishmentRank1);
                        chr.Spells.Remove(SpellId.FlameofExtinctionRank1);
                        break;
                    case 8:
                        chr.Spells.Remove(SpellId.CallofEarthquakeRank1);
                        chr.Spells.Remove(SpellId.CallofCrisisRank1);
                        chr.Spells.Remove(SpellId.CallofAnnihilationRank1);
                        break;
                    case 9:
                        chr.Spells.Remove(SpellId.FirstShockWaveRank1);
                        chr.Spells.Remove(SpellId.SecondShockWaveRank1);
                        chr.Spells.Remove(SpellId.ThirdShockWaveRank1);
                        break;
                }
            }
            if (proff >= 1 && proff <= 3)
            {

                chr.ProfessionLevel = (byte)proffLevel;
                if (proff >= 3)
                    chr.UpdateTitleCounter((Asda2TitleId)24, 1, 1);
            }
            if (proff >= 4 && proff <= 6)
            {

                chr.ProfessionLevel = (byte)(proffLevel + 11);
                if (proff >= 3)
                    chr.UpdateTitleCounter((Asda2TitleId)25, 1, 1);

            }
            if (proff >= 7 && proff <= 9)
            {

                chr.ProfessionLevel = (byte)(proffLevel + 22);
                if (proff >= 3)
                    chr.UpdateTitleCounter((Asda2TitleId)26, 1, 1);

            }
            chr.Archetype = ArchetypeMgr.GetArchetype(RaceId.Human, (ClassId)proff);
            switch (proff)
            {
                case 1:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.FistofIronRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.FistofIronRank1);
                            chr.Spells.AddSpell(SpellId.FistofDestructionRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.FistofIronRank1);
                            chr.Spells.AddSpell(SpellId.FistofDestructionRank1);
                            chr.Spells.AddSpell(SpellId.FistofSplinterRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.FistofIronRank1);
                            chr.Spells.AddSpell(SpellId.FistofDestructionRank1);
                            chr.Spells.AddSpell(SpellId.FistofSplinterRank1);
                            break;
                    }
                    break;
                case 2:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.RaidofThiefRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.RaidofThiefRank1);
                            chr.Spells.AddSpell(SpellId.RaidofBurglarRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.RaidofThiefRank1);
                            chr.Spells.AddSpell(SpellId.RaidofBurglarRank1);
                            chr.Spells.AddSpell(SpellId.RaidofTraitorRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.RaidofThiefRank1);
                            chr.Spells.AddSpell(SpellId.RaidofBurglarRank1);
                            chr.Spells.AddSpell(SpellId.RaidofTraitorRank1);
                            break;
                    }
                    break;
                case 3:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.SlicetheLandRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.SlicetheLandRank1);
                            chr.Spells.AddSpell(SpellId.SlicetheOceanRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.SlicetheLandRank1);
                            chr.Spells.AddSpell(SpellId.SlicetheOceanRank1);
                            chr.Spells.AddSpell(SpellId.SlicetheSkyRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.SlicetheLandRank1);
                            chr.Spells.AddSpell(SpellId.SlicetheOceanRank1);
                            chr.Spells.AddSpell(SpellId.SlicetheSkyRank1);
                            break;
                    }
                    break;
                case 4:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.SilencingShotRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.SilencingShotRank1);
                            chr.Spells.AddSpell(SpellId.DestructiveShotRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.SilencingShotRank1);
                            chr.Spells.AddSpell(SpellId.DestructiveShotRank1);
                            chr.Spells.AddSpell(SpellId.DarkShotRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.SilencingShotRank1);
                            chr.Spells.AddSpell(SpellId.DestructiveShotRank1);
                            chr.Spells.AddSpell(SpellId.DarkShotRank1);
                            break;
                    }
                    break;
                case 5:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.ExplosiveShotRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.ExplosiveShotRank1);
                            chr.Spells.AddSpell(SpellId.DefeatingShotRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.ExplosiveShotRank1);
                            chr.Spells.AddSpell(SpellId.DefeatingShotRank1);
                            chr.Spells.AddSpell(SpellId.SlicingShotRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.ExplosiveShotRank1);
                            chr.Spells.AddSpell(SpellId.DefeatingShotRank1);
                            chr.Spells.AddSpell(SpellId.SlicingShotRank1);
                            break;
                    }
                    break;
                case 6:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.FirePiercingShotRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.FirePiercingShotRank1);
                            chr.Spells.AddSpell(SpellId.IronPiercingShotRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.FirePiercingShotRank1);
                            chr.Spells.AddSpell(SpellId.IronPiercingShotRank1);
                            chr.Spells.AddSpell(SpellId.ImmortalPiercingShotRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.FirePiercingShotRank1);
                            chr.Spells.AddSpell(SpellId.IronPiercingShotRank1);
                            chr.Spells.AddSpell(SpellId.ImmortalPiercingShotRank1);
                            break;
                    }
                    break;

                case 7:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.FlameofSinRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.FlameofSinRank1);
                            chr.Spells.AddSpell(SpellId.FlameofPunishmentRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.FlameofSinRank1);
                            chr.Spells.AddSpell(SpellId.FlameofPunishmentRank1);
                            chr.Spells.AddSpell(SpellId.FlameofExtinctionRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.FlameofSinRank1);
                            chr.Spells.AddSpell(SpellId.FlameofPunishmentRank1);
                            chr.Spells.AddSpell(SpellId.FlameofExtinctionRank1);
                            break;
                    }
                    break;
                case 8:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.CallofEarthquakeRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.CallofEarthquakeRank1);
                            chr.Spells.AddSpell(SpellId.CallofCrisisRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.CallofEarthquakeRank1);
                            chr.Spells.AddSpell(SpellId.CallofCrisisRank1);
                            chr.Spells.AddSpell(SpellId.CallofAnnihilationRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.CallofEarthquakeRank1);
                            chr.Spells.AddSpell(SpellId.CallofCrisisRank1);
                            chr.Spells.AddSpell(SpellId.CallofAnnihilationRank1);
                            break;
                    }
                    break;
                case 9:
                    switch (chr.RealProffLevel)
                    {
                        case 1:
                            chr.Spells.AddSpell(SpellId.FirstShockWaveRank1);
                            break;
                        case 2:
                            chr.Spells.AddSpell(SpellId.FirstShockWaveRank1);
                            chr.Spells.AddSpell(SpellId.SecondShockWaveRank1);
                            break;
                        case 3:
                            chr.Spells.AddSpell(SpellId.FirstShockWaveRank1);
                            chr.Spells.AddSpell(SpellId.SecondShockWaveRank1);
                            chr.Spells.AddSpell(SpellId.ThirdShockWaveRank1);
                            break;
                        case 4:
                            chr.Spells.AddSpell(SpellId.FirstShockWaveRank1);
                            chr.Spells.AddSpell(SpellId.SecondShockWaveRank1);
                            chr.Spells.AddSpell(SpellId.ThirdShockWaveRank1);
                            break;
                    }
                    break;

            }
            #region 4thProff
            if (chr.RealProffLevel == 4)
            {
                if (proff == 1 || proff == 2 || proff == 3)
                {
                    chr.Spells.AddSpell(100632);
                    chr.Spells.AddSpell(100633);
                    chr.Spells.AddSpell(100634);
                    if ((chr.FirstTree + chr.SecondTree + chr.ThirdTree) >= 60)
                    {
                        chr.Spells.AddSpell(100617);
                        chr.Spells.AddSpell(100619);
                    }
                }
                if (proff == 4 || proff == 5 || proff == 6)
                {
                    chr.Spells.AddSpell(100841);
                    chr.Spells.AddSpell(100842);
                    chr.Spells.AddSpell(100843);
                    if ((chr.FirstTree + chr.SecondTree + chr.ThirdTree) >= 60)
                    {
                        chr.Spells.AddSpell(100826);
                        chr.Spells.AddSpell(100828);
                    }
                }
                if (proff == 7 || proff == 8 || proff == 9)
                {
                    chr.Spells.AddSpell(1001062);
                    chr.Spells.AddSpell(1001065);
                    chr.Spells.AddSpell(1001066);
                    if ((chr.FirstTree + chr.SecondTree + chr.ThirdTree) >= 60)
                    {
                        chr.Spells.AddSpell(1001050);
                        chr.Spells.AddSpell(1001051);
                    }
                }
            }
            #endregion
            //if (NoDefaultSpells(chr))
                AddDefaultSpells(chr, proff);
            Asda2CharacterHandler.SendLearnedSkillsInfo(this);
        }
        public static void AddDefaultSpells(Character chr, int proff)
        {
            #region Warrior Skills
            if (proff == 1 || proff == 2 || proff == 3)
            {
                chr.Spells.AddSpell(100501);
                chr.Spells.AddSpell(100502);
                chr.Spells.AddSpell(100520);
                chr.Spells.AddSpell(100504);
                chr.Spells.AddSpell(100505);
                chr.Spells.AddSpell(100527);
                chr.Spells.AddSpell(100555);
                chr.Spells.AddSpell(100507);
                chr.Spells.AddSpell(100508);
                chr.Spells.AddSpell(100590);
            }
            #endregion
            #region Archer Skills
            if (proff == 4 || proff == 5 || proff == 6)
            {
                chr.Spells.AddSpell(100701);
                chr.Spells.AddSpell(100702);
                chr.Spells.AddSpell(100723);
                chr.Spells.AddSpell(100704);
                chr.Spells.AddSpell(100705);
                chr.Spells.AddSpell(100818);
                chr.Spells.AddSpell(100774);
                chr.Spells.AddSpell(100707);
                chr.Spells.AddSpell(100716);
                chr.Spells.AddSpell(100787);
                chr.Spells.AddSpell(100773);
            }
            #endregion
            #region Mage Skills
            if (proff == 7 || proff == 8 || proff == 9)
            {
                chr.Spells.AddSpell(100905);
                chr.Spells.AddSpell(100901);
                chr.Spells.AddSpell(100992);
                chr.Spells.AddSpell(100944);
                chr.Spells.AddSpell(100902);
                chr.Spells.AddSpell(100906);
                chr.Spells.AddSpell(100977);
                chr.Spells.AddSpell(100927);
                chr.Spells.AddSpell(100903);
                chr.Spells.AddSpell(100909);
                chr.Spells.AddSpell(100533);
            }
            #endregion
            Asda2CharacterHandler.SendLearnedSkillsInfo(chr);
        }
        public bool NoDefaultSpells(Character chr)
        {
            if (PlayerSpells.Contains(100501) || PlayerSpells.Contains(1501) || PlayerSpells.Contains(2501) || PlayerSpells.Contains(3501) || PlayerSpells.Contains(4501) || PlayerSpells.Contains(5501) && PlayerSpells.Contains(100502) || PlayerSpells.Contains(1502) || PlayerSpells.Contains(2502) || PlayerSpells.Contains(3502) || PlayerSpells.Contains(4502) || PlayerSpells.Contains(5502) && PlayerSpells.Contains(100520) || PlayerSpells.Contains(1520) || PlayerSpells.Contains(2520) || PlayerSpells.Contains(3520) || PlayerSpells.Contains(4520) || PlayerSpells.Contains(5520) && PlayerSpells.Contains(100504) || PlayerSpells.Contains(1504) || PlayerSpells.Contains(2504) || PlayerSpells.Contains(3504) || PlayerSpells.Contains(4504) || PlayerSpells.Contains(5504) &&
                        PlayerSpells.Contains(100505) || PlayerSpells.Contains(1505) || PlayerSpells.Contains(2505) || PlayerSpells.Contains(3505) || PlayerSpells.Contains(4505) || PlayerSpells.Contains(5505) && PlayerSpells.Contains(100527) || PlayerSpells.Contains(1527) || PlayerSpells.Contains(2527) || PlayerSpells.Contains(3527) || PlayerSpells.Contains(4527) || PlayerSpells.Contains(5527) && PlayerSpells.Contains(100555) || PlayerSpells.Contains(1555) || PlayerSpells.Contains(2555) || PlayerSpells.Contains(3555) || PlayerSpells.Contains(4555) || PlayerSpells.Contains(5555) && PlayerSpells.Contains(100507) || PlayerSpells.Contains(1507) || PlayerSpells.Contains(2507) || PlayerSpells.Contains(3507) || PlayerSpells.Contains(4507) || PlayerSpells.Contains(5075) &&
                        PlayerSpells.Contains(100508) || PlayerSpells.Contains(1508) || PlayerSpells.Contains(2508) || PlayerSpells.Contains(3508) || PlayerSpells.Contains(4508) || PlayerSpells.Contains(5508) && PlayerSpells.Contains(100590) || PlayerSpells.Contains(1590) || PlayerSpells.Contains(2590) || PlayerSpells.Contains(3590) || PlayerSpells.Contains(4590) || PlayerSpells.Contains(5590) || PlayerSpells.Contains(100701) || PlayerSpells.Contains(1701) || PlayerSpells.Contains(2701) || PlayerSpells.Contains(3701) || PlayerSpells.Contains(4701) || PlayerSpells.Contains(5701) && PlayerSpells.Contains(100702) || PlayerSpells.Contains(1702) || PlayerSpells.Contains(2702) || PlayerSpells.Contains(3702) || PlayerSpells.Contains(4702) || PlayerSpells.Contains(5702) && PlayerSpells.Contains(100723) || PlayerSpells.Contains(1723) || PlayerSpells.Contains(2723) || PlayerSpells.Contains(3723) || PlayerSpells.Contains(4723) || PlayerSpells.Contains(5723) && PlayerSpells.Contains(100704) || PlayerSpells.Contains(1704) || PlayerSpells.Contains(2704) || PlayerSpells.Contains(3704) || PlayerSpells.Contains(4704) || PlayerSpells.Contains(5704) &&
                        PlayerSpells.Contains(100705) || PlayerSpells.Contains(1705) || PlayerSpells.Contains(2705) || PlayerSpells.Contains(3705) || PlayerSpells.Contains(4705) || PlayerSpells.Contains(5705) && PlayerSpells.Contains(100818) || PlayerSpells.Contains(1818) || PlayerSpells.Contains(2818) || PlayerSpells.Contains(3818) || PlayerSpells.Contains(4818) || PlayerSpells.Contains(5818) && PlayerSpells.Contains(100774) || PlayerSpells.Contains(1774) || PlayerSpells.Contains(2774) || PlayerSpells.Contains(3774) || PlayerSpells.Contains(4774) || PlayerSpells.Contains(5774) && PlayerSpells.Contains(100707) || PlayerSpells.Contains(1707) || PlayerSpells.Contains(2707) || PlayerSpells.Contains(3707) || PlayerSpells.Contains(4707) || PlayerSpells.Contains(5707) &&
                        PlayerSpells.Contains(100716) || PlayerSpells.Contains(1716) || PlayerSpells.Contains(2716) || PlayerSpells.Contains(3716) || PlayerSpells.Contains(4716) || PlayerSpells.Contains(5716) && PlayerSpells.Contains(100787) || PlayerSpells.Contains(1787) || PlayerSpells.Contains(2787) || PlayerSpells.Contains(3787) || PlayerSpells.Contains(4787) || PlayerSpells.Contains(5787) && PlayerSpells.Contains(100773) || PlayerSpells.Contains(1773) || PlayerSpells.Contains(2773) || PlayerSpells.Contains(3773) || PlayerSpells.Contains(4773) || PlayerSpells.Contains(5773) || PlayerSpells.Contains(100905) || PlayerSpells.Contains(1905) || PlayerSpells.Contains(2905) || PlayerSpells.Contains(3905) || PlayerSpells.Contains(4905) || PlayerSpells.Contains(5905) && PlayerSpells.Contains(100901) || PlayerSpells.Contains(1901) || PlayerSpells.Contains(2901) || PlayerSpells.Contains(3901) || PlayerSpells.Contains(4901) || PlayerSpells.Contains(5901) && PlayerSpells.Contains(100992) || PlayerSpells.Contains(1992) || PlayerSpells.Contains(2992) || PlayerSpells.Contains(3992) || PlayerSpells.Contains(4992) || PlayerSpells.Contains(5992) && PlayerSpells.Contains(100944) || PlayerSpells.Contains(1944) || PlayerSpells.Contains(2944) || PlayerSpells.Contains(3944) || PlayerSpells.Contains(4944) || PlayerSpells.Contains(5944) &&
                        PlayerSpells.Contains(100902) || PlayerSpells.Contains(1902) || PlayerSpells.Contains(2902) || PlayerSpells.Contains(3902) || PlayerSpells.Contains(4902) || PlayerSpells.Contains(5902) && PlayerSpells.Contains(100906) || PlayerSpells.Contains(1906) || PlayerSpells.Contains(2906) || PlayerSpells.Contains(3906) || PlayerSpells.Contains(4906) || PlayerSpells.Contains(5906) && PlayerSpells.Contains(100977) || PlayerSpells.Contains(1977) || PlayerSpells.Contains(2977) || PlayerSpells.Contains(3977) || PlayerSpells.Contains(4977) || PlayerSpells.Contains(5977) && PlayerSpells.Contains(100927) || PlayerSpells.Contains(1927) || PlayerSpells.Contains(2927) || PlayerSpells.Contains(3927) || PlayerSpells.Contains(4927) || PlayerSpells.Contains(5927) &&
                        PlayerSpells.Contains(100903) || PlayerSpells.Contains(1903) || PlayerSpells.Contains(2903) || PlayerSpells.Contains(3903) || PlayerSpells.Contains(4903) || PlayerSpells.Contains(5903) && PlayerSpells.Contains(100909) || PlayerSpells.Contains(1909) || PlayerSpells.Contains(2909) || PlayerSpells.Contains(3909) || PlayerSpells.Contains(4909) || PlayerSpells.Contains(5909) && PlayerSpells.Contains(100533) || PlayerSpells.Contains(1533) || PlayerSpells.Contains(2533) || PlayerSpells.Contains(3533) || PlayerSpells.Contains(4533) || PlayerSpells.Contains(5533))
                return false;
            else
                return true;
        }
        #region Login / Init


        public void CheckAddSpells()
        {
            var f = FirstTree;
            var s = SecondTree;
            var t = ThirdTree;
            var v = FourthTree;
            #region WarriorSkills
            if (Class == ClassId.OHS || Class == ClassId.Spear || Class == ClassId.THS)
            {
                if (f == 5)
                {
                    Spells.AddSpell(100503);
                    Spells.AddSpell(100523);
                    Spells.AddSpell(100581);
                    Spells.AddSpell(100551);
                }
                if (f == 10)
                {
                    Spells.AddSpell(100537);
                    Spells.AddSpell(100524);
                }
                if (PlayerSpells.Contains(5520) && f >= 10)
                    Spells.AddSpell(100542);
                if (f == 15)
                {
                    Spells.AddSpell(100535);
                    Spells.AddSpell(100522);
                }
                if (PlayerSpells.Contains(5581) && f >= 15)
                    Spells.AddSpell(100582);
                if (f == 20)
                {
                    Spells.AddSpell(100514);
                    Spells.AddSpell(100509);
                    Spells.AddSpell(100517);
                }
                if (f == 25)
                {
                    Spells.AddSpell(100594);
                    Spells.AddSpell(100521);
                }
                if (PlayerSpells.Contains(2535) && f >= 25)
                    Spells.AddSpell(100595);
                if (f == 30)
                {
                    Spells.AddSpell(100540);
                    Spells.AddSpell(100525);
                    Spells.AddSpell(100545);
                }
                if (f == 35)
                {
                    Spells.AddSpell(100526);
                    Spells.AddSpell(100600);
                }
                if (f == 40)
                {
                    Spells.AddSpell(100513);
                    Spells.AddSpell(100510);
                }
                if (PlayerSpells.Contains(5545) && f >= 45)
                    Spells.AddSpell(100596);
                if (PlayerSpells.Contains(5521) && f >= 50)
                    Spells.AddSpell(100531);
                if (PlayerSpells.Contains(5596) && f >= 50)
                    Spells.AddSpell(100530);
                if (f == 55)
                {
                    Spells.AddSpell(100597);
                }
                if (PlayerSpells.Contains(5524) && f >= 55)
                    Spells.AddSpell(100544);
                if (s == 5)
                {
                    Spells.AddSpell(100506);
                    Spells.AddSpell(100553);
                }
                if (PlayerSpells.Contains(5527) && s >= 5)
                    Spells.AddSpell(100931);
                if (s == 10)
                {
                    Spells.AddSpell(100567);
                    Spells.AddSpell(100549);
                    Spells.AddSpell(100562);
                }
                if (PlayerSpells.Contains(5931) && s >= 15)
                    Spells.AddSpell(100932);
                if (s == 20)
                {
                    Spells.AddSpell(100511);
                    Spells.AddSpell(100548);
                }
                if (PlayerSpells.Contains(5549) && s >= 20)
                    Spells.AddSpell(100565);
                if (PlayerSpells.Contains(5555) && s >= 20)
                    Spells.AddSpell(100584);
                if (s == 25)
                {
                    Spells.AddSpell(100599);
                    Spells.AddSpell(100759);
                    Spells.AddSpell(100561);
                }
                if (PlayerSpells.Contains(5562) && s >= 30)
                    Spells.AddSpell(100609);
                if (PlayerSpells.Contains(1759) && s >= 35)
                    Spells.AddSpell(100760);
                if (PlayerSpells.Contains(5609) && s >= 35)
                    Spells.AddSpell(100610);
                if (s == 40)
                {
                    Spells.AddSpell(100515);
                }
                if (PlayerSpells.Contains(1760) && s >= 40)
                {
                    Spells.AddSpell(100748);
                    Spells.AddSpell(100750);
                }
                if (s == 45)
                {
                    Spells.AddSpell(100560);
                    Spells.AddSpell(100554);
                }
                if (PlayerSpells.Contains(1748) && s >= 45)
                    Spells.AddSpell(100749);
                if (PlayerSpells.Contains(1749) && s >= 50)
                    Spells.AddSpell(100764);
                if (PlayerSpells.Contains(5554) && s >= 50)
                    Spells.AddSpell(100566);
                if (s == 55)
                {
                    Spells.AddSpell(100592);
                    Spells.AddSpell(100546);
                }
                if (t == 5)
                {
                    Spells.AddSpell(100571);
                    Spells.AddSpell(100518);
                    Spells.AddSpell(100570);
                }
                if (t == 10)
                {
                    Spells.AddSpell(100587);
                    Spells.AddSpell(100586);
                }
                if (t == 15)
                {
                    Spells.AddSpell(100574);
                    Spells.AddSpell(100564);
                }
                if (PlayerSpells.Contains(5586) && t >= 15)
                    Spells.AddSpell(100576);
                if (t == 20)
                {
                    Spells.AddSpell(100516);
                    Spells.AddSpell(100604);
                    Spells.AddSpell(100569);
                }
                if (PlayerSpells.Contains(5570) && t >= 20)
                    Spells.AddSpell(100584);
                if (t == 25)
                {
                    Spells.AddSpell(100601);
                    Spells.AddSpell(100591);
                }
                if (PlayerSpells.Contains(1584) && t >= 25)
                    Spells.AddSpell(100575);
                if (t == 30)
                {
                    Spells.AddSpell(100583);
                }
                if (PlayerSpells.Contains(5604) && t >= 30)
                    Spells.AddSpell(100605);
                if (t == 35)
                {
                    Spells.AddSpell(100588);
                }
                if (PlayerSpells.Contains(2605) && t >= 35)
                    Spells.AddSpell(100606);
                if (PlayerSpells.Contains(5584) && t >= 35)
                    Spells.AddSpell(100542);
                if (t == 40)
                {
                    Spells.AddSpell(100512);
                    Spells.AddSpell(100579);
                }
                if (t == 45)
                {
                    Spells.AddSpell(100593);
                    Spells.AddSpell(100573);
                }
                if (PlayerSpells.Contains(5579) && t >= 45)
                    Spells.AddSpell(100580);
                if (t == 50)
                {
                    Spells.AddSpell(100589);
                }
                if (PlayerSpells.Contains(5573) && t >= 50)
                    Spells.AddSpell(100602);
                if (t == 55)
                {
                    Spells.AddSpell(100568);
                    Spells.AddSpell(100607);
                }
                if (v == 5)
                {
                    Spells.AddSpell(100635);
                    Spells.AddSpell(100636);
                    Spells.AddSpell(100637);
                }
                if (v == 15)
                {
                    Spells.AddSpell(100611);
                    Spells.AddSpell(100613);
                }
                if (v == 25)
                {
                    Spells.AddSpell(100612);
                    Spells.AddSpell(100616);
                }
                if ((f + s + t) >= 60)
                {
                    if (v == 5)
                    {
                        Spells.AddSpell(100622);
                    }
                    if (v == 10)
                    {
                        Spells.AddSpell(100623);
                        Spells.AddSpell(100627);
                        Spells.AddSpell(100639);
                    }
                    if (v == 15)
                    {
                        Spells.AddSpell(100624);
                        Spells.AddSpell(100618);
                    }
                    if (PlayerSpells.Contains(3639) && v >= 15)
                        Spells.AddSpell(100615);
                    if (v == 20)
                    {
                        Spells.AddSpell(100626);
                        Spells.AddSpell(100638);
                        Spells.AddSpell(100628);
                    }
                    if (v == 25)
                    {
                        Spells.AddSpell(100630);
                        Spells.AddSpell(100631);
                    }
                    if (PlayerSpells.Contains(3638) && v >= 25)
                        Spells.AddSpell(100614);
                }
            }
            #endregion
            #region ArcherSkills
            if (Class == ClassId.Crossbow || Class == ClassId.Bow || Class == ClassId.Balista)
            {
                if (f == 5)
                {
                    Spells.AddSpell(100708);
                    Spells.AddSpell(100719);
                    Spells.AddSpell(100807);
                }
                if (f == 10)
                {
                    Spells.AddSpell(100737);
                    Spells.AddSpell(100743);
                }
                if (PlayerSpells.Contains(5723) && f >= 10)
                    Spells.AddSpell(100724);
                if (f == 15)
                {
                    Spells.AddSpell(100738);
                    Spells.AddSpell(100799);
                }
                if (f == 20)
                {
                    Spells.AddSpell(100711);
                    Spells.AddSpell(100712);
                    Spells.AddSpell(100715);
                }
                if (f == 25)
                {
                    Spells.AddSpell(100729);
                    Spells.AddSpell(100739);
                    Spells.AddSpell(100747);
                }
                if (f == 30)
                {
                    Spells.AddSpell(100775);
                }
                if (PlayerSpells.Contains(5747) && f >= 25)
                    Spells.AddSpell(100762);
                if (f == 35)
                {
                    Spells.AddSpell(100722);
                }
                if (PlayerSpells.Contains(5762) && f >= 35)
                    Spells.AddSpell(100763);
                if (f == 40)
                {
                    Spells.AddSpell(100709);
                    Spells.AddSpell(100703);
                }
                if (f == 45)
                {
                    Spells.AddSpell(100973);
                }
                if (PlayerSpells.Contains(1722) && f >= 45)
                    Spells.AddSpell(100801);
                if (PlayerSpells.Contains(5775) && f >= 45)
                    Spells.AddSpell(100789);
                if (f == 50)
                {
                    Spells.AddSpell(100728);
                }
                if (PlayerSpells.Contains(5973) && f >= 50)
                    Spells.AddSpell(100811);
                if (f == 55)
                {
                    Spells.AddSpell(100741);
                    Spells.AddSpell(100761);
                }
                if (s == 5)
                {
                    Spells.AddSpell(100713);
                    Spells.AddSpell(100765);
                    Spells.AddSpell(100806);
                    Spells.AddSpell(100718);
                }
                if (s == 10)
                {
                    Spells.AddSpell(100725);
                    Spells.AddSpell(100756);
                }
                if (PlayerSpells.Contains(5705) && s >= 10)
                    Spells.AddSpell(100752);
                if (s == 15)
                {
                    Spells.AddSpell(100785);
                    Spells.AddSpell(100726);
                }
                if (PlayerSpells.Contains(5713) && s >= 15)
                    Spells.AddSpell(100753);
                if (s == 20)
                {
                    Spells.AddSpell(100706);
                    Spells.AddSpell(100742);
                }
                if (s == 25)
                {
                    Spells.AddSpell(100767);
                }
                if (PlayerSpells.Contains(1756) && s >= 25)
                    Spells.AddSpell(100744);
                if (PlayerSpells.Contains(5726) && s >= 30)
                    Spells.AddSpell(100727);
                if (PlayerSpells.Contains(5767) && s >= 30)
                    Spells.AddSpell(100768);
                if (PlayerSpells.Contains(5785) && s >= 35)
                    Spells.AddSpell(100815);
                if (PlayerSpells.Contains(2744) && s >= 35)
                    Spells.AddSpell(100745);
                if (s == 40)
                {
                    Spells.AddSpell(100710);
                }
                if (PlayerSpells.Contains(5774) && s >= 40)
                    Spells.AddSpell(100794);
                if (s == 45)
                {
                    Spells.AddSpell(100771);
                }
                if (PlayerSpells.Contains(5815) && s >= 45)
                    Spells.AddSpell(100816);
                if (PlayerSpells.Contains(1727) && s >= 50)
                    Spells.AddSpell(100812);
                if (s == 55)
                {
                    Spells.AddSpell(100766);
                }
                if (PlayerSpells.Contains(5816) && s >= 55)
                    Spells.AddSpell(100817);
                if (t == 5)
                {
                    Spells.AddSpell(100717);
                    Spells.AddSpell(100805);
                }
                if (t == 10)
                {
                    Spells.AddSpell(100786);
                    Spells.AddSpell(100784);
                }
                if (PlayerSpells.Contains(5717) && s >= 10)
                    Spells.AddSpell(100730);
                if (t == 15)
                {
                    Spells.AddSpell(100791);
                }
                if (PlayerSpells.Contains(1786) && t >= 15)
                    Spells.AddSpell(100776);
                if (PlayerSpells.Contains(5730) && t >= 15)
                    Spells.AddSpell(100731);
                if (PlayerSpells.Contains(1784) && t >= 15)
                    Spells.AddSpell(100793);
                if (t == 20)
                {
                    Spells.AddSpell(100788);
                    Spells.AddSpell(100772);
                    Spells.AddSpell(100777);
                }
                if (t == 25)
                {
                    Spells.AddSpell(100732);
                }
                if (PlayerSpells.Contains(3788) && t >= 25)
                    Spells.AddSpell(100779);
                if (t == 30)
                {
                    Spells.AddSpell(100797);
                }
                if (PlayerSpells.Contains(5773) && t >= 30)
                    Spells.AddSpell(100780);
                if (t == 35)
                {
                    Spells.AddSpell(100736);
                }
                if (PlayerSpells.Contains(1732) && t >= 35)
                    Spells.AddSpell(100721);
                if (PlayerSpells.Contains(5793) && t >= 35)
                    Spells.AddSpell(100778);
                if (t == 40)
                {
                    Spells.AddSpell(100714);
                    Spells.AddSpell(100792);
                    Spells.AddSpell(100720);
                }
                if (t == 45)
                {
                    Spells.AddSpell(100809);
                }
                if (PlayerSpells.Contains(5791) && t >= 45)
                    Spells.AddSpell(100803);
                if (PlayerSpells.Contains(5720) && t >= 45)
                    Spells.AddSpell(100733);
                if (t == 50)
                {
                    Spells.AddSpell(100781);
                }
                if (PlayerSpells.Contains(3809) && t >= 50)
                    Spells.AddSpell(100810);
                if (PlayerSpells.Contains(5733) && t >= 50)
                    Spells.AddSpell(100735);
                if (t == 55)
                {
                    Spells.AddSpell(100819);
                    Spells.AddSpell(100795);
                }
                if (v == 5)
                {
                    Spells.AddSpell(100844);
                    Spells.AddSpell(100845);
                    Spells.AddSpell(100846);
                }
                if (v == 15)
                {
                    Spells.AddSpell(100820);
                    Spells.AddSpell(100824);
                }
                if (v == 25)
                {
                    Spells.AddSpell(100823);
                }
                if ((f + s + t) >= 60)
                {
                    if (v == 5)
                    {
                        Spells.AddSpell(100831);
                    }
                    if (v == 10)
                    {
                        Spells.AddSpell(100834);
                        Spells.AddSpell(100836);
                    }
                    if (v == 15)
                    {
                        Spells.AddSpell(100833);
                        Spells.AddSpell(100822);
                        Spells.AddSpell(100827);
                    }
                    if (v == 20)
                    {
                        Spells.AddSpell(100835);
                        Spells.AddSpell(100837);
                        Spells.AddSpell(100847);
                    }
                    if (v == 25)
                    {
                        Spells.AddSpell(100821);
                        Spells.AddSpell(100839);
                        Spells.AddSpell(100840);
                    }
                    if (PlayerSpells.Contains(3847) && v >= 25)
                        Spells.AddSpell(100825);
                }
            }
            #endregion
            #region MageSkills
            if (Class == ClassId.AtackMage || Class == ClassId.SupportMage || Class == ClassId.HealMage)
            {
                if (f == 5)
                {
                    Spells.AddSpell(100908);
                    Spells.AddSpell(100960);
                }
                if (PlayerSpells.Contains(5992) && f >= 5)
                    Spells.AddSpell(100993);
                if (f == 10)
                {
                    Spells.AddSpell(100912);
                    Spells.AddSpell(100556);
                    Spells.AddSpell(100957);
                }
                if (f == 15)
                {
                    Spells.AddSpell(100959);
                }
                if (PlayerSpells.Contains(5957) && f >= 15)
                    Spells.AddSpell(100958);
                if (f == 20)
                {
                    Spells.AddSpell(100907);
                    Spells.AddSpell(100914);
                    Spells.AddSpell(100951);
                }
                if (f == 25)
                {
                    Spells.AddSpell(100966);
                    Spells.AddSpell(100991);
                }
                if (f == 30)
                {
                    Spells.AddSpell(1007);
                    Spells.AddSpell(100941);
                }
                if (f == 35)
                {
                    Spells.AddSpell(100955);
                }
                if (PlayerSpells.Contains(1941) && f >= 35)
                    Spells.AddSpell(100923);
                if (f == 40)
                {
                    Spells.AddSpell(100911);
                    Spells.AddSpell(100915);
                    Spells.AddSpell(1008);
                    Spells.AddSpell(1012);
                }
                if (f == 45)
                {
                    Spells.AddSpell(100961);
                    Spells.AddSpell(100998);
                }
                if (f == 50)
                {
                    Spells.AddSpell(100964);
                }
                if (PlayerSpells.Contains(4008) && PlayerSpells.Contains(4012) && f >= 50)
                    Spells.AddSpell(1013);
                if (f == 55)
                {
                    Spells.AddSpell(100997);
                    Spells.AddSpell(100968);
                }
                if (s == 5)
                {
                    Spells.AddSpell(100938);
                    Spells.AddSpell(100920);
                    Spells.AddSpell(100933);
                }
                if (s == 10)
                {
                    Spells.AddSpell(100953);
                    Spells.AddSpell(100935);
                }
                if (PlayerSpells.Contains(1938) && s >= 10)
                    Spells.AddSpell(1014);
                if (PlayerSpells.Contains(5977) && s >= 10)
                    Spells.AddSpell(100978);
                if (s == 15)
                {
                    Spells.AddSpell(100918);
                    Spells.AddSpell(100994);
                    Spells.AddSpell(100930);
                }
                if (PlayerSpells.Contains(5927) && s >= 15)
                    Spells.AddSpell(1021);
                if (s == 20)
                {
                    Spells.AddSpell(100910);
                }
                if (PlayerSpells.Contains(5994) && s >= 20)
                    Spells.AddSpell(100996);
                if (s == 25)
                {
                    Spells.AddSpell(100940);
                }
                if (PlayerSpells.Contains(5933) && s >= 25)
                    Spells.AddSpell(1029);
                if (s == 30)
                {
                    Spells.AddSpell(1016);
                    Spells.AddSpell(100990);
                }
                if (PlayerSpells.Contains(3021) && s >= 30)
                    Spells.AddSpell(1022);
                if (s == 35)
                {
                    Spells.AddSpell(100925);
                    Spells.AddSpell(100943);
                }
                if (PlayerSpells.Contains(2014) && s >= 35)
                    Spells.AddSpell(100922);
                if (PlayerSpells.Contains(5930) && s >= 35)
                    Spells.AddSpell(1026);
                if (s == 40)
                {
                    Spells.AddSpell(100913);
                }
                if (PlayerSpells.Contains(5990) && s >= 40)
                    Spells.AddSpell(1006);
                if (PlayerSpells.Contains(3022) && s >= 45)
                    Spells.AddSpell(1023);
                if (PlayerSpells.Contains(3029) && s >= 45)
                    Spells.AddSpell(1030);
                if (PlayerSpells.Contains(3023) && s >= 50)
                    Spells.AddSpell(937);
                if (PlayerSpells.Contains(3030) && s >= 50)
                    Spells.AddSpell(1031);
                if (PlayerSpells.Contains(3026) && s >= 55)
                    Spells.AddSpell(1027);
                if (PlayerSpells.Contains(1937) && PlayerSpells.Contains(3027) && s >= 55)
                    Spells.AddSpell(1024);
                if (t == 5)
                {
                    Spells.AddSpell(100904);
                    Spells.AddSpell(100985);
                    Spells.AddSpell(100976);
                }
                if (PlayerSpells.Contains(5533) && s >= 5)
                    Spells.AddSpell(100534);
                if (t == 10)
                {
                    Spells.AddSpell(100984);
                    Spells.AddSpell(100581);
                    Spells.AddSpell(100582);
                }
                if (t == 15)
                {
                    Spells.AddSpell(100980);
                    Spells.AddSpell(100962);
                }
                if (PlayerSpells.Contains(5981) && PlayerSpells.Contains(5982) && s >= 15)
                    Spells.AddSpell(100983);
                if (t == 20)
                {
                    Spells.AddSpell(100916);
                    Spells.AddSpell(100954);
                    Spells.AddSpell(1003);
                }
                if (PlayerSpells.Contains(5976) && t >= 20)
                    Spells.AddSpell(1033);
                if (t == 25)
                {
                    Spells.AddSpell(100987);
                    Spells.AddSpell(100988);
                    Spells.AddSpell(100924);
                }
                if (t == 30)
                {
                    Spells.AddSpell(100965);
                }
                if (PlayerSpells.Contains(6033) && t >= 30)
                    Spells.AddSpell(100989);
                if (t == 35)
                {
                    Spells.AddSpell(100956);
                    Spells.AddSpell(100974);
                }
                if (PlayerSpells.Contains(6033) && t >= 35)
                    Spells.AddSpell(1034);
                if (t == 40)
                {
                    Spells.AddSpell(100917);
                    Spells.AddSpell(1004);
                }
                if (PlayerSpells.Contains(1965) && t >= 45)
                    Spells.AddSpell(1020);
                if (PlayerSpells.Contains(1989) && t >= 45)
                    Spells.AddSpell(1038);
                if (PlayerSpells.Contains(1986) && t >= 50)
                    Spells.AddSpell(1018);
                if (PlayerSpells.Contains(6034) && t >= 50)
                    Spells.AddSpell(1035);
                if (PlayerSpells.Contains(6035) && t >= 55)
                    Spells.AddSpell(1036);
                if (PlayerSpells.Contains(2038) && PlayerSpells.Contains(3036) && t >= 55)
                    Spells.AddSpell(1039);
                if (PlayerSpells.Contains(2039) && t >= 55)
                    Spells.AddSpell(1040);
                if (v == 5)
                {
                    Spells.AddSpell(1063);
                    Spells.AddSpell(1064);
                    Spells.AddSpell(1067);
                }
                if (v == 15)
                {
                    Spells.AddSpell(1041);
                }
                if (v == 25)
                {
                    Spells.AddSpell(1042);
                    Spells.AddSpell(1044);
                    Spells.AddSpell(1046);
                }
                if ((f + s + t) >= 60)
                {
                    if (v == 5)
                    {
                        Spells.AddSpell(1052);
                    }
                    if (v == 10)
                    {
                        Spells.AddSpell(1053);
                        Spells.AddSpell(1057);
                        Spells.AddSpell(1068);
                    }
                    if (v == 15)
                    {
                        Spells.AddSpell(1059);
                        Spells.AddSpell(1043);
                        Spells.AddSpell(1048);
                    }
                    if (PlayerSpells.Contains(4068) && v >= 15)
                        Spells.AddSpell(1045);
                    if (v == 20)
                    {
                        Spells.AddSpell(1056);
                        Spells.AddSpell(1058);
                    }
                    if (v == 25)
                    {
                        Spells.AddSpell(1060);
                        Spells.AddSpell(1061);
                    }
                }
            }
            #endregion
            Asda2CharacterHandler.SendLearnedSkillsInfo(this);
        }
        /// <summary>
        /// Loads and adds the Character to its Map.
        /// </summary>
        /// <remarks>Called initially from the IO-Context</remarks>
        internal void LoadAndLogin()
        {
            // set Zone *before* Map
            // TODO: Also retrieve Battlegrounds
            m_Map = World.GetMap(m_record);

            InstanceMgr.RetrieveInstances(this);

            AreaCharCount++; // Characters are always in active regions

            if (!Role.IsStaff)
            {
                Stunned++;
            }

            var isStaff = Role.IsStaff;
            if (m_Map == null && (!isStaff || (m_Map = InstanceMgr.CreateInstance(this, m_record.MapId)) == null))
            {
                // map does not exist anymore
                Load();
                TeleportToBindLocation();
                AddMessage(InitializeCharacter);
                return;
            }
            else
            {
                Load();
                if (m_Map.IsDisposed ||
                    (m_Map.IsInstance && !isStaff && (m_Map.CreationTime > m_record.LastLogout || !m_Map.CanEnter(this))))
                {
                    // invalid Map or not allowed back in (might be an Instance)
                    m_Map.TeleportOutside(this);

                    AddMessage(InitializeCharacter);
                }
                else
                {
                    m_Map.AddMessage(() =>
                    {
                        // add to map
                        if (m_Map is Battleground)
                        {
                            var bg = (Battleground)m_Map;
                            if (!bg.LogBackIn(this))
                            {
                                // teleport out of BG
                                AddMessage(InitializeCharacter);
                                return;
                            }
                        }

                        m_position = new Vector3(m_record.PositionX,
                                                 m_record.PositionY,
                                                 m_record.PositionZ);

                        m_zone = m_Map.GetZone(m_record.Zone);

                        if (m_zone != null && m_record.JustCreated)
                        {
                            // set initial zone explored automatically
                            SetZoneExplored(m_zone.Id, false);
                        }

                        // during the next Map-wide Character-update, the Character is going to be added to the map 
                        // and created/initialized immediately afterwards
                        m_Map.AddObjectNow(this);

                        InitializeCharacter();
                    });
                }
            }
        }


        /// <summary>
        /// Is called after Character has been added to a map the first time and 
        /// before it receives the first Update packet
        /// </summary>
        protected internal void InitializeCharacter()
        {
            World.AddCharacter(this);
            m_initialized = true;

            try
            {
                Regenerates = true;
                ((PlayerSpellCollection)m_spells).PlayerInitialize();


                if (m_record.JustCreated)
                {
                    if (m_zone != null)
                    {
                        m_zone.EnterZone(this, null);
                    }

                    m_spells.AddDefaultSpells();
                    m_reputations.Initialize();

                    /*if (Class == ClassId.Warrior && Spells.Contains(SpellId.ClassSkillBattleStance))
                    {
                        CallDelayed(1000, obj => SpellCast.Start(SpellId.ClassSkillBattleStance, false));
                    }
                    else if (Class == ClassId.DeathKnight && Spells.Contains(SpellId.ClassSkillBloodPresence))
                    {
                        CallDelayed(1000, obj => SpellCast.Start(SpellId.ClassSkillBloodPresence, false));
                    }*/

                    // set initial weapon skill max values
                    Skills.UpdateSkillsForLevel(Level);
                }
                else
                {
                    LoadDeathState();
                    LoadEquipmentState();
                }

                // load items
#if DEV
                // do this check in case that we did not load Items yet
                if (Asda2ItemMgr.Loaded)
#endif
                    InitItems();

                LoadAsda2Pets();
                LoadAsda2Mounts();
                LoadAsda2TeleportPoints();
                LoadFriends();
                // load ticket information
                var ticket = TicketMgr.Instance.GetTicket(EntityId.Low);
                if (ticket != null)
                {
                    Ticket = ticket;
                    Ticket.OnOwnerLogin(this);
                }

                // initialize sub systems
                Singleton<GroupMgr>.Instance.OnCharacterLogin(this);
                Singleton<GuildMgr>.Instance.OnCharacterLogin(this);
                Singleton<RelationMgr>.Instance.OnCharacterLogin(this);

                //Load Titles
                GetedTitles = new UpdateMask(Record.GetedTitles);
                DiscoveredTitles = new UpdateMask(Record.DiscoveredTitles);
                LearnedRecipes = new UpdateMask(Record.LearnedRecipes);
                for (int i = 0; i < 576; i++)
                {
                    if (LearnedRecipes.GetBit(i))
                        LearnedRecipesCount++;
                }
                for (int i = 0; i < GetedTitles.HighestIndex; i++)
                {
                    if (GetedTitles.GetBit(i))
                        Asda2TitlePoints += Asda2TitleTemplate.Templates[i].Points;
                }

                // set login date
                LastLogin = DateTime.Now;
                var isNew = m_record.JustCreated;

                // perform some stuff ingame
                AddMessage(() =>
                {
                    if (LastLogout == null)
                    {
                        RealmCommandHandler.ExecFirstLoginFileFor(this);
                    }

                    RealmCommandHandler.ExecAllCharsFileFor(this);

                    if (Account.Role.IsStaff)
                    {
                        RealmCommandHandler.ExecFileFor(this);
                    }

                    Stunned--;

                    if (m_record.NextTaxiVertexId != 0)
                    {
                        // we are on a Taxi
                        var vertex = TaxiMgr.GetVertex(m_record.NextTaxiVertexId);
                        if (vertex != null &&
                            vertex.MapId == m_Map.Id &&
                            vertex.ListEntry.Next != null &&
                            IsInRadius(vertex.Pos, vertex.ListEntry.Next.Value.DistFromPrevious))
                        {
                            TaxiPaths.Enqueue(vertex.Path);
                            TaxiMgr.FlyUnit(this, true, vertex.ListEntry);
                        }
                        else
                        {
                            m_record.NextTaxiVertexId = 0;
                        }
                    }
                    else
                    {
                        // cannot stand up instantly because else no one will see the char sitting in the first place
                        StandState = StandState.Stand;
                    }
                    GodMode = m_record.GodMode;

                    if (isNew)
                    {
                        // newly created Char logs in the first time
                        var evt = Created;
                        if (evt != null)
                        {
                            evt(this);
                        }
                    }

                    //if (Role.IsStaff)
                    if (GodMode)
                    {
                        //Notify("Your GodMode is " + (GodMode ? "ON" : "OFF") + "!");
                        /*Notify(RealmLangKey.GodModeIsActivated);*/
                        //Map.CallDelayed(5000, () => SendSystemMessage("God mode is activated."));
                    }

                    var login = LoggedIn;
                    if (login != null)
                    {
                        login(this, true);
                    }
                });

                if (isNew)
                {
                    SaveLater();
                    m_record.JustCreated = false;
                }
                else
                {
                    ServerApp<RealmServer>.IOQueue.AddMessage(() =>
                    {
                        try
                        {
                            m_record.Update();
                        }
                        catch (Exception ex)
                        {
                            SaveLater();
                            LogUtil.ErrorException(ex,
                                                   "Failed to Update CharacterRecord: " +
                                                   m_record);
                        }
                    });
                }

                OnLogin();
            }
            catch (Exception e)
            {
                if (m_record.JustCreated)
                {
                    m_record.CanSave = false;
                    m_record.Delete();
                }
                World.RemoveCharacter(this);
                LogUtil.ErrorException(e, "Failed to initialize Character: " + this);
                m_client.Disconnect();
            }
        }

        private void LoadFriends()
        {
            FriendRecords = Asda2FriendshipRecord.LoadAll(EntityId.Low);
            foreach (var asda2FriendshipRecord in FriendRecords)
            {
                var friendId = asda2FriendshipRecord.GetFriendId(EntityId.Low);
                var charRec = CharacterRecord.LoadRecordByEntityId(friendId);
                if (charRec == null)
                {
                    log.Warn(String.Format("Friendship record broken cause character {0} not founded.", friendId));
                    continue;
                }
                if (Friends.ContainsKey((uint)charRec.AccountId))
                {
                    asda2FriendshipRecord.DeleteLater();
                }
                else
                {
                    Friends.Add((uint)charRec.AccountId, charRec);
                    var chr = World.GetCharacterByAccId((uint)charRec.AccountId);
                    if (chr != null)
                        chr.SendInfoMsg(String.Format("Your friend {0} is now online.", Name));
                }
            }
        }

        private void LoadAsda2TeleportPoints()
        {
            var points = Asda2TeleportingPointRecord.LoadItems(EntityId.Low);
            for (int i = 0; i < points.Length; i++)
            {
                TeleportPoints[i] = points[i];
            }
        }

        private void LoadAsda2Pets()
        {
            var pets = Asda2PetRecord.LoadAll(this);
            foreach (var asda2PetRecord in pets)
            {
                OwnedPets.Add(asda2PetRecord.Guid, asda2PetRecord);
            }
        }
        private void LoadAsda2Mounts()
        {
            var mounts = Asda2MountRecord.GetAllRecordsOfCharacter(EntityId.Low);
            foreach (var m in mounts)
            {
                OwnedMounts.Add(m.Id, m);
            }
        }
        /// <summary>
        /// Load items from DB or (if new char) add initial Items.
        /// Happens either on login or when items have been loaded during runtime
        /// </summary>
        protected internal void InitItems()
        {
            if (m_record.JustCreated)
            {
                _asda2Inventory.FillOnCharacterCreate();
            }
            else
            {
                _asda2Inventory.AddOwnedItems();
            }
        }
        protected internal void InitItemsFirst()
        {
            if (m_record.JustCreated)
            {
                _asda2Inventory.FillOnCharacterCreate();
            }
            else
            {
                _asda2Inventory.AddOwnedItemsLoadonly();
            }
        }
        /// <summary>
        /// Called within Map Context.
        /// Sends initial packets
        /// </summary>
        private void OnLogin()
        {

            IsConnected = true;
            if (IsLoginServerStep)
            {
                Asda2LoginHandler.SendEnterGameResposeResponse(m_client);
                Asda2LoginHandler.SendEnterGameResponseItemsOnCharacterResponse(m_client);
                Asda2LoginHandler.SendEnterWorldIpeResponseResponse(m_client);
                Client.Disconnect(true);
            }
            else
            {
                if (Experience < 0)
                {
                    Experience = 1;
                    LogUtil.WarnException("Character {0} has negative exp. Set it to 1.", Name);
                }
                if (Record.WarehousePassword != null)
                    IsWarehouseLocked = true;
                Asda2CharacterHandler.SendSomeInitGSResponse(m_client);
                Asda2CharacterHandler.SendSomeInitGSOneResponse(m_client);
                Asda2CharacterHandler.SendCharacterInfoSessIdPositionResponse(m_client);
                Asda2LoginHandler.SendInventoryInfoResponse(m_client);
                Asda2CharacterHandler.SendUpdateStatsResponse(m_client);
                Asda2CharacterHandler.SendUpdateStatsOneResponse(m_client);
                Asda2InventoryHandler.SendAllFastItemSlotsInfo(this);
                if (IsFirstGameConnection)
                    Asda2CharacterHandler.SendLearnedSkillsInfo(this);
                if (LastLogin==null)
                {
                    if (Account.Characters.Count == 1)
                    {
                        Asda2Inventory.AddDonateItem(Asda2ItemMgr.GetTemplate(490), 1, "CBT Startup", true);
                        Asda2Inventory.AddDonateItem(Asda2ItemMgr.GetTemplate(503), 1, "CBT Startup", true);
                        LastLogin = DateTime.Now;
                    }
                }
                Asda2CharacterHandler.SendMySessionIdResponse(m_client);
                Asda2CharacterHandler.SendPetBoxSizeInitResponse(this);
                Handlers.Asda2QuestHandler.SendQuestsListResponse(m_client);
                Asda2TitlesHandler.SendDiscoveredTitlesResponse(Client);
                Asda2TitlesHandler.SendGetedTitlesResponse(Client);
                GlobalHandler.SendCharacterPlaceInTitleRatingResponse(Client, this);
                Asda2CraftingHandler.SendLeanedRecipesResponse(Client);
                Asda2MountHandler.SendMountBoxSizeInitResponse(Client);
                if (OwnedMounts.Count > 0)
                    Asda2MountHandler.SendOwnedMountsListResponse(Client);

                if (Asda2Pet != null)
                    GlobalHandler.SendCharacterInfoPetResponse(Client, this);
                if (RegisteredFishingBooks.Count > 0)
                {
                    foreach (var registeredFishingBook in RegisteredFishingBooks)
                    {
                        Asda2FishingHandler.SendFishingBooksInfoResponse(Client, registeredFishingBook.Value);
                    }
                    Asda2FishingHandler.SendFishingBookListEndedResponse(Client);
                }
                if (IsInGuild)
                {
                    GlobalHandler.SendCharacterInfoClanNameResponse(Client, this);
                    Map.CallDelayed(10000, () => Asda2GuildHandler.SendClanFlagAndClanNameInfoSelfResponse(this));
                    Asda2GuildHandler.SendGuildInfoOnLoginResponse(this, Guild);
                    Asda2GuildHandler.SendGuildSkillsInfoResponse(this);
                    Asda2GuildHandler.SendUpdateGuildInfoResponse(Guild);
                    Asda2GuildHandler.SendGuildNotificationResponse(Guild, GuildNotificationType.LoggedIn, GuildMember);
                    Map.CallDelayed(2000, () => Asda2GuildHandler.SendGuildMembersInfoResponse(Client, Guild));
                }
                if (IsInGroup)
                {
                    Group.SendUpdate();
                }
                GlobalHandler.SendCharacterFactionAndFactionRankResponse(Client, this);
                Asda2SoulmateHandler.SendCharacterSoulMateIntrodactionUpdateResponse(Client);
                Asda2LoginHandler.SendLongTimeBuffsInfoResponse(Client);
                ProcessSoulmateRelation(true);
                Asda2CharacterHandler.SendFactionAndHonorPointsInitResponse(Client);
                Asda2FishingHandler.SendFishingLvlResponse(Client);
                GlobalHandler.SendSetClientTimeResponse(Client);
                Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
                Asda2CharacterHandler.SendUpdateStatsResponse(Client);

                if (PrivateShop != null)
                {
                    Map.CallDelayed(4000, () => PrivateShop.ShowOnLogin(this));
                }
                if (Asda2TradeWindow != null)
                {
                    Asda2TradeWindow.CancelTrade();
                }
                UpdateSettings();
                Map.CallDelayed(3800, () =>
                {
                    if (Asda2TradeDescription.Contains("[OFFLINE]") &&
                        Asda2TradeDescription.Length > 10)
                        Asda2TradeDescription = Asda2TradeDescription.Substring(10);
                    if (Asda2TradeDescription.Contains("[OFFLINE]"))
                    {
                        IsAsda2TradeDescriptionEnabled = false;
                        Asda2TradeDescription = "";
                    }
                    IsSitting = false;
                    if (IsOnTransport)
                        FunctionalItemsHandler.SendShopItemUsedResponse(Client,
                                                                        TransportItemId);
                    if (IsOnMount)
                        Asda2MountHandler.SendCharacterOnMountStatusChangedResponse(this, Asda2MountHandler.UseMountStatus.Ok);
                    if (IsDead)
                        Asda2CharacterHandler.SendSelfDeathResponse(this);
                    Asda2AuctionMgr.OnLogin(this);
                    Asda2CharacterHandler.SendRates(this, 2, 2);
                    if (IsAsda2BattlegroundInProgress)
                    {
                        CurrentBattleGround.SendCurrentProgress(this);
                        Asda2BattlegroundHandler.SendWarTeamListResponse(this);
                        Asda2BattlegroundHandler.SendTeamPointsResponse(CurrentBattleGround,
                                                                        this);
                        Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(
                            CurrentBattleGround, this);
                        GlobalHandler.SendFightingModeChangedOnWarResponse(Client, SessionId,
                                                                           (int)AccId,
                                                                           Asda2FactionId);
                        Asda2BattlegroundHandler.SendWarRemainingTimeResponse(Client);
                        if (!CurrentBattleGround.IsStarted)
                        {
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(
                                CurrentBattleGround, BattleGroundInfoMessageType.PreWarCircle,
                                -1);
                        }
                        if (CurrentBattleGround.WarType == Asda2BattlegroundType.Occupation)
                        {
                            foreach (var asda2WarPoint in CurrentBattleGround.Points)
                            {
                                Asda2BattlegroundHandler.SendWarPointsPreInitResponse(Client,
                                                                                      asda2WarPoint);
                            }
                        }
                    }
                    if (MailMessages.Count > 0)
                    {
                        var unreadedCnt =
                            MailMessages.Values.Count(
                                asda2MailMessage => !asda2MailMessage.IsReaded);
                        if (unreadedCnt > 0)
                        {
                            SendMailMsg(String.Format("You have {0} unreaded messages.",
                                                      unreadedCnt));
                            Asda2MailHandler.SendYouHaveNewMailResponse(Client, unreadedCnt);
                        }
                    }
                    if (TeleportPoints.Count(c => c != null) > 0)
                    {
                        Asda2TeleportHandler.SendSavedLocationsInitResponse(Client);
                    }
                    /*if (Asda2Inventory.DonationItems.Count(di => di.Value.Recived == false) >
                        0)
                        Asda2InventoryHandler.SendSomeNewItemRecivedResponse(Client, 20551,
                                                                             102);*/
                    FunctionalItemsHandler.SendWingsInfoResponse(this, Client);
                    foreach (var functionItemBuff in PremiumBuffs)
                    {
                        for (int j = 0; j < functionItemBuff.Value.Stacks; j++)
                        {

                            FunctionalItemsHandler.SendShopItemUsedResponse(Client,
                                                                            functionItemBuff.
                                                                                Value.ItemId,
                                                                            (int)
                                                                            functionItemBuff.
                                                                                Value.Duration /
                                                                            1000);
                        }
                    }
                    //todo It is loading all donation record of character on every chanel change too expensive

                    /*var donationRecords = DonationRecord.FindAllByProperty(
                        "CharacterName", Name).Where(r => !r.IsDelivered).ToList();
                    foreach (var donationRecord in donationRecords)
                    {
                        DonationRecord record = donationRecord;
                        RealmServer.IOQueue.AddMessage(() => Asda2Inventory.AddDonateItem(
                            Asda2ItemMgr.GetTemplate(CharacterFormulas.DonationItemId),
                            record.Amount, "donation_system"));
                        donationRecord.IsDelivered = true;
                        donationRecord.DeliveredDateTime = DateTime.Now;
                        donationRecord.Update();
                    }*/
                });
                foreach (var asda2PetRecord in OwnedPets)
                {
                    Asda2PetHandler.SendInitPetInfoOnLoginResponse(Client, asda2PetRecord.Value);
                }
                if (IsFirstGameConnection)
                {
                    //SendInfoMsg("سيرفر عالم الأسرار 1 تحت التطوير");
                    IsFirstGameConnection = false;
                }
            }

            IsLoginServerStep = false;

            InstanceHandler.SendDungeonDifficulty(this);
            //CharacterHandler.SendVerifyWorld(this);
            //AccountDataHandler.SendAccountDataTimes(m_client);
            //VoiceChatHandler.SendSystemStatus(this, VoiceSystemStatus.Disabled);
            // SMSG_GUILD_EVENT
            // SMSG_GUILD_BANK_LIST
            //CharacterHandler.SendBindUpdate(this, BindLocation);
            TutorialHandler.SendTutorialFlags(this);
            SpellHandler.SendSpellsAndCooldowns(this);
            //CharacterHandler.SendActionButtons(this);
            FactionHandler.SendFactionList(this);
            // SMSG_INIT_WORLD_STATES
            // SMSG_EQUIPMENT_SET_LIST
            AchievementHandler.SendAchievementData(this);
            // SMSG_EXPLORATION_EXPERIENCE
            //CharacterHandler.SendTimeSpeed(this);
            TalentHandler.SendTalentGroupList(m_talents);
            AuraHandler.SendAllAuras(this);
            SendMoneyUpdate();
            // SMSG_PET_GUIDS

            /*using (var con = new MySqlConnection(RealmServerConfiguration.DBConnectionString))
            {
                con.Open();
                var stm = $"UPDATE serverinfo SET OnlinePlayers = {World.CharacterCount.ToString()} WHERE id = 1";
                var cmd = new MySqlCommand(stm, con);
                cmd.ExecuteScalar();

            }*/
        }

        public void ProcessSoulmateRelation(bool callOnSoulmate)
        {
            SoulmateRecord = Asda2SoulmateMgr.GetSoulmateRecord((uint)Account.AccountId);
            if (SoulmateRecord == null)
            {
                Asda2SoulmateHandler.SendDisbandSoulMateResultResponse(Client, DisbandSoulmateResult.SoulmateReleased);
                //Clean up all soulmate relation data
                SoulmateRealmAccount = null;
                SoulmatedCharactersRecords = null;
                return;
            }
            var soulmateAccId = SoulmateRecord.AccId == Account.AccountId
                                    ? SoulmateRecord.RelatedAccId
                                    : SoulmateRecord.AccId;
            var soulmateAcc = AccountMgr.GetAccount(soulmateAccId);
            if (soulmateAcc == null)
            {
                //account was deleted
                SoulmateRecord.DeleteLater();
                SoulmateRecord = null;
                SoulmatedCharactersRecords = null;
                Asda2SoulmateHandler.SendDisbandSoulMateResultResponse(Client, DisbandSoulmateResult.SoulmateReleased);
                return;
            }
            SoulmatedCharactersRecords = CharacterRecord.FindAllOfAccount((int)soulmateAccId);
            SoulmateRealmAccount = ServerApp<RealmServer>.Instance.GetLoggedInAccount(soulmateAcc.Name);
            //Notify about your friend
            if (SoulmateRealmAccount != null && SoulmateRealmAccount.ActiveCharacter != null)
            {
                SoulmateRealmAccount.ActiveCharacter.SoulmateRecord = SoulmateRecord;
                if (callOnSoulmate)
                    SoulmateCharacter.ProcessSoulmateRelation(false);
                Map.CallDelayed(500, () => Asda2SoulmateHandler.SendSoulMateInfoInitResponse(this, true));
                Map.CallDelayed(1000, () => Asda2SoulmateHandler.SendSoulmateEnterdGameResponse(Client));
                Map.CallDelayed(1500, () => Asda2SoulmateHandler.SendSoulMateHpMpUpdateResponse(Client));
                Map.CallDelayed(2000, () => Asda2SoulmateHandler.SendSoulmatePositionResponse(Client));
                SoulmateCharacter.SoulStoneExperience = 0;
                SoulStoneExperience = 0;
                IsSoulmateSafeHandsEnabled = false;
            }
            else
            {
                Asda2SoulmateHandler.SendSoulMateInfoInitResponse(this, false);
            }

        }

        public void AsdaStoryDiscoverTitle(Asda2TitleId titleId)
        {
            if (DiscoveredTitles.GetBit((int)titleId))
                return;

            DiscoveredTitles.SetBit((int)titleId);
            Asda2TitlesHandler.SendTitleDiscoveredResponse(Client, (short)titleId);
        }

        public void AsdaStoryGetTitle(Asda2TitleId titleId)
        {
            if (GetedTitles.GetBit((int)titleId))
                return;
            DiscoveredTitles.UnsetBit((int)titleId);
            GetedTitles.SetBit((int)titleId);
            Asda2TitlePoints += Asda2TitleTemplate.Templates[(int)titleId].Points;
            Asda2TitlesHandler.SendYouGetNewTitleResponse(this, (short)titleId);
        }
        public void DiscoverTitle(Asda2TitleId titleId)
        {
            /*DiscoveredTitles.SetBit((int)titleId);
            Asda2TitlesHandler.SendTitleDiscoveredResponse(Client, (short)titleId);*/
        }

        public void GetTitle(Asda2TitleId titleId)
        {
            /*DiscoveredTitles.UnsetBit((int)titleId);
            GetedTitles.SetBit((int)titleId);
            Asda2TitlePoints += Asda2TitleTemplate.Templates[(int)titleId].Points;
            Asda2TitlesHandler.SendYouGetNewTitleResponse(this, (short)titleId);*/
        }

        public bool isTitleGetted(Asda2TitleId titleId)
        {
            return GetedTitles.GetBit((int)titleId);
        }

        public bool isTitleDiscovered(Asda2TitleId titleId)
        {
            return DiscoveredTitles.GetBit((int)titleId);
        }

        /// <summary>
        /// Reconnects a client to a character that was logging out.
        /// Resends required initial packets.
        /// Called from within the map context.
        /// </summary>
        /// <param name="newClient"></param>
        internal void ReconnectCharacter(IRealmClient newClient)
        {
            // if (chr.LastLogout != null && chr.LastLogout)
            CancelLogout(false);


            newClient.ActiveCharacter = this;
            m_client = newClient;

            ClearSelfKnowledge();
            OnLogin();

            m_lastPlayTimeUpdate = DateTime.Now;

            var evt = LoggedIn;
            if (evt != null)
            {
                evt(this, false);
            }

        }

        #endregion

        #region Save

        /// <summary>
        /// Enqueues saving of this Character to the IO-Queue.
        /// <see cref="SaveNow"/>
        /// </summary>
        public void SaveLater()
        {
            RealmServer.IOQueue.AddMessage(new Message(() => SaveNow()));
        }

        /// <summary>
        /// Saves the Character to the DB instantly.
        /// Blocking call.
        /// See: <see cref="SaveLater()"/>.
        /// When calling this method directly, make sure to set m_saving = true
        /// </summary>
        protected internal bool SaveNow()
        {
            if (!m_record.CanSave)
            {
                return false;
            }

            /*	if (DebugUtil.Dumps)
                {
                    var writer = DebugUtil.GetTextWriter(m_client.Account);
                    writer.WriteLine("Saving {0}...", Name);
                }*/

            try
            {
                if (m_record == null)
                {
                    throw new InvalidOperationException("Cannot save Character while not in world.");
                }
                try
                {
                    UpdatePlayedTime();

                    // always make sure that the values saved to DB, will not be influenced by buffs etc
                    m_record.Race = Race;
                    m_record.Class = Class;
                    m_record.Gender = Gender;
                    m_record.Skin = Skin;
                    m_record.Face = Facial;
                    m_record.HairStyle = HairStyle;
                    m_record.HairColor = HairColor;
                    m_record.FacialHair = FacialHair;
                    m_record.Outfit = Outfit;
                    m_record.Name = Name;
                    m_record.Level = Level;
                    if (m_Map != null)
                    {
                        // only save position information if we are in world
                        m_record.PositionX = Position.X;
                        m_record.PositionY = Position.Y;
                        m_record.PositionZ = Position.Z;
                        m_record.Orientation = Orientation;
                        m_record.MapId = m_Map.Id;
                        m_record.InstanceId = m_Map.InstanceId;
                        m_record.Zone = ZoneId;
                    }
                    m_record.DisplayId = DisplayId;
                    m_record.BindX = m_bindLocation.Position.X;
                    m_record.BindY = m_bindLocation.Position.Y;
                    m_record.BindZ = m_bindLocation.Position.Z;
                    m_record.BindMap = m_bindLocation.MapId;
                    m_record.BindZone = m_bindLocation.ZoneId;

                    m_record.Health = Health;
                    m_record.BaseHealth = BaseHealth;
                    m_record.Power = Power;
                    m_record.BasePower = BasePower;
                    m_record.Money = Money;
                    m_record.WatchedFaction = WatchedFaction;
                    m_record.BaseStrength = Asda2BaseStrength;
                    m_record.BaseStamina = Asda2BaseStamina;
                    m_record.BaseSpirit = Asda2BaseSpirit;
                    m_record.BaseIntellect = Asda2BaseIntellect;
                    m_record.BaseAgility = Asda2BaseAgility;
                    m_record.BaseLuck = Asda2BaseLuck;
                    m_record.Xp = (int)Experience;
                    m_record.RestXp = (int)RestXp;

                    // Honor and Arena
                    m_record.KillsTotal = KillsTotal;
                    m_record.HonorToday = HonorToday;
                    m_record.HonorYesterday = HonorYesterday;
                    m_record.LifetimeHonorableKills = LifetimeHonorableKills;
                    m_record.HonorPoints = HonorPoints;
                    m_record.ArenaPoints = ArenaPoints;
                    m_record.TitlePoints = (uint)Asda2TitlePoints;
                    m_record.Rank = Asda2Rank;
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex,
                                           string.Format("failed to save pre basic ops, character {0} acc {1}[{2}]",
                                                         Name, Account.Name, AccId));
                }
                try
                {
                    PlayerSpells.OnSave();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save spells, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                /*try
                {
                    Record.TitleProgress = JsonHelper.Serialize(TitleProgress);
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save title progress, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }*/
                try
                {
                    foreach (var asda2PetRecord in OwnedPets)
                    {
                        asda2PetRecord.Value.Save();
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save pets, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                try
                {
                    foreach (var registeredFishingBook in RegisteredFishingBooks)
                    {
                        registeredFishingBook.Value.Save();
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save fishing books, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
            }
            catch (Exception e)
            {
                OnSaveFailed(e);
                return false;
            }

            try
            {

                // Interface settings
                try
                {
                    Account.AccountData.Save();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save account data, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                try
                {
                    _asda2Inventory.SaveAll();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save inventory, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }

                try
                {
                    if (m_auras != null)
                        m_auras.SaveAurasNow();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save auras, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                try
                {
                    foreach (var functionItemBuff in PremiumBuffs.Values.ToArray())
                        if (functionItemBuff != null)
                            functionItemBuff.Save();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save functional item buffs, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                try
                {
                    foreach (var longTimePremiumBuff in LongTimePremiumBuffs.ToArray())
                        if (longTimePremiumBuff != null)
                            longTimePremiumBuff.Save();
                }
                catch (Exception ex)
                {
                    LogUtil.WarnException(ex, string.Format("failed to save long time buffs, character {0} acc {1}[{2}]", Name, Account.Name, AccId));
                }
                // General Character data
                m_record.LastSaveTime = DateTime.Now;
                m_record.Save();

                return true;
            }
            catch (Exception ex)
            {
                OnSaveFailed(ex);
                return false;
            }
            finally
            {
            }
        }

        private void OnSaveFailed(Exception ex)
        {
            SendSystemMessage("Saving failed - Please excuse the inconvenience!");

            /*if (DebugUtil.Dumps)
            {
                var writer = DebugUtil.GetTextWriter(m_client.Account);
                writer.WriteLine("Failed to save {0}: {1}", Name, ex);
            }*/

            LogUtil.ErrorException(ex, "Could not save Character " + this);
        }

        #endregion

        #region Logout

        public bool CanLogoutInstantly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// whether the Logout sequence initialized (Client might already be disconnected)
        /// </summary>
        public bool IsLoggingOut
        {
            get { return m_isLoggingOut; }
        }

        /// <summary>
        /// whether the player is currently logging out by itself (not forcefully being logged out).
        /// Players who are forced to logout cannot cancel.
        /// Is false while Client is logged in.
        /// </summary>
        public bool IsPlayerLogout
        {
            get { return _isPlayerLogout; }
            internal set { _isPlayerLogout = value; }
        }

        public bool CanLogout
        {
            get { return !m_IsPinnedDown && !IsInCombat; }
        }

        /// <summary>
        /// Enqueues logout of this player to the Map's queue
        /// </summary>
        /// <param name="forced">whether the Character is forced to logout (as oppose to initializeing logout oneself)</param>
        public void LogoutLater(bool forced)
        {
            AddMessage(() => Logout(forced));
        }

        /// <summary>
        /// Starts the logout process with the default delay (or instantly if
        /// in city or staff)
        /// Requires map context.
        /// </summary>
        /// <param name="forced"></param>
        public void Logout(bool forced)
        {
            Logout(forced, CanLogoutInstantly ? 0 : DefaultLogoutDelayMillis);
        }

        /// <summary>
        /// Starts the logout process.
        /// Disconnects the Client after the given delay in seconds, if not in combat (or instantly if delay = 0)
        /// Requires map context.
        /// </summary>
        /// <param name="forced">whether the Character is forced to logout (as opposed to initializing logout oneself)</param>
        /// <param name="delay">The delay until the client will be disconnected in seconds</param>
        public void Logout(bool forced, int delay)
        {
            if (!m_isLoggingOut)
            {
                m_isLoggingOut = true;

                IsPlayerLogout = !forced;

                CancelAllActions();


                if (forced)
                {
                    Stunned++;
                }

                if (delay <= 0 || forced)
                {
                    FinishLogout();
                }
                else
                {
                    m_logoutTimer.Start(delay);
                }
            }
            else
            {
                if (forced)
                {
                    // logout is now mandatory
                    IsPlayerLogout = false;

                    // reset timer
                    if (delay <= 0)
                    {
                        m_logoutTimer.Stop();
                        FinishLogout();
                    }
                    else
                    {
                        m_logoutTimer.Start(delay);
                    }
                }
            }
        }

        /// <summary>
        /// Cancels whatever this Character currently does
        /// </summary>
        //public override void CancelAllActions()
        //{
        //    base.CancelAllActions();
        //}

        /// <summary>
        /// Cancels logout of this Character.
        /// Requires map context.
        /// </summary>
        public void CancelLogout()
        {
            CancelLogout(true);
        }

        /// <summary>
        /// Cancels whatever this Character currently does
        /// </summary>
        //public override void CancelAllActions()
        //{
        //    base.CancelAllActions();
        //}

        /// <summary>
        /// Cancels logout of this Character.
        /// Requires map context.
        /// </summary>
        /// <param name="sendCancelReply">whether to send the Cancel-reply (if client did not disconnect in the meantime)</param>
        public void CancelLogout(bool sendCancelReply)
        {
            if (m_isLoggingOut)
            {
                if (!IsPlayerLogout)
                {
                    Stunned--;
                }

                m_isLoggingOut = false;
                IsPlayerLogout = false;

                m_logoutTimer.Stop();

                DecMechanicCount(SpellMechanic.Frozen);

                //StandState = StandState.Stand;
                IsSitting = false;


                /*if (sendCancelReply)
                {
                    CharacterHandler.SendLogoutCancelReply(Client);
                }*/
            }
        }

        /// <summary>
        /// Saves and then removes Character
        /// </summary>
        /// <remarks>Requires map context for synchronization.</remarks>
        internal void FinishLogout()
        {
            ServerApp<RealmServer>.IOQueue.AddMessage(new Message(() =>
            {
                Record.LastLogout = DateTime.Now;
                SaveNow();

                var handler = ContextHandler;
                if (handler != null)
                {
                    ContextHandler.AddMessage(
                        () => DoFinishLogout());
                }
                else
                {
                    DoFinishLogout();
                }
            }));
        }

        internal void DoFinishLogout()
        {
            if (!m_isLoggingOut)
            {
                // cancel if logout was cancelled
                return;
            }
            try
            {
                if (SoulmateCharacter != null)
                    Asda2SoulmateHandler.SendSoulmateLoggedOutResponse(SoulmateCharacter.Client);
            }
            catch (Exception ex)
            {
                LogUtil.ErrorException("Failed to notify guild or friend about logut {0},{1},{2}", Name, EntryId, ex.Message);
            }
            CharacterLogoutHandler loggingOut = LoggingOut;
            if (loggingOut != null)
                loggingOut(this);
            if (!World.RemoveCharacter(this))
                return;
            m_client.ActiveCharacter = null;
            Account.ActiveCharacter = null;
            m_isLoggingOut = false;
            RemoveSummonedEntourage();
            DetatchFromVechicle();
            for (int index = ChatChannels.Count - 1; index >= 0; --index)
                ChatChannels[index].Leave(this, true);
            if (Ticket != null)
            {
                Ticket.OnOwnerLogout();
                Ticket = null;
            }

            if (m_TaxiMovementTimer != null)
                m_TaxiMovementTimer.Stop();
            if (Asda2TradeWindow != null)
                Asda2TradeWindow.CancelTrade();
            if (PrivateShop != null)
                PrivateShop.Exit(this);
            Singleton<GroupMgr>.Instance.OnCharacterLogout(m_groupMember);
            Singleton<GuildMgr>.Instance.OnCharacterLogout(m_guildMember);
            Singleton<RelationMgr>.Instance.OnCharacterLogout(this);
            InstanceMgr.OnCharacterLogout(this);
            Asda2BattlegroundMgr.OnCharacterLogout(this);
            Battlegrounds.OnLogout();
            LastLogout = DateTime.Now;
            if (m_corpse != null)
                m_corpse.Delete();
            CancelAllActions();
            m_auras.CleanupAuras();
            m_Map.RemoveObjectNow(this);
            if (!Account.IsActive)
                m_client.Disconnect(false);
            m_initialized = false;
            ServerApp<RealmServer>.Instance.UnregisterAccount(Account);
            Client.Disconnect(false);
            Dispose();
        }

        #endregion


        #region Kick

        // TODO: Log Kicking

        /// <summary>
        /// Kicks this Character with the given msg instantly.
        /// </summary>
        /// <remarks>
        /// Requires map context.
        /// </remarks>
        public void Kick(string msg)
        {
            Kick(null, msg, 0);
        }

        /// <summary>
        /// Kicks this Character with the given msg after the given delay in seconds.
        /// Requires map context.
        /// </summary>
        /// <param name="delay">The delay until the Client should be disconnected in seconds</param>
        public void Kick(string reason, float delay)
        {
            Kick(reason, delay);
        }

        /// <summary>
        /// Broadcasts a kick message and then kicks this Character after the default delay.
        /// Requires map context.
        /// </summary>
        public void Kick(Character kicker, string reason)
        {
            Kick(kicker, reason, DefaultLogoutDelayMillis);
        }

        /// <summary>
        /// Broadcasts a kick message and then kicks this Character after the default delay.
        /// Requires map context.
        /// </summary>
        public void Kick(INamed kicker, string reason, int delay)
        {
            var other = (kicker != null ? " by " + kicker.Name : "") +
                        (!String.IsNullOrEmpty(reason) ? " (" + reason + ")" : ".");
            World.Broadcast("{0} has been kicked{1}", Name, other);

            SendSystemMessage("You have been kicked" + other);

            CancelTaxiFlight();
            Logout(true, delay);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Performs any needed object/object pool cleanup.
        /// </summary>
        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CancelSummon(false);
            if (m_bgInfo != null)
            {
                m_bgInfo.Character = null;
                m_bgInfo = null;
            }

            m_InstanceCollection = null;
            if (m_activePet != null)
            {
                m_activePet.Delete();
                m_activePet = null;
            }

            m_minions = null;
            m_activePet = null;
            if (m_skills != null)
            {
                m_skills.m_owner = null;
                m_skills = null;
            }

            if (m_talents != null)
            {
                m_talents.Owner = null;
                m_talents = null;
            }

            _asda2Inventory = null;
            if (m_mailAccount != null)
            {
                m_mailAccount.Owner = null;
                m_mailAccount = null;
            }

            m_groupMember = null;
            if (m_reputations != null)
            {
                m_reputations.Owner = null;
                m_reputations = null;
            }

            if (m_InstanceCollection != null)
                m_InstanceCollection.Dispose();
            if (m_achievements != null)
            {
                m_achievements.m_owner = null;
                m_achievements = null;
            }

            if (m_CasterReference != null)
            {
                m_CasterReference.Object = null;
                m_CasterReference = null;
            }

            if (m_looterEntry != null)
            {
                m_looterEntry.m_owner = null;
                m_looterEntry = null;
            }

            if (m_ExtraInfo != null)
            {
                m_ExtraInfo.Dispose();
                m_ExtraInfo = null;
            }

            KnownObjects.Clear();
            WorldObjectSetPool.Recycle(KnownObjects);
        }

        /// <summary>
        /// Throws an exception, since logged in Characters may not be deleted
        /// </summary>
        protected internal override void DeleteNow()
        {
            //throw new InvalidOperationException("Cannot delete logged in Character.");
            Client.Disconnect();
        }

        /// <summary>
        /// Throws an exception, since logged in Characters may not be deleted
        /// </summary>
        public override void Delete()
        {
            //throw new InvalidOperationException("Cannot delete logged in Character.");
            Client.Disconnect();
        }

        #endregion

        public string TryAddStatPoints(Asda2StatType statType, int points)
        {
            if (FreeStatPoints <= 0)
                return "Sorry, but you have not free stat points.";
            if (points <= 0 || points > FreeStatPoints)
                return
                    String.Format(
                        "You must enter stat points count from {0} to {1}, but you enter {2}. Failed to increace {3}", 1,
                        FreeStatPoints, points, statType);
            FreeStatPoints -= points;

            Log.Create(Log.Types.StatsOperations, LogSourceType.Character, EntryId)
                                             .AddAttribute("source", 0, "add_stat_points")
                                             .AddAttribute("amount", points)
                                             .AddAttribute("free", FreeStatPoints)
                                             .AddAttribute("stat", (double)statType, statType.ToString())
                                             .Write();
            switch (statType)
            {
                case Asda2StatType.Stamina:
                    Asda2BaseStamina += points;
                    UpdateAsda2Stamina();
                    break;
                case Asda2StatType.Dexterity:
                    Asda2BaseAgility += points;
                    UpdateAsda2Agility();
                    break;
                case Asda2StatType.Strength:
                    Asda2BaseStrength += points;
                    UpdateAsda2Strength();
                    break;
                case Asda2StatType.Intelect:
                    Asda2BaseIntellect += points;
                    UpdateAsda2Intellect();
                    break;
                case Asda2StatType.Spirit:
                    Asda2BaseSpirit += points;
                    UpdateAsda2Spirit();
                    break;
                case Asda2StatType.Luck:
                    Asda2BaseLuck += points;
                    UpdateAsda2Luck();
                    break;
            }
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            return String.Format("Succeful increase {0}. Now you have {1} free stat points.", statType, FreeStatPoints);
        }

        public void ResetStatPoints()
        {
            Asda2BaseStrength = 1;
            Asda2BaseIntellect = 1;
            Asda2BaseAgility = 1;
            Asda2BaseSpirit = 1;
            Asda2BaseStamina = 1;
            Asda2BaseLuck = 1;
            FreeStatPoints = CharacterFormulas.CalculateFreeStatPointForLevel(Level, Record.RebornCount);
            Log.Create(Log.Types.StatsOperations, LogSourceType.Character, EntryId)
                                             .AddAttribute("source", 0, "reset_stat_points")
                                             .AddAttribute("free", FreeStatPoints)
                                             .Write();
            UpdateAsda2Agility();
            UpdateAsda2Strength();
            UpdateAsda2Stamina();
            UpdateAsda2Luck();
            UpdateAsda2Spirit();
            UpdateAsda2Intellect();
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
        }

        public static uint CharacterIdFromAccIdAndCharNum(int targetAccId, short targetCharNumOnAcc)
        {
            return (uint)(targetAccId + 1000000 * targetCharNumOnAcc);
        }

        #region Implementation of IPacketReceiver

        public bool IsRussianClient { get; set; }

        #endregion

        public bool IsFromFriendDamageBonusApplied { get; set; }

        public bool IsSoulmateEmpowerPositive { get; set; }

        public bool IsSoulmateSafeHandsKeeper { get; set; }

        public bool IsSoulSongEnabled { get; set; }
        public DateTime SoulmateSongEndTime { get; set; }

        public void AddSoulmateSong()
        {
            SoulmateSongEndTime = DateTime.Now.AddMinutes(30);
            if (IsSoulSongEnabled)
                return;
            IsSoulSongEnabled = true;
            SendInfoMsg("You feeling soulmate song effect !!!");

            this.ChangeModifier(StatModifierFloat.Strength, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Luck, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Agility, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Intelect, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Spirit, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Stamina, CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Damage, CharacterFormulas.SoulmateSongDamageBonusPrc);
            this.ChangeModifier(StatModifierFloat.MagicDamage, CharacterFormulas.SoulmateSongDamageBonusPrc);
            this.ChangeModifier(StatModifierFloat.Asda2Defence, CharacterFormulas.SoulmateSongDeffenceBonusPrc);
            this.ChangeModifier(StatModifierFloat.Asda2MagicDefence, CharacterFormulas.SoulmateSongDeffenceBonusPrc);

            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }

        public void RemoveSoulmateSong()
        {
            if (!IsSoulSongEnabled)
                return;
            IsSoulSongEnabled = false;
            SendInfoMsg("Soulmate song effect removed.");

            this.ChangeModifier(StatModifierFloat.Strength, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Luck, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Agility, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Intelect, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Spirit, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Stamina, -CharacterFormulas.SoulmateSongStatBonusPrc);
            this.ChangeModifier(StatModifierFloat.Damage, -CharacterFormulas.SoulmateSongDamageBonusPrc);
            this.ChangeModifier(StatModifierFloat.MagicDamage, -CharacterFormulas.SoulmateSongDamageBonusPrc);
            this.ChangeModifier(StatModifierFloat.Asda2Defence, -CharacterFormulas.SoulmateSongDeffenceBonusPrc);
            this.ChangeModifier(StatModifierFloat.Asda2MagicDefence, -CharacterFormulas.SoulmateSongDeffenceBonusPrc);

            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }

        public void AddFriendEmpower(bool positive)
        {
            SoulmateEmpowerEndTime = DateTime.Now.AddMinutes(5);
            if (IsSoulmateEmpowerEnabled)
            {
                SendInfoMsg("Soulmate empower duration updated.");
                return;
            }

            IsSoulmateEmpowerEnabled = true;
            IsSoulmateEmpowerPositive = positive;
            if (IsSoulmateEmpowerPositive)
            {
                SendInfoMsg("You feeling positive soulmate empower effect.");
                this.ChangeModifier(StatModifierFloat.Damage, CharacterFormulas.FriendEmpowerDamageBonus);
                this.ChangeModifier(StatModifierFloat.MagicDamage, CharacterFormulas.FriendEmpowerDamageBonus);
            }
            else
            {
                this.ChangeModifier(StatModifierFloat.Damage, -CharacterFormulas.FriendEmpowerDamageBonus * 2);
                this.ChangeModifier(StatModifierFloat.MagicDamage, -CharacterFormulas.FriendEmpowerDamageBonus * 2);
                SendInfoMsg("You feeling negative soulmate empower effect.");
            }
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }

        public void AddSafeHands(bool positive)
        {
            SoulmateSafeHandsEndTime = DateTime.Now.AddMinutes(30);
            if (IsSoulmateSafeHandsEnabled)
            {
                SendInfoMsg("تم تحديث مدة مهارة الايدي الامينة.");
                return;
            }

            IsSoulmateSafeHandsEnabled = true;
            IsSoulmateSafeHandsKeeper = positive;
            SoulmateCharacter.Client.Disconnect();
        }

        public void RemoveFriendEmpower()
        {
            if (!IsSoulmateEmpowerEnabled)
                return;
            SendInfoMsg("Soulmate empower effect removed.");
            IsSoulmateEmpowerEnabled = false;
            if (IsSoulmateEmpowerPositive)
            {
                this.ChangeModifier(StatModifierFloat.Damage, -CharacterFormulas.FriendEmpowerDamageBonus);
                this.ChangeModifier(StatModifierFloat.MagicDamage, -CharacterFormulas.FriendEmpowerDamageBonus);
            }
            else
            {
                this.ChangeModifier(StatModifierFloat.Damage, CharacterFormulas.FriendEmpowerDamageBonus * 2);
                this.ChangeModifier(StatModifierFloat.MagicDamage, CharacterFormulas.FriendEmpowerDamageBonus * 2);
            }
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }

        public void AddFromFriendDamageBonus()
        {
            if (IsFromFriendDamageBonusApplied)
                return;
            IsFromFriendDamageBonusApplied = true;
            this.ChangeModifier(StatModifierFloat.Damage, CharacterFormulas.NearFriendDamageBonus);
            this.ChangeModifier(StatModifierFloat.MagicDamage, CharacterFormulas.NearFriendDamageBonus);
            this.ChangeModifier(StatModifierFloat.Asda2MagicDefence, CharacterFormulas.NearFriendDeffenceBonus);
            this.ChangeModifier(StatModifierFloat.Asda2Defence, CharacterFormulas.NearFriendDeffenceBonus);
            this.ChangeModifier(StatModifierFloat.Speed, CharacterFormulas.NearFriendSpeedBonus);
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }


        public void RemoveFromFriendDamageBonus()
        {
            if (!IsFromFriendDamageBonusApplied)
                return;
            IsFromFriendDamageBonusApplied = false;
            this.ChangeModifier(StatModifierFloat.Damage, -CharacterFormulas.NearFriendDamageBonus);
            this.ChangeModifier(StatModifierFloat.MagicDamage, -CharacterFormulas.NearFriendDamageBonus);
            this.ChangeModifier(StatModifierFloat.Asda2MagicDefence, -CharacterFormulas.NearFriendDeffenceBonus);
            this.ChangeModifier(StatModifierFloat.Asda2Defence, -CharacterFormulas.NearFriendDeffenceBonus);
            this.ChangeModifier(StatModifierFloat.Speed, -CharacterFormulas.NearFriendSpeedBonus);
            Asda2CharacterHandler.SendUpdateStatsOneResponse(Client);
            Asda2CharacterHandler.SendUpdateStatsResponse(Client);
        }

        public void RemovaAllSoulmateBonuses()
        {
            RemoveFriendEmpower();
            RemoveFromFriendDamageBonus();
            RemoveSoulmateSong();
            IsSoulmateSoulSaved = false;
        }

    }
}