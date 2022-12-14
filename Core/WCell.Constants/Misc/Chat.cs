namespace WCell.Constants.Misc
{
	/// <summary>
	/// Message Types
	/// </summary>
	public enum ChatMsgType : int
	{
		Addon = -1,
		System = 0,
		Say = 1,
		Party = 2,
		Raid = 3,
		Guild = 4,
		Officer = 5,
		Yell = 6,
		Whisper = 7,

		WhisperInform = 8,

		MsgReply = 0x09,
		Emote = 10,
		//TextEmote = 0x0B,
		MonsterSay = 12,
		MonsterParty = 13,
		MonsterYell = 14,
		MonsterWhisper = 15,
		MonsterEmote = 16,

		Channel = 17,
		//ChannelJoin = 0x12,
		//ChannelLeave = 0x13,
		//ChannelList = 0x14,
		//ChannelNotice = 0x15,
		//ChannelNoticeUser = 0x16,
		AFK = 23,
		DND = 24,
		Ignored = 0x19,
		//Skill = 0x1A,
		//Loot = 0x1B,
		//Money = 0x1C,
		//Opening = 0x1D,
		//Tradeskills = 0x1E,
		//PetInfo = 0x1F,
		//CombatMiscInfo = 0x20,
		CombatXPGain = 0x21,
		//CombatHonorGain = 0x22,
		//CombatFactionChange = 0x23,

		BGSystemNeutral = 36,
		BGSystemAlliance = 37,
		BGSystemHorde = 38,

		RaidLeader = 39,
		RaidWarn = 40,

		RaidBossEmote = 41,
		RaidBossWhisper = 42,

		Filtered = 43,
		Battleground = 44,
		BattlegroundLeader = 45,
		Restricted = 46,

		Battlenet = 47,
		Achievment = 48,
		ArenaPoints = 49,
		PartyLeader,
		End
	}
    [System.Serializable]
	/// <summary>
	/// Chat Language Ids
	/// <remarks>Values from the first column of Languages.dbc</remarks>
	/// </summary>
	public enum ChatLanguage : uint
	{
		Universal2 = uint.MaxValue,
		Universal = 0,
		Orcish = 1,
		Darnassian = 2,
		Taurahe = 3,
		Dwarven = 6,
		Common = 7,
		DemonTongue = 8,
		Titan = 9,
		Thalassian = 10,
		Draconic = 11,
		OldTongue = 12,
		Gnomish = 13,
		Troll = 14,
		Gutterspeak = 33,
		Draenei = 35,
		End
	}

	/// <summary>
	/// A list of all HTML color-names. Use: <see href="ChatUtil.Colors[(int)ChatColor]"/>.
	/// See http://www.w3schools.com/html/html_colornames.asp
	/// </summary>
	public enum ChatColor
	{
		AliceBlue,
		AntiqueWhite,
		Aqua,
		Aquamarine,
		Azure,
		Beige,
		Bisque,
		Black,
		BlanchedAlmond,
		Blue,
		BlueViolet,
		Brown,
		BurlyWood,
		CadetBlue,
		Chartreuse,
		Chocolate,
		Coral,
		CornflowerBlue,
		Cornsilk,
		Crimson,
		Cyan,
		DarkBlue,
		DarkCyan,
		DarkGoldenRod,
		DarkGray,
		DarkGrey,
		DarkGreen,
		DarkKhaki,
		DarkMagenta,
		DarkOliveGreen,
		Darkorange,
		DarkOrchid,
		DarkRed,
		DarkSalmon,
		DarkSeaGreen,
		DarkSlateBlue,
		DarkSlateGray,
		DarkSlateGrey,
		DarkTurquoise,
		DarkViolet,
		DeepPink,
		DeepSkyBlue,
		DimGray,
		DimGrey,
		DodgerBlue,
		FireBrick,
		FloralWhite,
		ForestGreen,
		Fuchsia,
		Gainsboro,
		GhostWhite,
		Gold,
		GoldenRod,
		Gray,
		Grey,
		Green,
		GreenYellow,
		HoneyDew,
		HotPink,
		IndianRed,
		Indigo,
		Ivory,
		Khaki,
		Lavender,
		LavenderBlush,
		LawnGreen,
		LemonChiffon,
		LightBlue,
		LightCoral,
		LightCyan,
		LightGoldenRodYellow,
		LightGray,
		LightGrey,
		LightGreen,
		LightPink,
		LightSalmon,
		LightSeaGreen,
		LightSkyBlue,
		LightSlateGray,
		LightSlateGrey,
		LightSteelBlue,
		LightYellow,
		Lime,
		LimeGreen,
		Linen,
		Magenta,
		Maroon,
		MediumAquaMarine,
		MediumBlue,
		MediumOrchid,
		MediumPurple,
		MediumSeaGreen,
		MediumSlateBlue,
		MediumSpringGreen,
		MediumTurquoise,
		MediumVioletRed,
		MidnightBlue,
		MintCream,
		MistyRose,
		Moccasin,
		NavajoWhite,
		Navy,
		OldLace,
		Olive,
		OliveDrab,
		Orange,
		OrangeRed,
		Orchid,
		PaleGoldenRod,
		PaleGreen,
		PaleTurquoise,
		PaleVioletRed,
		PapayaWhip,
		PeachPuff,
		Peru,
		Pink,
		Plum,
		PowderBlue,
		Purple,
		Red,
		RosyBrown,
		RoyalBlue,
		SaddleBrown,
		Salmon,
		SandyBrown,
		SeaGreen,
		SeaShell,
		Sienna,
		Silver,
		SkyBlue,
		SlateBlue,
		SlateGray,
		SlateGrey,
		Snow,
		SpringGreen,
		SteelBlue,
		Tan,
		Teal,
		Thistle,
		Tomato,
		Turquoise,
		Violet,
		Wheat,
		White,
		WhiteSmoke,
		Yellow,
		YellowGreen
	}
    [System.Serializable]
	/// <summary>
	/// Shows a Character performing an action
	/// </summary>
	public enum EmoteType
	{
		None = 0,
		SimpleTalk = 1,
		SimpleBow = 2,
		SimpleWave = 3,
		SimpleCheer = 4,
		SimpleExclamation = 5,
		SimpleQuestion = 6,
		SimpleEat = 7,
		StateDance = 10,
		SimpleLaugh = 11,
		StateSleep = 12,
		StateSit = 13,
		SimpleRude = 14,
		SimpleRoar = 15,
		SimpleKneel = 16,
		SimpleKiss = 17,
		SimpleCry = 18,
		SimpleChicken = 19,
		SimpleBeg = 20,
		SimpleApplaud = 21,
		SimpleShout = 22,
		SimpleFlex = 23,
		SimpleShy = 24,
		SimplePoint = 25,
		StateStand = 26,
		StateReadyunarmed = 27,
		StateWork = 28,
		StatePoint = 29,
		StateNone = 30,
		SimpleWound = 33,
		SimpleWoundcritical = 34,
		SimpleAttackunarmed = 35,
		SimpleAttack1h = 36,
		SimpleAttack2htight = 37,
		SimpleAttack2hloose = 38,
		SimpleParryunarmed = 39,
		SimpleParryshield = 43,
		SimpleReadyunarmed = 44,
		SimpleReady1h = 45,
		SimpleReadybow = 48,
		SimpleSpellprecast = 50,
		SimpleSpellcast = 51,
		SimpleBattleroar = 53,
		SimpleSpecialattack1h = 54,
		SimpleKick = 60,
		SimpleAttackthrown = 61,
		StateStun = 64,
		StateDead = 65,
		SimpleSalute = 66,
		StateKneel = 68,
		StateUsestanding = 69,
		SimpleWaveNosheathe = 70,
		SimpleCheerNosheathe = 71,
		SimpleEatNosheathe = 92,
		StateStunNosheathe = 93,
		SimpleDance = 94,
		SimpleSaluteNosheath = 113,
		StateUsestandingNosheathe = 133,
		SimpleLaughNosheathe = 153,
		StateWorkNosheathe = 173,
		StateSpellprecast = 193,
		SimpleReadyrifle = 213,
		StateReadyrifle = 214,
		StateWorkNosheatheMining = 233,
		StateWorkNosheatheChopwood = 234,
		Old_SimpleLiftoff = 253,
		SimpleLiftoff = 254,
		SimpleYes = 273,
		SimpleNo = 274,
		SimpleTrain = 275,
		SimpleLand = 293,
		StateAtEase = 313,
		StateReady1h = 333,
		StateSpellkneelstart = 353,
		StateSubmerged = 373,
		SimpleSubmerge = 374,
		StateReady2h = 375,
		StateReadybow = 376,
		SimpleMountspecial = 377,
		StateTalk = 378,
		StateFishing = 379,
		SimpleFishing = 380,
		SimpleLoot = 381,
		StateWhirlwind = 382,
		StateDrowned = 383,
		StateHoldBow = 384,
		StateHoldRifle = 385,
		StateHoldThrown = 386,
		SimpleDrown = 387,
		SimpleStomp = 388,
		SimpleAttackoff = 389,
		SimpleAttackoffpierce = 390,
		StateRoar = 391,
		StateLaugh = 392,
		SimpleCreatureSpecial = 393,
		SimpleJumpandrun = 394,
		SimpleJumpend = 395,
		SimpleTalkNosheathe = 396,
		SimplePointNosheathe = 397,
		StateCannibalize = 398,
		SimpleJumpstart = 399,
		StateDancespecial = 400,
		SimpleDancespecial = 401,
		SimpleCustomspell01 = 402,
		SimpleCustomspell02 = 403,
		SimpleCustomspell03 = 404,
		SimpleCustomspell04 = 405,
		SimpleCustomspell05 = 406,
		SimpleCustomspell06 = 407,
		SimpleCustomspell07 = 408,
		SimpleCustomspell08 = 409,
		SimpleCustomspell09 = 410,
		SimpleCustomspell10 = 411,
		StateExclaim = 412,
        PullingString = 416
	}
}