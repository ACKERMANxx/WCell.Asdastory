using System;
using System.Collections.Generic;
using System.Text;
using Castle.ActiveRecord;
using WCell.Core.Database;
using WCell.Core.Initialization;
using WCell.Core.Timers;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Guilds;
using WCell.Util.Variables;

namespace WCell.RealmServer.Asda2BattleGround
{
    public static class Asda2BattlegroundMgr
    {
        public static int MinimumPlayersToStartWar = 0;
        [NotVariable]
        public static int[] TotalWars = new int[4];
        [NotVariable]
        public static int[] LightWins = new int[4];
        [NotVariable]
        public static int[] DarkWins = new int[4];
        [NotVariable]
        public static int[] ChaosWins = new int[4];
        [NotVariable]
        public static Dictionary<Asda2BattlegroundTown,List<Asda2Battleground>> AllBattleGrounds = new Dictionary<Asda2BattlegroundTown, List<Asda2Battleground>>();

        public static byte OccupationDurationMins = 30;
        public static byte DeathMatchDurationMins = 30;
        [Initialization(InitializationPass.Tenth,"Asda2 battleground system.")]
        public static void InitBattlegrounds()
        {
            AllBattleGrounds.Add(Asda2BattlegroundTown.Alpia, new List<Asda2Battleground>());
            AllBattleGrounds.Add(Asda2BattlegroundTown.Silaris, new List<Asda2Battleground>());
            AllBattleGrounds.Add(Asda2BattlegroundTown.Flamio, new List<Asda2Battleground>());
            AllBattleGrounds.Add(Asda2BattlegroundTown.Aquaton, new List<Asda2Battleground>());
            AddBattleGround(Asda2BattlegroundTown.Alpia);
            AddBattleGround(Asda2BattlegroundTown.Silaris);
            AddBattleGround(Asda2BattlegroundTown.Flamio);
            AddBattleGround(Asda2BattlegroundTown.Aquaton);
            var btgrndreslts = BattlegroundResultRecord.FindAll();
            foreach (var battlegroundResultRecord in btgrndreslts)
            {
                ProcessBattlegroundResultRecord(battlegroundResultRecord);
            }
        }

        public static void ProcessBattlegroundResultRecord(BattlegroundResultRecord battlegroundResultRecord)
        {
            var town = (int) battlegroundResultRecord.Town;
            TotalWars[town]++;
            if (battlegroundResultRecord.IsLightWins != null)
            {
                if ((bool)battlegroundResultRecord.IsLightWins)
                    LightWins[town]++;
                else if ((bool)battlegroundResultRecord.IsDarkWins)
                    DarkWins[town]++;
                else if ((bool)battlegroundResultRecord.IsChaosWins)
                    ChaosWins[town]++;
            }
        }

        public static void AddBattleGround(Asda2BattlegroundTown town)
        {
            var newBtgrnd = new Asda2Battleground {Town = town};
            switch (town)
            {
                case Asda2BattlegroundTown.Alpia:
                    newBtgrnd.MinEntryLevel = 1;
                    newBtgrnd.MaxEntryLevel = 39;
                    break;
                case Asda2BattlegroundTown.Silaris:
                    newBtgrnd.MinEntryLevel = 40;
                    newBtgrnd.MaxEntryLevel = 59;
                    break;
                case Asda2BattlegroundTown.Flamio:
                    newBtgrnd.MinEntryLevel = 60;
                    newBtgrnd.MaxEntryLevel = 79;
                    break;
                case Asda2BattlegroundTown.Aquaton:
                    newBtgrnd.MinEntryLevel = 80;
                    newBtgrnd.MaxEntryLevel = 250;
                    break;
            }
            Asda2BattlegroundType warType;
            newBtgrnd.StartTime = GetNextWarTime(town, out warType,DateTime.Now);
            newBtgrnd.WarType = warType;
            newBtgrnd.EndTime =
                newBtgrnd.StartTime.AddMinutes(warType == Asda2BattlegroundType.Occupation
                                                   ? OccupationDurationMins
                                                   : DeathMatchDurationMins);

            
            
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 0, X = 265, Y = 152, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 1, X = 157, Y = 212, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 2, X = 389, Y = 217, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 3, X = 215, Y = 243, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 4, X = 321, Y = 243, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 5, X = 264, Y = 271, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 6, X = 153, Y = 335, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 7, X = 267, Y = 343, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 8, X = 383, Y = 344, BattleGround = newBtgrnd});
            newBtgrnd.Points.Add(new Asda2WarPoint() {Id = 9, X = 276, Y = 410, BattleGround = newBtgrnd});

            foreach (var asda2WarPoint in newBtgrnd.Points)
            {
                asda2WarPoint.OwnedFaction = -1;
                World.TaskQueue.RegisterUpdatableLater(asda2WarPoint);
            }
            AllBattleGrounds[town].Add(newBtgrnd);
            World.TaskQueue.RegisterUpdatableLater(newBtgrnd);
        }

        public static byte BeetweenWarsMinutes = 40;
        public static byte WeekendDeathMatchHour = 10;
        public static byte EveryDayDeathMatchHour = 13;
        public static byte WeekendOcupationHour = 16;
        public static byte EveryDayOcupationHour = 19;
        public static DateTime GetNextWarTime(Asda2BattlegroundTown town, out Asda2BattlegroundType type, DateTime now)
        {
            type = Asda2BattlegroundType.Deathmatch;
            DateTime dt;
            switch (now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Tuesday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Wednesday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Thursday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Friday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Saturday:
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
                case DayOfWeek.Sunday:
                    dt = new DateTime(now.Year, now.Month, now.Day, WeekendDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayDeathMatchHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if (now < dt)
                        return dt;
                    dt = new DateTime(now.Year, now.Month, now.Day, WeekendOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = new DateTime(now.Year, now.Month, now.Day, EveryDayOcupationHour, 0, 0).AddMinutes((byte)town * BeetweenWarsMinutes);
                    if(now<dt)
                    {
                        type = Asda2BattlegroundType.Occupation;
                        return dt;
                    }
                    dt = dt.AddDays(1).Subtract(new TimeSpan(dt.Hour,dt.Minute,dt.Millisecond)).AddMinutes(1);
                    return GetNextWarTime(town,out type,dt);
            }
            return DateTime.MaxValue;
        }

        public static void OnCharacterLogout(Character character)
        {
            if(character.CurrentBattleGround!=null)
            {
                character.CurrentBattleGround.Leave(character);
            }
        }
    }
    [ActiveRecord("BattlegroundResultRecord", Access = PropertyAccess.Property)]
    public class BattlegroundResultRecord : WCellRecord<BattlegroundResultRecord>
    {
        [Property]
        public Asda2BattlegroundTown Town { get; set; }
        [Property]
        public string MvpCharacterName { get; set; }
        [Property]
        public uint MvpCharacterGuid { get; set; }
        [Property]
        public int LightScores { get; set; }
        [Property]
        public int DarkScores { get; set; }
        [Property]
        public int ChaosScores { get; set; }
        private static readonly NHIdGenerator _idGenerator = new NHIdGenerator(typeof(BattlegroundResultRecord), "Guid");

        [PrimaryKey(PrimaryKeyType.Assigned, "Guid")]
        public long Guid
        {
            get;
            set;
        }

        public bool? IsLightWins
        {
            get { if (LightScores == DarkScores || LightScores == ChaosScores) return null;
                return LightScores > DarkScores && LightScores > ChaosScores;
            }
        }
        public bool? IsDarkWins
        {
            get
            {
                if (DarkScores == LightScores || DarkScores == ChaosScores) return null;
                return DarkScores > LightScores && DarkScores > ChaosScores;
            }
        }
        public bool? IsChaosWins
        {
            get
            {
                if (ChaosScores == LightScores || ChaosScores == DarkScores) return null;
                return ChaosScores > LightScores && LightScores > ChaosScores;
            }
        }

        public BattlegroundResultRecord() { }
        public BattlegroundResultRecord(Asda2BattlegroundTown town, string mvpCharacterName, uint mvpCharacterGuid,int lightScores,int darkScores, int chaosScores)
        {
            Town = town;
            MvpCharacterName = mvpCharacterName;
            MvpCharacterGuid = mvpCharacterGuid;
            LightScores = lightScores;
            DarkScores = darkScores;
            ChaosScores = chaosScores;
            Guid = _idGenerator.Next();
        }
    }
    [ActiveRecord("BattlegroundCharacterResultRecord", Access = PropertyAccess.Property)]
    public class BattlegroundCharacterResultRecord : WCellRecord<BattlegroundCharacterResultRecord>
    {
        [Property]
        public long WarGuid { get; set; }
        [Property]
        public string CharacterName { get; set; }
        [Property]
        public uint CharacterGuid { get; set; }
        [Property]
        public int ActScores { get; set; }
        [Property]
        public int Kills { get; set; }
        [Property]
        public int Deathes { get; set; }
        private static readonly NHIdGenerator _idGenerator = new NHIdGenerator(typeof(BattlegroundCharacterResultRecord), "Guid");

        [PrimaryKey(PrimaryKeyType.Assigned, "Guid")]
        public long Guid
        {
            get;
            set;
        }

        public BattlegroundCharacterResultRecord() { }
        public BattlegroundCharacterResultRecord(long warGuid, string characterName, uint characterGuid, int actScores, int kills,int deathes)
        {
            WarGuid = warGuid;
            CharacterName = characterName;
            CharacterGuid = characterGuid;
            ActScores = actScores;
            Kills = kills;
            Deathes = deathes;
            Guid = _idGenerator.Next();
        }
    }

    public class Asda2WarPoint : IUpdatable
    {
        private bool _isCapturing;
        
        private int _tomeToCaprute=CharacterFormulas.DefaultCaptureTime;
        private int _timeToStartCapturing = CharacterFormulas.DefaultTimeToStartCapture;
        private int _timeToNextGainPoints = CharacterFormulas.DefaultTimeGainExpReward;
        public Character CapturingCharacter { get; set; }
        public short Id { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public short OwnedFaction { get; set; }
        public Asda2WarPointStatus Status { get; set; }
        public Asda2Battleground BattleGround { get; set; }
        //todo disable on move\take dmg\stun
        public void TryCapture(Character activeCharacter)
        {
            lock (this)
            {
                if (CapturingCharacter != null)
                {
                    activeCharacter.SendWarMsg(string.Format("Point {0} already capturing by {1}.", Id + 1, CapturingCharacter.Name));
                    Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(activeCharacter.Client, Id, OcupationPointStartedStatus.Fail);
                    return;
                }
                if(activeCharacter.Asda2Position.GetDistance(new Util.Graphics.Vector3(X,Y))>7)
                {
                    activeCharacter.SendWarMsg(string.Format("Distance to {0} is too big.", Id + 1));
                    Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(activeCharacter.Client, Id, OcupationPointStartedStatus.Fail);
                    return;
                }
                if(Status != Asda2WarPointStatus.NotOwned && OwnedFaction == activeCharacter.Asda2FactionId)
                {
                    Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(activeCharacter.Client, Id, OcupationPointStartedStatus.YouAreOcupaingTheSameSide);
                    return;
                }
                CapturingCharacter = activeCharacter;
                activeCharacter.CurrentCapturingPoint = this; 
                CapturingCharacter.IsMoving = false;
                CapturingCharacter.IsFighting = false;
                Handlers.Asda2MovmentHandler.SendEndMoveByFastInstantRegularMoveResponse(CapturingCharacter);
                _isCapturing = false;
                _timeToStartCapturing = CharacterFormulas.DefaultTimeToStartCapture;
                Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(activeCharacter.Client,Id,OcupationPointStartedStatus.Ok);
                
            }
        }

        public void StopCapture()
        {
            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround,BattleGroundInfoMessageType.FailedToTemporarilyOccuptyTheNumOccupationPoints, Id,null,CapturingCharacter.Asda2FactionId);
            Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(CapturingCharacter.Client, Id, OcupationPointStartedStatus.Fail);
            CapturingCharacter.CurrentCapturingPoint = null;
            CapturingCharacter = null;
            _isCapturing = false;
        }
        public void Update(int dt)
        {
            if(Status==Asda2WarPointStatus.Owned)
            {
                //gain scores each one minute to team
                _timeToNextGainPoints -= dt;
                if(_timeToNextGainPoints<=0)
                {
                    BattleGround.GainScores(OwnedFaction,CharacterFormulas.FactionWarPointsPerTicForCapturedPoints);
                    _timeToNextGainPoints += CharacterFormulas.DefaultTimeGainExpReward;
                }
            }
            if(_isCapturing)
            {
                _tomeToCaprute -= dt;
                if(_tomeToCaprute<=0)
                {
                    //point captured
                    Status=Asda2WarPointStatus.Owned;
                    Asda2BattlegroundHandler.SendUpdatePointInfoResponse(null, this);
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.SuccessToCompletelyOccuptyTheNumOccupationPoints, Id, null, OwnedFaction);
                    switch (OwnedFaction)
                    {
                        case 0:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(1));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(2));
                            break;
                        case 1:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(0));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(2));
                            break;
                        case 2:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(1));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(0));
                            break;
                        default:
                            break;
                    }
                    BattleGround.GainScores(OwnedFaction, CharacterFormulas.FactionWarPointsPerTicForCapturedPoints); 
                    _isCapturing = false;
                }
            }
            else
            {
                if(CapturingCharacter==null || !BattleGround.IsStarted)
                return;
                _timeToStartCapturing -= dt;
                if (_timeToStartCapturing <= 0)
                {
                    _tomeToCaprute = CharacterFormulas.DefaultCaptureTime;
                    _isCapturing = true;
                    OwnedFaction = CapturingCharacter.Asda2FactionId;
                    CapturingCharacter.GainActPoints(1);
                    BattleGround.GainScores(CapturingCharacter,1);
                    Status = Asda2WarPointStatus.Capturing;
                    Asda2BattlegroundHandler.SendUpdatePointInfoResponse(null, this);
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.SuccessToTemporarilyOccuptyTheNumOccupationPoints, Id, null, CapturingCharacter.Asda2FactionId);
                    switch (CapturingCharacter.Asda2FactionId)
                    {
                        case 0:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(1));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(2));
                            break;
                        case 1:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(0));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(2));
                            break;
                        case 2:
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(1));
                            Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?)(0));
                            break;
                        default:
                            break;
                    }

                    //Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(BattleGround, BattleGroundInfoMessageType.TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint, Id, null, (short?) (CapturingCharacter.Asda2FactionId == 1 ? CapturingCharacter.Asda2FactionId : CapturingCharacter.Asda2FactionId));
                    Asda2BattlegroundHandler.SendOccupyingPointStartedResponse(CapturingCharacter.Client, Id, OcupationPointStartedStatus.Fail);
                    CapturingCharacter.CurrentCapturingPoint = null;

                    CapturingCharacter = null;
                    _timeToNextGainPoints = CharacterFormulas.DefaultTimeGainExpReward;
                }
            }

        }
    }
    public enum OcupationPointStartedStatus
    {
        Fail=0,
        Ok=1,
        YouAreOcupaingTheSameSide =3,
    }
    public enum Asda2WarPointStatus : short
    {
        NotOwned =0,
        Capturing =1,
        Owned =2,
    }
    public enum Asda2BattlegroundType
    {
        Occupation =0,
        Deathmatch =1,
    }
    public enum Asda2BattlegroundWarCanceledReason
    {
        CurrentWaitingListHasBeenDeleted =1,
        BattleFieldHasBeenClosed =2,
        WarCanceledDueLowPlayers =3,
    }
    public enum Asda2BattlegroundTown
    {
        Alpia =0,
        Silaris =1,
        Flamio =2,
        Aquaton=3
    }
    public enum BattleGroundInfoMessageType
    {
        FailedToTemporarilyOccuptyTheNumOccupationPoints = 0,
        SuccessToTemporarilyOccuptyTheNumOccupationPoints = 1,
        CanceledToTemporarilyOccuptyTheNumOccupationPoints = 2,
        FailedToCompletelyOccuptyTheNumOccupationPoints = 3,
        SuccessToCompletelyOccuptyTheNumOccupationPoints = 4,
        CanceledToCompletelyOccuptyTheNumOccupationPoints = 5,
        TheOtherSideHasTemporarilyOccupiedTheNumOccupationPoint =6,
        WarStartsInNumMins =7,
        WarStarted =8,
        WarEndsInNumMins = 9,
        PreWarCircle = 10,
        DarkWillReciveBuffs =11,
        DarkBuffsHasBeedRemoved =12,

    }
    public enum RegisterToBattlegroundStatus
    {
        Fail =0,
        Ok=1,
        YouRegisterAsFactionWarCandidat = 2,
        YouMustCHangeYourJobTwiceToEnterWar =3,
        BattleGroupInfoIsInvalid =4,
        YouHaveAlreadyRegistered = 5,
        YouCanJoinTheFActionWarOnlyOncePerDay =6,
        GamesInfoStrange =8,
        YouCantEnterCauseYouHaveBeenDissmised =9,
        WrongLevel =10,
        WarHasBeenCanceledCauseLowPlayers =11,
    }
}
