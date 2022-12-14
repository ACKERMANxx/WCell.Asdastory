using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using NLog;
using WCell.Constants;
using WCell.Constants.Login;
using WCell.Constants.NPCs;
using WCell.Constants.Spells;
using WCell.Constants.Updates;
using WCell.Constants.World;
using WCell.Core;
using WCell.Core.Database;
using WCell.RealmServer.Achievements;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Groups;
using WCell.RealmServer.Guilds;
using WCell.RealmServer.Instances;
using WCell.RealmServer.Interaction;
using WCell.RealmServer.Items;
using WCell.RealmServer.Mail;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.NPCs.Pets;
using WCell.RealmServer.RacesClasses;
using WCell.RealmServer.Talents;
using WCell.Util.Graphics;
using WCell.Util.NLog;
using WCell.Util.Threading;
using Alias = System.Collections.Generic.KeyValuePair<string, string>;


namespace WCell.RealmServer.Database
{
	[ActiveRecord(Access = PropertyAccess.Property)]
	public class CharacterRecord : WCellRecord<CharacterRecord>, ILivingEntity, IMapId, IActivePetSettings
	{
		#region Static
		private static readonly Logger s_log = LogManager.GetCurrentClassLogger();

		public static readonly CharacterRecord[] EmptyArray = new CharacterRecord[] { };

		/// <summary>
		/// Character will not have Ids below this threshold. 
		/// You can use those unused ids for self-implemented mechanisms, eg to fake participants in chat-channels etc.
		/// </summary>
		/// <remarks>
		/// Do not change this value once the first Character exists.
		/// If you want to change this value to reserve more (or less) ids for other use, make sure
		/// that none of the ids below this threshold are in the DB.
		/// </remarks>
		public const long LowestCharId = 1000;

		protected static readonly NHIdGenerator _idGenerator = new NHIdGenerator(typeof(CharacterRecord), "Guid", LowestCharId);

		/// <summary>
		/// Returns the next unique Id for a new Character
		/// </summary>
		public static uint NextId()
		{
			return (uint)_idGenerator.Next();
		}

		/// <summary>
		/// Creates a new CharacterRecord row in the database with the given information.
		/// </summary>
		/// <param name="account">the account this character is on</param>
		/// <param name="name">the name of the new character</param>
		/// <returns>the <seealso cref="CharacterRecord"/> object</returns>
		public static CharacterRecord CreateNewCharacterRecord(RealmAccount account, string name)
		{
			CharacterRecord record;

			try
			{
				record = new CharacterRecord(account.AccountId)
				{
					EntityLowId = (uint)_idGenerator.Next(),
					Name = name,
					Created = DateTime.Now
				};
			}
			catch (Exception ex)
			{
				s_log.Error("Character creation error (DBS: " + RealmServerConfiguration.DatabaseType + "): ", ex);
				record = null;
			}

			return record;
		}

		/// <summary>
		/// Retrieves a CharacterRecord based on the character name
		/// </summary>
		/// <param name="name">the character name</param>
		/// <returns>the corresponding <seealso cref="CharacterRecord"/></returns>
		public static CharacterRecord GetRecordByName(string name)
		{
			try
			{
				return FindOne(Restrictions.Like("Name", name));
			}
			catch (Exception ex)
			{
				RealmDBMgr.OnDBError(ex);
				return null;
			}
		}

        public static CharacterRecord GetRecordByAccountId(uint AccountId)
        {
            try
            {
                return FindOne(Restrictions.Like("AccountId", AccountId));
            }
            catch (Exception ex)
            {
                RealmDBMgr.OnDBError(ex);
                return null;
            }
        }
        /// <summary>
        /// Checks if a character with the given name already exists.
        /// </summary>
        /// <param name="characterName">the name to check for</param>
        /// <returns>true if the character exists; false otherwise</returns>
        public static bool Exists(string characterName)
		{
			try
			{
				return Exists((ICriterion)Restrictions.Like("Name", characterName));
			}
			catch (Exception ex)
			{
				RealmDBMgr.OnDBError(ex);
				return false;
			}
		}

		/// <summary>
		/// Checks if a character with the given Id already exists.
		/// </summary>
		public static bool Exists(uint entityLowId)
		{
			try
			{
				return Exists((ICriterion)Restrictions.Eq("Guid", (long)entityLowId));
			}
			catch (Exception ex)
			{
				RealmDBMgr.OnDBError(ex);
				return false;
			}
		}

		/// <summary>
		/// Retrieves a CharacterRecord based on a character's entity ID
		/// </summary>
		/// <param name="lowUid">the character unique ID</param>		/// <returns>the corresponding <seealso cref="CharacterRecord"/></returns>
		public static CharacterRecord LoadRecordByEntityId(uint lowUid)
		{
			return FindOne(Restrictions.Eq("Guid", (long)lowUid));
		}

		/// <summary>
		/// Retrieves a CharacterRecord based on a character's entity ID.
		/// </summary>
		/// <returns>the corresponding <seealso cref="CharacterRecord"/></returns>
		public static CharacterRecord LoadRecordByID(long guid)
		{
			return FindOne(Restrictions.Eq("CharacterId", guid));
		}

		public static int GetCount()
		{
			return Count();
		}
		#endregion

		[Field("DisplayId", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
		private int _displayId;
		[Field("WatchedFaction", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
		private int _watchedFaction;
		[Field("ClassId", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
		private int m_Class;
		[Field("Map", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
		private int m_Map;
		[Field("CorpseMap", Access = PropertyAccess.FieldCamelcase)]
		private int m_CorpseMap;
		[Field("Zone", Access = PropertyAccess.FieldCamelcase)]
		private int m_zoneId;
		[Field("BindZone", Access = PropertyAccess.FieldCamelcase)]
		private int m_BindZone;
		[Field("BindMap", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
		private int m_BindMap;

		private DateTime? m_lastLogin;


		protected CharacterRecord()
		{
			CanSave = true;
			AbilitySpells = new List<SpellRecord>();
		}

		public CharacterRecord(int accountId)
			: this()
		{
			State = RecordState.New;
			JustCreated = true;

			AccountId = accountId;
			ExploredZones = new byte[UpdateFieldMgr.ExplorationZoneFieldSize * 4];

		}

		public virtual Character CreateCharacter()
		{
			return new Character();
		}

		public bool JustCreated
		{
			get;
			internal set;
		}

		/// <summary>
		/// Whether this record should be saved to DB
		/// </summary>
		public bool CanSave
		{
			get;
			set;
		}

		public DateTime LastSaveTime
		{
			get;
			internal set;
		}

        [Property]
        public int shirt
        {
            get;
            set;
        }
        [Property]
        public int gloves
        {
            get;
            set;
        }
        [Property]
        public int pants
        {
            get;
            set;
        }
        [Property]
        public int shoes
        {
            get;
            set;
        }

        #region Misc

        [PrimaryKey(PrimaryKeyType.Assigned, "EntityLowId")]
		public long Guid
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int AccountId
		{
			get;
			set;
		}

		public uint EntityLowId
		{
			get
			{
				return (uint)Guid;
			}
			set
			{
				Guid = (int)value;
			}
		}

		public EntityId EntityId
		{
			get
			{
				return EntityId.GetPlayerId(EntityLowId);
			}
		}

		[Property(Length = 12, NotNull = true, Unique = true)]
		public string Name
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public DateTime Created
		{
			get;
			set;
		}

        [Property]
        public DateTime? BanChatTill
        {
            get;
            set;
        }
		/// <summary>
		/// Whether the Character that this Record belongs to is currently logged in.
		/// </summary>
		[Property(Access = PropertyAccess.ReadOnly)]
		public bool IsOnline
		{
			get { return LastLogin != null && LastLogin > RealmServer.StartTime && (LastLogout == null || LastLogout < LastLogin); }
		}

		[Property]
		public DateTime? LastLogin
		{
			get
			{
				return m_lastLogin;
			}
			set
			{
				m_lastLogin = value;
				if (m_lastLogin == null)
				{
					JustCreated = true;
				}
			}
		}

		[Property]
		public DateTime? LastLogout
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public CharEnumFlags CharacterFlags
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public RaceId Race
		{
			get;
			set;
		}

		public ClassId Class
		{
			get { return (ClassId)m_Class; }
			set { m_Class = (int)value; }
		}

		[Property(NotNull = true)]
		public GenderType Gender
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public byte Skin
		{
			get;
			set;
		}

		[Property("face", NotNull = true)]
		public byte Face
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public byte HairStyle
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public byte HairColor
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public byte FacialHair
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public byte Outfit
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int Level
		{
			get;
			set;
		}

		[Property]
		public int Xp
		{
			get;
			set;
		}

		public int WatchedFaction
		{
			get { return _watchedFaction; }
			set { _watchedFaction = value; }
		}

		public uint DisplayId
		{
			get { return (uint)_displayId; }
			set { _displayId = (int)value; }
		}

		[Property(NotNull = true)]
		public int TotalPlayTime
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int LevelPlayTime
		{
			get;
			set;
		}

		[Property(ColumnType = "BinaryBlob", Length = 32, NotNull = true)]
		public byte[] TutorialFlags
		{
			get;
			set;
		}

		[Property(ColumnType = "BinaryBlob")]
		public byte[] ExploredZones
		{
			get;
			set;
		}

		#endregion

		#region Location
		[Property(NotNull = true)]
		public float PositionX
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public float PositionY
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public float PositionZ
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public float Orientation
		{
			get;
			set;
		}

		public MapId MapId
		{
			get { return (MapId)m_Map; }
			set { m_Map = (int)value; }
		}

		public uint InstanceId
		{
			get;
			set;
		}

		public ZoneId Zone
		{
			get { return (ZoneId)m_zoneId; }
			set { m_zoneId = (int)value; }
		}

		#endregion

		#region Death
		public DateTime LastDeathTime
		{
			get;
			set;
		}

		/// <summary>
		/// Time of last resurrection
		/// </summary>
		public DateTime LastResTime
		{
			get;
			set;
		}

		public MapId CorpseMap
		{
			get { return (MapId)m_CorpseMap; }
			set { m_CorpseMap = (int)value; }
		}

		/// <summary>
		/// If CorpseX is null, there is no Corpse
		/// </summary>
		[Property]
		public float? CorpseX
		{
			get;
			set;
		}

		[Property]
		public float CorpseY
		{
			get;
			set;
		}

		[Property]
		public float CorpseZ
		{
			get;
			set;
		}

		[Property]
		public float CorpseO
		{
			get;
			set;
		}
		#endregion

		#region InnKeeper binding
		[Property(NotNull = true)]
		public float BindX
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public float BindY
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public float BindZ
		{
			get;
			set;
		}

        public MapId BindMap
		{
			get { return (MapId)m_BindMap; }
			set { m_BindMap = (int)value; }
		}

		public ZoneId BindZone
		{
			get { return (ZoneId)m_BindZone; }
			set { m_BindZone = (int)value; }
		}

		#endregion

		#region Spells & Auras & Runes
		/// <summary>
		/// Default spells; talents excluded.
		/// Talent spells can be found in <see cref="SpecProfile"/>.
		/// </summary>
		public List<SpellRecord> AbilitySpells
		{
			get;
			private set;
		}

		[Property]
		public int RuneSetMask
		{
			get;
			set;
		}

		[Property]
		public float[] RuneCooldowns
		{

			get;
			set;
		}
		#endregion

		#region Stats
		[Property(NotNull = true)]
		public int BaseStrength
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BaseStamina
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BaseSpirit
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BaseIntellect
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BaseAgility
		{
			get;
			set;
		}
		#endregion

		[Property]
		public bool GodMode
		{
			get;
			set;
		}
        [Property]
        public byte ProfessionLevel { get; set; }
		#region Dynamic Properties
		[Property(NotNull = true)]
		public int Health
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BaseHealth
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int Power
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public int BasePower
		{
			get;
			set;
		}

		[Property(NotNull = true)]
		public long Money
		{
			get;
			set;
		}
        [Property(NotNull = true)]
        public byte PetBoxEnchants
        {
            get;
            set;
        }
        [Property(NotNull = true)]
        public byte MountBoxExpands
        {
            get;
            set;
        }
		#endregion

		#region Skills
		public SkillRecord[] LoadSkills()
		{
			return SkillRecord.GetAllSkillsFor(Guid);
		}
		#endregion

		#region Reputations
		internal ReputationRecord CreateReputationRecord()
		{
			return new ReputationRecord { OwnerId = Guid };
		}
		#endregion

		#region Auras
		#endregion

		#region Quests
		[Property("FinishedQuests", NotNull = false)]
		public uint[] FinishedQuests
		{
			get;
			set;
		}

		[Property("FinishedDailyQuests", NotNull = false)]
		public uint[] FinishedDailyQuests
		{
			get;
			set;
		}
		#endregion

		#region Mail
		public int MailCount
		{
			get
			{
				return MailMessage.Count("ReceiverId = " + (uint)Guid);
			}
		}
		#endregion

		#region Guilds
		[Field("GuildId", Access = PropertyAccess.FieldCamelcase)]
		private int m_GuildId;

		public uint GuildId
		{
			get { return (uint)m_GuildId; }
			set { m_GuildId = (int)value; }
		}

		#endregion

		#region Items
		private void LoadItems()
		{
			try
			{
				//m_loadedItems = Asda2ItemRecord.LoadItems(EntityLowId);
                _asda2LoadedItems = Asda2ItemRecord.LoadItems(EntityLowId);
              			    _asda2LoadedFastItemSlots = Asda2FastItemSlotRecord.LoadItems(EntityLowId);
			}
			catch (Exception e)
			{
				RealmDBMgr.OnDBError(e);
                //m_loadedItems = Asda2ItemRecord.LoadItems(EntityLowId);
                _asda2LoadedItems = Asda2ItemRecord.LoadItems(EntityLowId);
                _asda2LoadedFastItemSlots = Asda2FastItemSlotRecord.LoadItems(EntityLowId);
			}
		}
        private void LoadFastItemSlots()
        {
            try
            {
                _asda2LoadedFastItemSlots = Asda2FastItemSlotRecord.LoadItems(EntityLowId);
            }
            catch (Exception e)
            {
                RealmDBMgr.OnDBError(e);
                _asda2LoadedFastItemSlots = Asda2FastItemSlotRecord.LoadItems(EntityLowId);
            }
        }
        public ICollection<Asda2ItemRecord> GetOrLoadItems()
		{
            if (_asda2LoadedItems == null)
			{
				LoadItems();
			}

            return _asda2LoadedItems;
		}
        public ICollection<Asda2FastItemSlotRecord> GetOrLoadFastItemSlots()
        {
            if (_asda2LoadedFastItemSlots == null)
            {
                LoadFastItemSlots();
            }

            return _asda2LoadedFastItemSlots;
        }

		public List<Asda2ItemRecord> GetMailItems(long mailId, int count)
		{
			var items = new List<Asda2ItemRecord>(count);
			foreach (var item in Asda2LoadedItems)
			{
				if (item.MailId == mailId)
				{
					items.Add(item);
				}
			}
			return items;
		}

		//[HasMany(typeof(EquipmentSet), Inverse = true, Cascade = ManyRelationCascadeEnum.AllDeleteOrphan)]
		public IList<EquipmentSet> EquipmentSets
		{
			get;
			set;
		}
		#endregion

		#region Resting
		/// <summary>
		/// Amount of accumulated rest-XP 
		/// </summary>
		[Property]
		public int RestXp
		{
			get;
			set;
		}

		/// <summary>
		/// The id of the AreaTrigger which is letting us rest (or 0 if there is none)
		/// </summary>
		[Property]
		public int RestTriggerId
		{
			get;
			set;
		}
		#endregion

		#region Taxis
		[Property]
		public int NextTaxiVertexId
		{
			get;
			set;
		}

		[Property]
		public uint[] TaxiMask
		{
			get;
			set;
		}
		#endregion

		#region Pets
		[Field("SummonSpell")]
		private int m_SummonSpellId;

		[Field("PetEntryId")]
		private int m_PetEntryId;

		[Property]
		public bool IsPetActive
		{
			get;
			set;
		}

		[Property]
		public int StableSlotCount
		{
			get;
			set;
		}

		/// <summary>
		/// Amount of action-bar information etc for summoned pets
		/// </summary>
		[Property]
		public int PetSummonedCount
		{
			get;
			set;
		}

		/// <summary>
		/// Amount of Hunter pets
		/// </summary>
		[Property]
		public int PetCount
		{
			get;
			set;
		}

		public NPCId PetEntryId
		{
			get { return (NPCId)m_PetEntryId; }
			set { m_PetEntryId = (int)value; }
		}

		public NPCEntry PetEntry
		{
			get { return PetEntryId != 0 ? NPCMgr.GetEntry(PetEntryId) : null; }
		}

		[Property]
		public int PetHealth
		{
			get;
			set;
		}

		[Property]
		public int PetPower
		{
			get;
			set;
		}
        [Property]
        public int PrivatePerLevelItemBonusTemplateId { get; set; }
		public SpellId PetSummonSpellId
		{
			get { return (SpellId)m_SummonSpellId; }
			set { m_SummonSpellId = (int)value; }
		}

		/// <summary>
		/// Remaining duration in millis
		/// </summary>
		[Property]
		public int PetDuration
		{
			get;
			set;
		}
		#endregion
		#region Talents
		public int CurrentSpecIndex
		{
			get;
			set;
		}

		[Property("LastTalentResetTime")]
		public DateTime? LastTalentResetTime
		{
			get;
			set;
		}

		[Field("TalentResetPriceTier", NotNull = true)]
		private int _talentResetPriceTier;

		public int TalentResetPriceTier
		{
			get { return _talentResetPriceTier; }
			set
			{
				if (value < 0)
				{
					value = 0;
				}
				if (value > (TalentMgr.PlayerTalentResetPricesPerTier.Length - 1))
				{
					value = (TalentMgr.PlayerTalentResetPricesPerTier.Length - 1);
				}
				_talentResetPriceTier = value;
			}
		}
		#endregion

		#region Instances & BGs
		private BattlegroundSide m_BattlegroundTeam = BattlegroundSide.End;

		[Property]
		public DungeonDifficulty DungeonDifficulty
		{
			get;
			set;
		}

		[Property]
		public RaidDifficulty RaidDifficulty
		{
			get;
			set;
		}

		[Property]
		public BattlegroundSide BattlegroundTeam
		{
			get { return m_BattlegroundTeam; }
			set { m_BattlegroundTeam = value; }
		}
		#endregion

		#region Honor & Arena

		[Field("KillsTotal", NotNull = true)]
		private int _killsTotal;
		[Field("HonorToday", NotNull = true)]
		private int _honorToday;
		[Field("HonorYesterday", NotNull = true)]
		private int _honorYesterday;
		[Field("LifetimeHonorableKills", NotNull = true)]
		private int _lifetimeHonorableKills;
		[Field("HonorPoints", NotNull = true)]
		private int _honorPoints;
		[Field("ArenaPoints", NotNull = true)]
		private int _arenaPoints;
        [Field("Quest1", NotNull = true)] private int _quest1;
        [Field("Quest2", NotNull = true)] private int _quest2;
        [Field("Quest3", NotNull = true)] private int _quest3;
        [Field("Quest4", NotNull = true)] private int _quest4;
        [Field("Quest5", NotNull = true)] private int _quest5;
        [Field("Quest6", NotNull = true)] private int _quest6;
        [Field("Quest7", NotNull = true)] private int _quest7;
        [Field("Quest8", NotNull = true)] private int _quest8;
        [Field("Quest9", NotNull = true)] private int _quest9;
        [Field("Quest10", NotNull = true)] private int _quest10;
        [Field("Quest11", NotNull = true)] private int _quest11;
        [Field("Quest12", NotNull = true)] private int _quest12;
		[Field("FirstTree", NotNull = true)] private int _ftree;
		[Field("SecondTree", NotNull = true)] private int _stree;
		[Field("ThirdTree", NotNull = true)] private int _ttree;
		[Field("FourthTree", NotNull = true)] private int _4thtree;
		public uint KillsTotal
		{
			get { return (uint)_killsTotal; }
			set { _killsTotal = (int)value; }
		}

		public uint HonorToday
		{
			get { return (uint)_honorToday; }
			set { _honorToday = (int)value; }
		}

		public uint HonorYesterday
		{
			get { return (uint)_honorYesterday; }
			set { _honorYesterday = (int)value; }
		}

		public uint LifetimeHonorableKills
		{
			get { return (uint)_lifetimeHonorableKills; }
			set { _lifetimeHonorableKills = (int)value; }
		}

		public uint HonorPoints
		{
			get { return (uint)_honorPoints; }
			set { _honorPoints = (int)value; }
		}

        public int Quest1
        {
            get { return _quest1; }
            set { _quest1 = value; }
        }
        public int Quest2
        {
            get { return _quest2; }
            set { _quest2 = value; }

        }
        public int Quest3
        {
            get { return _quest3; }
            set { _quest3 = value; }
        }
        public int Quest4
        {
            get { return _quest4; }
            set { _quest4 = value; }
        }
        public int Quest5
        {
            get { return _quest5; }
            set { _quest5 = value; }
        }
        public int Quest6
        {
            get { return _quest6; }
            set { _quest6 = value; }
        }
        public int Quest7
        {
            get { return _quest7; }
            set { _quest7 = value; }
        }
        public int Quest8
        {
            get { return _quest8; }
            set { _quest8 = value; }
        }
        public int Quest9
        {
            get { return _quest9; }
            set { _quest9 = value; }
        }
        public int Quest10
        {
            get { return _quest10; }
            set { _quest10 = value; }
        }
        public int Quest11
        {
            get { return _quest11; }
            set { _quest11 = value; }
        }
        public int Quest12
        {
            get { return _quest12; }
            set { _quest12 = value; }
        }
		public int FirstTree
		{
			get { return _ftree; }
			set { _ftree = value; }
		}
		public int SecondTree
		{
			get { return _stree; }
			set { _stree = value; }
		}
		public int ThirdTree
		{
			get { return _ttree; }
			set { _ttree = value; }
		}
		public int FourthTree
		{
			get { return _4thtree; }
			set { _4thtree = value; }
		}
		public uint ArenaPoints
		{
			get { return (uint)_arenaPoints; }
			set { _arenaPoints = (int)value; }
		}

        #endregion

        #region Titles
        [Field("TitlePoints", NotNull = true)]
        private int _titlePoints;
        [Field("Rank", NotNull = true)]
        private int _rank;
        public uint TitlePoints
        {
            get { return (uint)_titlePoints; }
            set { _titlePoints = (int)value; }
        }

        public int Rank
        {
            get { return _rank; }
            set { _rank = value; }
        }
        #endregion

        #region Delete
        public void DeleteLater()
		{
			RealmServer.IOQueue.AddMessage(new Message(Delete));
		}

		public override void Delete()
		{
			TryDelete();
		}

		public override void DeleteAndFlush()
		{
			TryDelete();
		}

		public LoginErrorCode TryDelete()
		{
			if (DeleteCharAccessories(EntityLowId))
			{
				DeleteFromGuild(EntityLowId, GuildId);
				base.DeleteAndFlush();
				return LoginErrorCode.CHAR_DELETE_SUCCESS;
			}
			return LoginErrorCode.CHAR_DELETE_FAILED;
		}

		public static void DeleteChar(uint charId)
		{
			RealmServer.IOQueue.ExecuteInContext(() =>
			{
				var chr = World.GetCharacter(charId);
				uint guildId;
				if (chr != null)
				{
					guildId = chr.GuildId;
					chr.Client.Disconnect();
				}
				else
				{
					guildId = GetGuildId(charId);
				}

				if (DeleteCharAccessories(charId))
				{
					DeleteFromGuild(charId, guildId);
					DeleteAll("Guid = " + charId);
				}
			});
		}

		private static void DeleteFromGuild(uint charId, uint guildId)
		{
			if (guildId != 0)
			{
				var guild = GuildMgr.GetGuild(guildId);
				if (guild != null)
				{
					guild.RemoveMember(charId);
				}
			}
		}

		static bool DeleteCharAccessories(uint charId)
		{
			try
			{
				SpellRecord.DeleteAll("OwnerId = " + charId);
				AuraRecord.DeleteAll("OwnerId = " + charId);
				ItemRecord.DeleteAll("OwnerId = " + charId);
				SkillRecord.DeleteAll("OwnerId = " + charId);
				SpecProfile.DeleteAll("CharacterId = " + charId);
				ReputationRecord.DeleteAll("OwnerId = " + charId);
				QuestRecord.DeleteAll("OwnerId = " + charId);
				SummonedPetRecord.DeleteAll("OwnerLowId = " + charId);
				PermanentPetRecord.DeleteAll("OwnerLowId = " + charId);

				MailMgr.ReturnValueMailFor(charId);
				MailMessage.DeleteAll("ReceiverId = " + charId);

				RelationMgr.Instance.RemoveRelations(charId);
				InstanceMgr.RemoveLog(charId);
				GroupMgr.Instance.RemoveOfflineCharacter(charId);
				AchievementRecord.DeleteAll("CharacterId = " + charId);
				AchievementProgressRecord.DeleteAll("CharacterId = " + charId);

				return true;
			}
			catch (Exception ex)
			{
				LogUtil.ErrorException(ex, "Failed to delete character with Id: " + charId);

				return false;
			}
		}
		#endregion

		#region Setup
		public void SetupNewRecord(Archetype archetype, int vshirt, int vgloves, int vpants, int vshoes)
		{
			Race = archetype.Race.Id;
			Class = archetype.Class.Id;
			Level = 1;
			PositionX = 3115;
			PositionY = 3310;
			PositionZ = 0;
			Orientation = archetype.StartOrientation;
			MapId = MapId.Alpia;
			Zone = ZoneId.None;
			TotalPlayTime = 0;
			LevelPlayTime = 0;
			TutorialFlags = new byte[32];
			WatchedFaction = -1;
            BindMap = MapId.Alpia;
            BindX = 3115;
            BindY = 3310;
		    BindZ = 0;
            BindZone = ZoneId.None;
		    FreeStatPoints = CharacterFormulas.FreestatPointsOnStart;
		    Money = 1;
			DisplayId = archetype.Race.GetDisplayId(Gender);
		    GlobalChatColor = Color.Yellow;
            SettingsFlags = new byte[19];
		    for (int i = 0; i < 16; i++)
		    {
		        SettingsFlags[i] = 1;
		    }
            AvatarMask = 63;
		    Asda2FactionId = -1;
            DiscoveredTitles = new uint[16];
            GetedTitles = new uint[16];
		    PreTitleId = -1;
		    PostTitleId = -1;
            LearnedRecipes = new uint[18];
		    CraftingLevel = 1;
			shirt = vshirt;
			pants = vpants;
			gloves = vgloves;
			shoes = vshoes;
		}
		#endregion

		#region Find & Get
		/// <summary>
		/// Gets the characters for the given account.
		/// </summary>
		/// <param name="account">the account</param>
		/// <returns>a collection of character objects of the characters on the given account</returns>
		public static CharacterRecord[] FindAllOfAccount(RealmAccount account)
		{
			CharacterRecord[] chrs;
			try
			{
				chrs = FindAllByProperty("Created", "AccountId", account.AccountId);
				//var chrs = FindAllByProperty("Created", "AccountId", account.AccountId);
				//chrs.Reverse();
				//return chrs;
			}
			catch (Exception ex)
			{
				RealmDBMgr.OnDBError(ex);
				chrs = FindAllByProperty("Created", "AccountId", account.AccountId);
			}
			//chrs.Reverse();
			return chrs;
		}
        /// <summary>
        /// Gets the characters for the given account.
        /// </summary>
        /// <param name="account">the account</param>
        /// <returns>a collection of character objects of the characters on the given account</returns>
        public static CharacterRecord[] FindAllOfAccount(int accId)
        {
            CharacterRecord[] chrs;
            try
            {
                chrs = FindAllByProperty("Created", "AccountId", accId);
                //var chrs = FindAllByProperty("Created", "AccountId", account.AccountId);
                //chrs.Reverse();
                //return chrs;
            }
            catch (Exception ex)
            {
                RealmDBMgr.OnDBError(ex);
                chrs = FindAllByProperty("Created", "AccountId", accId);
            }
            //chrs.Reverse();
            return chrs;
        }

		public static CharacterRecord GetRecord(uint id)
		{
			var senderChr = World.GetCharacter(id);
			CharacterRecord sender;
			if (senderChr == null)
			{
				sender = LoadRecordByEntityId(id);
			}
			else
			{
				sender = senderChr.Record;
			}
			return sender;
		}

		public static uint GetIdByName(string name)
		{
			var sql = string.Format("SELECT {0} FROM {1} WHERE {2} = {3} LIMIT 1",
				DatabaseUtil.Dialect.QuoteForColumnName("EntityLowId"),
				DatabaseUtil.Dialect.QuoteForTableName(typeof(CharacterRecord).Name),
				DatabaseUtil.Dialect.QuoteForColumnName("Name"),
				DatabaseUtil.ToSqlValueString(name));
			var query = new ScalarQuery<long>(typeof(CharacterRecord), QueryLanguage.Sql, sql);
			return (uint)query.Execute();
		}

		public static uint GetGuildId(uint charId)
		{
			var sql = string.Format("SELECT {0} FROM {1} WHERE {2} = {3} LIMIT 1",
				DatabaseUtil.Dialect.QuoteForColumnName("GuildId"),
				DatabaseUtil.Dialect.QuoteForTableName(typeof(CharacterRecord).Name),
				DatabaseUtil.Dialect.QuoteForColumnName("Guid"),
				charId);
			var query = new ScalarQuery<int>(typeof(CharacterRecord), QueryLanguage.Sql, sql);
			return (uint)query.Execute();
		}
		#endregion

		#region Aliases
		//[Property]
		public byte[] RawAliases
		{
			get;
			set;
		}

        [Property (NotNull = true)]
	    public byte CharNum { get; set; }
        [Property(NotNull = true)]
	    public byte Zodiac { get; set; }

	    private ICollection<Asda2ItemRecord> _asda2LoadedItems;
        public ICollection<Asda2ItemRecord> Asda2LoadedItems
	    {
	        get { return _asda2LoadedItems; }
	    }
        private ICollection<Asda2FastItemSlotRecord> _asda2LoadedFastItemSlots;
	    

	    [Property]
	    public byte Asda2Class { get; set; }

	    public ICollection<Asda2FastItemSlotRecord> Asda2LoadedFastItemSlots
        {
            get { return _asda2LoadedFastItemSlots; }
        }
        [Property]
	    public byte EyesColor { get; set; }
        [Property]
	    public int BaseLuck { get; set; }
        [Property]
	    public int FreeStatPoints { get; set; }
        [Property]
        public int GlobalChatColorDb
        {
            get { return GlobalChatColor.ARGBValue; }
            set { GlobalChatColor = new Color {ARGBValue = value}; }
        }
        [Property (Length = 16)]
        public uint[] DiscoveredTitles { get; set; }
        [Property (Length = 16)]
        public uint[] GetedTitles { get; set; }
        [Property(Length = 18)]
        public uint[] LearnedRecipes { get; set; }
        public Color GlobalChatColor { get; set; }
         [Property]
	    public int FishingLevel { get; set; }
        [Property]
        public int AvatarMask { get; set; }
        [Property (Length = 19)]
	    public byte[] SettingsFlags { get; set; }
        [Property]
	    public short Asda2FactionId { get; set; }
        [Property]
	    public int GuildPoints { get; set; }
        [Property]
        public short PreTitleId { get; set; }
        [Property]
        public short PostTitleId { get; set; }
        [Property]
	    public byte MaxRepipesCount { get; set; }
         [Property]
	    public byte CraftingLevel { get; set; }
        /// <summary>
        /// ???? ?????? ? %
        /// </summary>
        [Property]
	    public float CraftingExp { get; set; }
        [Property]
	    public int BanPoints { get; set; }
        [Property]
	    public byte PremiumWarehouseBagsCount { get; set; }
        [Property]
        public byte PremiumAvatarWarehouseBagsCount { get; set; }
         [Property]
	    public string WarehousePassword { get; set; }
        [Property]
	    public int Asda2HonorPoints { get; set; }
        [Property]
	    public int RebornCount { get; set; }
        [Property]
	    public bool ChatBanned { get; set; }
		//[Property]
		//public string TitleProgress { get; set; }

		public void SetAliases(IEnumerable<Alias> aliases)
		{
			var bytes = new List<byte>(100);
			foreach (var alias in aliases)
			{
				// todo: Use client locale to identify correct encoding
				bytes.AddRange(Encoding.UTF8.GetBytes(alias.Key));
				bytes.Add(0);	// 0 is definitely neither in key, nor in value
				bytes.AddRange(Encoding.UTF8.GetBytes(alias.Value));
				bytes.Add(0);	// 0 is definitely neither in key, nor in value
			}
			RawAliases = bytes.ToArray();
		}

		public Dictionary<string, string> ParseAliases()
		{
			var map = new Dictionary<string, string>();
			if (RawAliases != null)
			{
				var isKey = true;
				var keyIndex = 0;
				var valueIndex = -1;
				for (var i = 0; i < RawAliases.Length; i++)
				{
					var b = RawAliases[i];
					if (b == 0)
					{
						// found new key or value
						isKey = !isKey;
						if (isKey)
						{
							// new alias
							if (valueIndex >= 0)
							{
								var key = Encoding.UTF8.GetString(RawAliases, keyIndex, valueIndex - keyIndex);
								var value = Encoding.UTF8.GetString(RawAliases, valueIndex, i - valueIndex);
								map[key] = value;
							}
							keyIndex = i;
							valueIndex = -1;
						}
						else
						{
							// read key already, now read value
							valueIndex = i;
						}
					}
				}
			}
			return map;
		}
		#endregion

		public override string ToString()
		{
			return string.Format("{0} (Id: {1}, Account: {2})", Name, EntityLowId, AccountId);
		}
	}

}