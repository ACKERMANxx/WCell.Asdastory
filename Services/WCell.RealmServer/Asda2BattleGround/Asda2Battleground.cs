using System;
using System.Collections.Generic;
using System.Threading;
using WCell.Constants.World;
using WCell.Core.Network;
using WCell.Core.Timers;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Formulas;
using WCell.RealmServer.Global;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Items;
using WCell.RealmServer.Logs;
using WCell.Util.Graphics;

namespace WCell.RealmServer.Asda2BattleGround
{
    public class Asda2Battleground : IUpdatable
    {
        public List<Asda2WarPoint> Points = new List<Asda2WarPoint>(7);

        public bool IsStarted { get; set; }
        public byte CurrentWarDurationMins
        {
            get
            {
                return WarType == Asda2BattlegroundType.Occupation
                           ? Asda2BattlegroundMgr.OccupationDurationMins
                           : Asda2BattlegroundMgr.DeathMatchDurationMins;
            }
        }
        public short LightWins { get { return (short)Asda2BattlegroundMgr.LightWins[(int)Town]; } }
        public short LightLooses { get { return (short)Asda2BattlegroundMgr.DarkWins[(int)Town]; } }
        public short DarkWins { get { return (short)Asda2BattlegroundMgr.DarkWins[(int)Town]; } }
        public short DarkLooses { get { return (short)Asda2BattlegroundMgr.LightWins[(int)Town]; } }
        public short ChaosWins { get { return (short)Asda2BattlegroundMgr.DarkWins[(int)Town]; } }
        public short ChaosLooses { get { return (short)Asda2BattlegroundMgr.LightWins[(int)Town]; } }
        public Asda2BattlegroundTown Town { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int LightScores { get; set; }
        public int DarkScores { get; set; }
        public int ChaosScores { get; set; }
        public byte MinEntryLevel { get; set; }
        public byte MaxEntryLevel { get; set; }
        public WorldLocation LightStartLocation = new WorldLocation(MapId.BatleField, new Vector3(19263, 19100));
        public WorldLocation DarkStartLocation = new WorldLocation(MapId.BatleField, new Vector3(19106, 19361));
        public WorldLocation ChaosStartLocation = new WorldLocation(MapId.BatleField, new Vector3(19422, 19361));
        public Character MvpCharacter { get; set; }
        public byte WarNotofocationStep { get; set; }
        public byte AmountOfBattleGroundsInList
        {
            get { return (byte)Asda2BattlegroundMgr.AllBattleGrounds[Town].Count; }
        }

        public Asda2BattlegroundType WarType { get; set; }

        public Dictionary<byte, Character> LightTeam = new Dictionary<byte, Character>();
        public Dictionary<byte, Character> DarkTeam = new Dictionary<byte, Character>();
        public Dictionary<byte, Character> ChaosTeam = new Dictionary<byte, Character>();
        public List<byte> FreeLightIds = new List<byte>();
        public List<byte> FreeDarkIds = new List<byte>();
        public List<byte> FreeChaosIds = new List<byte>();
        public bool IsRunning { get; set; }

        public List<string> DissmisedCharacterNames = new List<string>();

        public bool IsLeader { get; set; }

        public Asda2Battleground()
        {
            for (byte i = 0; i < 255; i++)
            {
                FreeDarkIds.Add(i);
                FreeLightIds.Add(i);
                FreeChaosIds.Add(i);
            }
        }

        public readonly object JoinLock = new object();
        public bool Join(Character chr)
        {
            lock (JoinLock)
            {
                chr.BattlegroundActPoints = 0;
                chr.BattlegroundKills = 0;
                chr.BattlegroundDeathes = 0;
                if (chr.Asda2FactionId == 0)//light
                {
                    if (FreeLightIds.Count == 0)
                        return false;
                    var id = FreeLightIds[0];
                    LightTeam.Add(id, chr);
                    FreeLightIds.RemoveAt(0);
                    chr.CurrentBattleGround = this;
                    chr.CurrentBattleGroundId = id;
                    chr.LocatonBeforeOnEnterWar = new WorldLocation(chr.Map, chr.Position);
                    if (IsRunning)
                    {
                        //TeleportToWar(chr);
                        Asda2BattlegroundHandler.SendYouCanEnterWarResponse(chr.Client);
                        //Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(chr.Client);
                    }
                    //chr.Map.CallDelayed(1,()=> Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this));
                    return true;
                }
                if (chr.Asda2FactionId == 1)//Dark
                {
                    if (FreeDarkIds.Count == 0)
                        return false;
                    var id = FreeDarkIds[0];
                    DarkTeam.Add(id, chr);
                    FreeDarkIds.RemoveAt(0);
                    chr.CurrentBattleGround = this;
                    chr.CurrentBattleGroundId = id;
                    chr.LocatonBeforeOnEnterWar = new WorldLocation(chr.Map, chr.Position);
                    if (IsRunning)
                    {
                        //TeleportToWar(chr);
                        Asda2BattlegroundHandler.SendYouCanEnterWarResponse(chr.Client);
                        //Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(chr.Client);
                    }
                    //chr.Map.CallDelayed(1, () => Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this));
                    return true;
                }
                if (chr.Asda2FactionId == 2)//Chaos
                {
                    if (FreeChaosIds.Count == 0)
                        return false;
                    var id = FreeChaosIds[0];
                    ChaosTeam.Add(id, chr);
                    FreeChaosIds.RemoveAt(0);
                    chr.CurrentBattleGround = this;
                    chr.CurrentBattleGroundId = id;
                    chr.LocatonBeforeOnEnterWar = new WorldLocation(chr.Map, chr.Position);
                    if (IsRunning)
                    {
                        //TeleportToWar(chr);
                        Asda2BattlegroundHandler.SendYouCanEnterWarResponse(chr.Client);
                        //Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(chr.Client);
                    }
                    //chr.Map.CallDelayed(1,()=> Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this));
                    return true;
                }
                return false;
            }
        }
        public bool Leave(Character chr)
        {
            lock (JoinLock)
            {
                if (chr.Asda2FactionId == 0)//light
                {
                    if (!LightTeam.ContainsValue(chr))
                        return false;
                    LightTeam.Remove(chr.CurrentBattleGroundId);
                    FreeLightIds.Add(chr.CurrentBattleGroundId);
                    chr.CurrentBattleGround = null;

                    chr.Map.CallDelayed(1, () =>
                    {
                        Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this);
                        Asda2BattlegroundHandler.SendCharacterHasLeftWarResponse(this,
                                                                                 (int)
                                                                                 chr.AccId,
                                                                                 chr.
                                                                                     CurrentBattleGroundId,
                                                                                 chr.Name,
                                                                                 chr.
                                                                                     Asda2FactionId);
                    });
                    if (chr.MapId == MapId.BatleField)
                        chr.TeleportTo(chr.LocatonBeforeOnEnterWar);
                    if (chr.IsStunned)
                        chr.Stunned--;
                    return true;
                }
                if (chr.Asda2FactionId == 1)//Dark
                {
                    if (!DarkTeam.ContainsValue(chr))
                        return false;
                    DarkTeam.Remove(chr.CurrentBattleGroundId);
                    FreeDarkIds.Add(chr.CurrentBattleGroundId);
                    chr.CurrentBattleGround = null;
                    chr.Map.CallDelayed(1, () =>
                                               {
                                                   Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this);
                                                   Asda2BattlegroundHandler.SendCharacterHasLeftWarResponse(this,
                                                                                                            (int)
                                                                                                            chr.AccId,
                                                                                                            chr.
                                                                                                                CurrentBattleGroundId,
                                                                                                            chr.Name,
                                                                                                            chr.
                                                                                                                Asda2FactionId);
                                               });
                    if (chr.MapId == MapId.BatleField)
                        chr.TeleportTo(chr.LocatonBeforeOnEnterWar);
                    if (chr.IsStunned)
                        chr.Stunned--;
                    return true;
                }
                if (chr.Asda2FactionId == 2)//Chaos
                {
                    if (!ChaosTeam.ContainsValue(chr))
                        return false;
                    ChaosTeam.Remove(chr.CurrentBattleGroundId);
                    FreeChaosIds.Add(chr.CurrentBattleGroundId);
                    chr.CurrentBattleGround = null;
                    chr.Map.CallDelayed(1, () =>
                    {
                        Asda2BattlegroundHandler.SendHowManyPeopleInWarTeamsResponse(this);
                        Asda2BattlegroundHandler.SendCharacterHasLeftWarResponse(this,
                                                                                 (int)
                                                                                 chr.AccId,
                                                                                 chr.
                                                                                     CurrentBattleGroundId,
                                                                                 chr.Name,
                                                                                 chr.
                                                                                     Asda2FactionId);
                    });
                    if (chr.MapId == MapId.BatleField)
                        chr.TeleportTo(chr.LocatonBeforeOnEnterWar);
                    if (chr.IsStunned)
                        chr.Stunned--;
                    return true;
                }
                return false;
            }
        }


        #region Implementation of IUpdatable
        private object _lock = new object();
        private DateTime lastbrc = DateTime.MinValue;
        private int _notificationsAboutStart = 3;
        public void Update(int dt)
        {
            lock (_lock)
            {
                if (DateTime.Now - lastbrc >= new TimeSpan(0, 15, 0))
                {
                    lastbrc = DateTime.Now;
                    Thread t = new Thread(new ThreadStart(() =>
                    {

                    }));
                    t.Start();
                }
            }
            //REMOVE TO START WAR
            /*switch (_notificationsAboutStart)
            {
                case 3:
                    if (DateTime.Now > StartTime.Subtract(new TimeSpan(0, 30, 0)))
                    {
                        _notificationsAboutStart--;

                        Asda2BattlegroundHandler.SendMessageServerAboutWarStartsResponse(30);
                    }
                    break;
                case 2:
                    if (DateTime.Now > StartTime.Subtract(new TimeSpan(0, 15, 0)))
                    {
                        _notificationsAboutStart--;
                        Asda2BattlegroundHandler.SendMessageServerAboutWarStartsResponse(15);
                    }
                    break;
                case 1:
                    if (DateTime.Now > StartTime.Subtract(new TimeSpan(0, 5, 0)))
                    {
                        _notificationsAboutStart--;
                        Asda2BattlegroundHandler.SendMessageServerAboutWarStartsResponse(5);
                    }
                    break;
                default:
                    break;
            }
            if (DateTime.Now > EndTime && IsRunning)
                Stop();
            else if (DateTime.Now > StartTime && DateTime.Now < EndTime)
                Start();*/
        }
        public int WiningFactionId { get { return Math.Max(LightScores, Math.Max(DarkScores, ChaosScores)); } }
        public long CurrentWarResultRecordGuid { get; set; }
        public void Stop()
        {
            if (!IsRunning)
                return;
            _notificationsAboutStart = 3;
            IsStarted = false;
            World.Broadcast(string.Format("War in {0} has ended. Light scores {1} vs {2} dark scores vs Chaos scores {3}.", Town, LightScores, DarkScores, ChaosScores));
            IsRunning = false;
            SetNextWarParametrs();
            //Notify players war ended.
            lock (JoinLock)
            {
                //find mvp
                foreach (var character in LightScores > DarkScores ? LightTeam.Values : DarkTeam.Values)
                {
                    if (MvpCharacter == null)
                    {
                        MvpCharacter = character;
                        continue;
                    }
                    if (MvpCharacter.BattlegroundActPoints < character.BattlegroundActPoints)
                        MvpCharacter = character;
                }
                Asda2BattlegroundHandler.SendWiningFactionInfoResponse(Town, WiningFactionId, MvpCharacter == null ? "[No character]" : MvpCharacter.Name);

                if (MvpCharacter != null)
                {
                    //create db records about war

                    RealmServer.IOQueue.AddMessage(() =>
                    {
                        var warResRec = new BattlegroundResultRecord(Town, MvpCharacter.Name, MvpCharacter.EntityId.Low,
                            LightScores, DarkScores, ChaosScores);
                        warResRec.CreateLater();
                        CurrentWarResultRecordGuid = warResRec.Guid;
                        Asda2BattlegroundMgr.ProcessBattlegroundResultRecord(warResRec);
                    });
                }
                foreach (var character in LightTeam.Values)
                {
                    ProcessEndWar(character);
                }
                foreach (var character in DarkTeam.Values)
                {
                    ProcessEndWar(character);
                }
                foreach (var character in ChaosTeam.Values)
                {
                    ProcessEndWar(character);
                }
                foreach (var asda2WarPoint in Points)
                {
                    asda2WarPoint.Status = Asda2WarPointStatus.NotOwned;
                    asda2WarPoint.OwnedFaction = -1;
                    Asda2BattlegroundHandler.SendUpdatePointInfoResponse(null, asda2WarPoint);
                }
                World.TaskQueue.CallDelayed(60000, KickAll);
            }
        }

        private void SetNextWarParametrs()
        {
            Asda2BattlegroundType type;
            StartTime = Asda2BattlegroundMgr.GetNextWarTime(Town, out type, DateTime.Now);
            WarType = type;
            EndTime =
                StartTime.AddMinutes(WarType == Asda2BattlegroundType.Occupation
                                         ? Asda2BattlegroundMgr.OccupationDurationMins
                                         : Asda2BattlegroundMgr.DeathMatchDurationMins);
        }

        private void ProcessEndWar(Character character)
        {
            character.Stunned++;
            GlobalHandler.SendFightingModeChangedResponse(character.Client, character.SessionId, (int)character.AccId, -1);
            //create db record
            if (MvpCharacter != null)
            {
RealmServer.IOQueue.AddMessage(() =>
{
    var rec = new BattlegroundCharacterResultRecord(CurrentWarResultRecordGuid, character.Name,
                                                                character.EntityId.Low, character.BattlegroundActPoints,
                                                                character.BattlegroundKills,
                                                                character.BattlegroundDeathes);
    rec.CreateLater();
});
                
            }
            var honorPoints = CharacterFormulas.CalcHonorPoints(character.Level, character.BattlegroundActPoints,
                                                                                      LightScores > DarkScores,
                                                                                      character.BattlegroundDeathes,
                                                                                      character.BattlegroundKills, MvpCharacter == character, Town);
            var honorCoins = (short)(honorPoints / CharacterFormulas.HonorCoinsDivider);
            if (character.BattlegroundActPoints < 5)
                character.BattlegroundActPoints = 5;
            if (honorPoints <= 0)
                honorPoints = 1;
            if (honorCoins <= 0)
                honorCoins = 1;
            Asda2Item itemCoins = null;
            if (honorCoins > 0)
            {
                character.Asda2Inventory.TryAdd(
                    20614, honorCoins,
                    true, ref itemCoins);
                Log.Create(Log.Types.ItemOperations, LogSourceType.Character, character.EntryId)
              .AddAttribute("source", 0, "honor_coins_for_bg")
              .AddItemAttributes(itemCoins)
              .AddAttribute("amount", honorCoins)
              .Write();
            }
            var bonusExp = WiningFactionId == 2 ? 0 : (int)((float)XpGenerator.GetBaseExpForLevel(character.Level) * character.BattlegroundActPoints / 2.5);
            character.GainXp(bonusExp, "battle_ground");

            character.Asda2HonorPoints += honorPoints;
            Asda2BattlegroundHandler.SendWarEndedResponse(character.Client, (byte)WiningFactionId,
                                                          LightScores,
                                                          DarkScores, ChaosScores, honorPoints,
                                                          honorCoins, bonusExp, MvpCharacter == null ? "" : MvpCharacter.Name);
            Asda2BattlegroundHandler.SendWarEndedOneResponse(character.Client, new List<Asda2Item> { itemCoins });
            character.SendWarMsg("You will automaticly teleported to town in 1 minute.");
        }
        public void KickAll()
        {
            lock (JoinLock)
            {
                var list = new List<Character>();
                list.AddRange(LightTeam.Values);
                list.AddRange(DarkTeam.Values);
                list.AddRange(ChaosTeam.Values);
                foreach (var character in list)
                {
                    Leave(character);
                }
            }
        }

        public void Start()
        {
            if (IsRunning)
                return;
            StartTime = DateTime.Now;
            EndTime =
                DateTime.Now.AddMinutes(WarType == Asda2BattlegroundType.Occupation
                                            ? Asda2BattlegroundMgr.EveryDayDeathMatchHour
                                            : Asda2BattlegroundMgr.DeathMatchDurationMins);
            if (LightTeam.Count < Asda2BattlegroundMgr.MinimumPlayersToStartWar || DarkTeam.Count < Asda2BattlegroundMgr.MinimumPlayersToStartWar || ChaosTeam.Count < Asda2BattlegroundMgr.MinimumPlayersToStartWar)
            {
                World.Broadcast(string.Format("War terminated due not enought players in {0}.", Town));
                SetNextWarParametrs();
                return;
            }
            World.Broadcast(string.Format("War started in {0}. Avalible lvls {1}-{2}.", Town, MinEntryLevel, MaxEntryLevel));
            foreach (var asda2WarPoint in Points)
            {
                asda2WarPoint.Status = Asda2WarPointStatus.NotOwned;
                asda2WarPoint.OwnedFaction = -1;
            }
            DissmisedCharacterNames.Clear();
            IsRunning = true;
            LightScores = 0;
            DarkScores = 0;
            ChaosScores = 0;
            MvpCharacter = null;
            WarNotofocationStep = 0;
            //Notify character that they can login
            lock (JoinLock)
            {
                foreach (var character in LightTeam.Values)
                {
                    //TeleportToWar(character);
                    Asda2BattlegroundHandler.SendYouCanEnterWarResponse(character.Client);
                    //Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(character.Client);
                }
                foreach (var character in DarkTeam.Values)
                {
                    //TeleportToWar(character);
                    Asda2BattlegroundHandler.SendYouCanEnterWarResponse(character.Client);
                    // Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(character.Client);
                }
                foreach (var character in ChaosTeam.Values)
                {
                    //TeleportToWar(character);
                    Asda2BattlegroundHandler.SendYouCanEnterWarResponse(character.Client);
                    // Asda2BattlegroundHandler.SendYouCanEnterWarAfterResponse(character.Client);
                }
            }
            World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
        }

        private void SendWarTimeMotofocation()
        {
            if (!IsRunning) return;
            switch (WarNotofocationStep)
            {
                case 0:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarStartsInNumMins, 1);
                    World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
                    break;
                case 1:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarStarted, 0);
                    World.TaskQueue.CallDelayed(23 * 60000, SendWarTimeMotofocation);
                    IsStarted = true;
                    break;
                case 2:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarEndsInNumMins, 5);
                    World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
                    break;
                case 3:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarEndsInNumMins, 4);
                    World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
                    break;
                case 4:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarEndsInNumMins, 3);
                    World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
                    break;
                case 5:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarEndsInNumMins, 2);
                    World.TaskQueue.CallDelayed(60000, SendWarTimeMotofocation);
                    break;
                case 6:
                    Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarEndsInNumMins, 1);
                    break;
            }
            WarNotofocationStep++;
        }

        #endregion

        public void Send(RealmPacketOut packet, bool addEnd = false, short? asda2FactionId = null, Locale locale = Locale.Any)
        {
            lock (JoinLock)
            {
                if (asda2FactionId == null)
                {
                    foreach (var chr in ChaosTeam.Values)
                    {
                        if (locale == Locale.Any || chr.Client.Locale == locale)
                            chr.Send(packet, addEnd: addEnd);
                    }
                    foreach (var chr in DarkTeam.Values)
                    {
                        if (locale == Locale.Any || chr.Client.Locale == locale)
                            chr.Send(packet, addEnd: addEnd);
                    }
                    foreach (var chr in LightTeam.Values)
                    {
                        if (locale == Locale.Any || chr.Client.Locale == locale)
                            chr.Send(packet, addEnd: addEnd);
                    }
                }
                else
                {
                    foreach (var chr in asda2FactionId == 0 ? LightTeam.Values : (asda2FactionId == 1 ? DarkTeam.Values : ChaosTeam.Values))
                    {
                        if (locale == Locale.Any || chr.Client.Locale == locale)
                            chr.Send(packet);
                    }
                }
            }
        }

        public void TeleportToWar(Character activeCharacter)
        {
            if (activeCharacter.Asda2FactionId == 0)
                activeCharacter.TeleportTo(LightStartLocation);
            else if (activeCharacter.Asda2FactionId == 1)
                activeCharacter.TeleportTo(DarkStartLocation);
            else if (activeCharacter.Asda2FactionId == 2)
                activeCharacter.TeleportTo(ChaosStartLocation);
        }

        public void GainScores(Character killer, short points)
        {
            GainScores(killer.Asda2FactionId, points);
        }
        public void GainScores(short factionId, short points)
        {
            if (factionId == 0)
                LightScores += points;
            else if (factionId == 1)
                DarkScores += points;
            else if (factionId == 2)
                ChaosScores += points;
            Asda2BattlegroundHandler.SendTeamPointsResponse(this);
        }

        public void SendCurrentProgress(Character character)
        {
            if (DateTime.Now < StartTime.AddMinutes(2))
                Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarStartsInNumMins, (short)(StartTime.AddMinutes(2) - DateTime.Now).TotalMinutes, character);
            else
                Asda2BattlegroundHandler.SendWarCurrentActionInfoResponse(this, BattleGroundInfoMessageType.WarStarted, 0, character);
        }

        public Vector3 GetBasePosition(Character activeCharacter)
        {
            return activeCharacter.Asda2FactionId == 0 ? LightStartLocation.Position : (activeCharacter.Asda2FactionId == 1 ? DarkStartLocation.Position : ChaosStartLocation.Position);
        }

        public Vector3 GetForeigLocation(Character activeCharacter)
        {
            return activeCharacter.Asda2FactionId == 0 ? DarkStartLocation.Position : (activeCharacter.Asda2FactionId == 2 ? LightStartLocation.Position : ChaosStartLocation.Position);
        }

        public short DissmisFaction { get; set; }
        public bool IsDismissInProgress;
        public Character DissmissingCharacter { get; set; }
        public DateTime DissmissTimeouted { get; set; }
        public List<Character> DissmissYes = new List<Character>();
        public List<Character> DissmissNo = new List<Character>();
        public void AnswerDismiss(bool kick, Character answerer)
        {
            lock (this)
            {
                if (!IsDismissInProgress || answerer.Asda2FactionId != DissmisFaction || answerer == DissmissingCharacter)
                    return;
                if (kick)
                {
                    if (DissmissYes.Contains(answerer))
                        return;
                    DissmissYes.Add(answerer);
                    if (DissmissYes.Count > (DissmisFaction == 0 ? LightTeam.Count * 0.65 : DarkTeam.Count * 0.65))
                    {
                        //kick him
                        Asda2BattlegroundHandler.SendDissmissResultResponse(this, DismissPlayerResult.Ok,
                                                                            DissmissingCharacter.SessionId,
                                                                            (int)DissmissingCharacter.AccId);
                        Leave(DissmissingCharacter);
                        IsDismissInProgress = false;
                        DissmissingCharacter = null;
                    }
                }
                else
                {
                    if (DissmissNo.Contains(answerer))
                        return;
                    DissmissNo.Add(answerer);
                    if (DissmissNo.Count > (DissmisFaction == 0 ? LightTeam.Count * 0.3 : DarkTeam.Count * 0.3))
                    {
                        //CANcel dissmis
                        Asda2BattlegroundHandler.SendDissmissResultResponse(this, DismissPlayerResult.Fail,
                                                                            DissmissingCharacter.SessionId,
                                                                            (int)DissmissingCharacter.AccId);
                        IsDismissInProgress = false;
                        DissmissingCharacter = null;
                    }
                }
            }
        }
        public bool TryStartDissmisProgress(Character initer, Character dissmiser)
        {
            lock (this)
            {
                if (IsDismissInProgress)
                {
                    if (DissmissTimeouted < DateTime.Now)
                    {
                        Asda2BattlegroundHandler.SendDissmissResultResponse(this, DismissPlayerResult.Fail, DissmissingCharacter.SessionId, (int)DissmissingCharacter.AccId);
                    }
                    else
                        return false;
                }
                IsDismissInProgress = true;
                Asda2BattlegroundHandler.SendQuestionDismissPlayerOrNotResponse(this, initer, dissmiser);
                DissmissingCharacter = dissmiser;
                DissmissYes.Clear();
                DissmissNo.Clear();
                DissmissTimeouted = DateTime.Now.AddMinutes(1);
                DissmisFaction = initer.Asda2FactionId;
                return true;
            }
        }

        public Character GetCharacter(short asda2FactionId, byte warId)
        {
            if (warId == 0)
            {
                return LightTeam.Count <= warId ? null : LightTeam[warId];
            }
            else if (warId == 1)
            {
                return DarkTeam.Count <= warId ? null : DarkTeam[warId];
            }
            return ChaosTeam.Count <= warId ? null : ChaosTeam[warId];
        }
    }
}