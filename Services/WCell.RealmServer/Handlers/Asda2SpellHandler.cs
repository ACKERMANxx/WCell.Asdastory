using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Constants;
using WCell.Constants.Achievements;
using WCell.Constants.Items;
using WCell.Constants.Spells;
using WCell.Core.Network;
using WCell.RealmServer.Achievements;
using WCell.RealmServer.Asda2_Items;
using WCell.RealmServer.Asda2BattleGround;
using WCell.RealmServer.Asda2Looting;
using WCell.RealmServer.Asda2Mail;
using WCell.RealmServer.Asda2Titles;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using WCell.RealmServer.Items;
using WCell.RealmServer.Misc;
using WCell.RealmServer.Network;
using WCell.RealmServer.Quests;
using WCell.RealmServer.Spells;
using WCell.RealmServer.Spells.Auras;

namespace WCell.RealmServer.Handlers
{
    internal class Asda2SpellHandler
    {
        private static readonly byte[] stab15 = new byte[47]
    {
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      byte.MaxValue,
      0,
      0,
      byte.MaxValue,
      byte.MaxValue,
      0,
      0,
      0,
      0,
      0,
      0,
      1,
      0,
      2,
      0,
      1,
      0,
      0,
      0,
      0
    };
        
        public static void SendShowSoulGuard(Character chr, int soulid)
        {
            using (var packet = new RealmPacketOut((RealmServerOpCode)6156))
            {
                packet.WriteInt32(chr.AccId);
                // packet.WriteInt16(0); // default : 0 Len : 2
                packet.WriteInt16(soulid);
                chr.SendPacketToArea(packet, addEnd: true);
            }

        }
        [PacketHandler(RealmServerOpCode.UseSkill)] //5256
        public static void UseSkillRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            client.ActiveCharacter.IsFighting = false;
            //Todo send spell init packet
            client.ActiveCharacter.IsMoving = false;
            var skillId = packet.ReadInt16();//default : 927Len : 2
            packet.Position += 1;//nk1 default : 1Len : 1
            var x = packet.ReadInt16();//default : 100Len : 2
            var y = packet.ReadInt16();//default : 362Len : 2
            var targetType = packet.ReadByte();//default : 1Len : 1
            var targetId = packet.ReadUInt16();//default : 18Len : 4
            Spell spell = client.ActiveCharacter.Spells.GetSpellByRealId(skillId);
            if (spell == null) return;
            if (spell.SoulGuardProffLevel !=0)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to use SoulguardSkill as normal skill.");return;
            }
            ProcessUseSkill(client, targetType, skillId, targetId);
        }
        [PacketHandler(RealmServerOpCode.CancelSkill)]//6142
        public static void CancelSkillRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 4;//nk default : unkLen : 5
            Console.WriteLine(packet.ToHexDump()+ "\n" + BitConverter.ToString(packet.ReadBytes(4)));
            
            packet.Position -= 4;//nk default : unkLen : 5
            var skillId = packet.ReadInt16();//default : 202Len : 2
            client.ActiveCharacter.Auras.RemoveFirstVisibleAura(a => a.Spell.RealId == skillId && a.IsBeneficial);
        }


        private static void ProcessUseSkill(IRealmClient client, byte targetType, short skillId, ushort targetId)
        {
            Unit target = null;
            if (targetType == 0)
                target = client.ActiveCharacter.Map.GetNpcByUniqMapId(targetId);
            else if (targetType == 1)
                target = World.GetCharacterBySessionId(targetId);
            else
            {
                client.ActiveCharacter.SendSystemMessage(
                    string.Format("Unknown skill target type {0}. SkillId {1}. Please report to developers.", targetType,
                                  skillId));
            }
            if (target == null)
            {
                client.ActiveCharacter.SendInfoMsg("Bad target.");
                return;
            }
            Spell spell = client.ActiveCharacter.Spells.GetSpellByRealId(skillId);
            if (spell != null)
            {
                SendSetAtackStateGuiResponse(client.ActiveCharacter);
                SpellCast cast = client.ActiveCharacter.SpellCast;
                var reason = cast.Start(spell, target);
                if(reason == SpellFailedReason.Ok)
                {
                    if(spell.LearnLevel<10)
                    {
                        if (client.ActiveCharacter.GreenCharges < 10)
                            client.ActiveCharacter.GreenCharges += 1;
                    }
                     else if(spell.LearnLevel<30)
                    {
                        if(client.ActiveCharacter.GreenCharges<10)
                            client.ActiveCharacter.GreenCharges += 1;
                    }
                    else if (spell.LearnLevel < 50)
                    {
                        if (client.ActiveCharacter.BlueCharges < 10)     
                            client.ActiveCharacter.BlueCharges += 1;
                        if (client.ActiveCharacter.GreenCharges < 10)
                            client.ActiveCharacter.GreenCharges += 1;
                        
                    }
                    else
                    {
                        if (client.ActiveCharacter.RedCharges < 10)                        
                            client.ActiveCharacter.RedCharges += 1;
                        if (client.ActiveCharacter.BlueCharges < 10)
                            client.ActiveCharacter.BlueCharges += 1;
                        if (client.ActiveCharacter.GreenCharges < 10)
                            client.ActiveCharacter.GreenCharges += 1;
                        
                    }
                    if (client.ActiveCharacter.GreenCharges >= 5)
                    {
                        client.ActiveCharacter.Auras.CreateAndStartAura(client.ActiveCharacter.SharedReference, SpellId.FirepowerSupportRank1, true, null);
                    }
                    SendSetSkiillPowersStatsResponse(client.ActiveCharacter, true, skillId);
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)42, 100, 200);
                }
                else if (reason == SpellFailedReason.OutOfRange)
                {
                    Asda2MovmentHandler.MoveToSelectedTargetAndAttack(client.ActiveCharacter);
                }
            }
        }
        public static void CheckSoulGuard(Character chr, int skillid)
        {
            if (chr.GreenCharges >= 8)
            {
                if (firstTree.Contains(skillid))
                {
                    if(chr.Profession == Asda2Profession.Warrior)
                        SendShowSoulGuard(chr, 0);
                    if (chr.Profession == Asda2Profession.Archer)
                        SendShowSoulGuard(chr, 9);
                    if (chr.Profession == Asda2Profession.Mage)
                        SendShowSoulGuard(chr, 18);
                }
                if (secondTree.Contains(skillid))
                {
                    if (chr.Profession == Asda2Profession.Warrior)
                        SendShowSoulGuard(chr, 3);
                    if (chr.Profession == Asda2Profession.Archer)
                        SendShowSoulGuard(chr, 12);
                    if (chr.Profession == Asda2Profession.Mage)
                        SendShowSoulGuard(chr, 21);
                }
                if (thirdTree.Contains(skillid))
                {
                    if (chr.Profession == Asda2Profession.Warrior)
                        SendShowSoulGuard(chr, 6);
                    if (chr.Profession == Asda2Profession.Archer)
                        SendShowSoulGuard(chr, 15);
                    if (chr.Profession == Asda2Profession.Mage)
                        SendShowSoulGuard(chr, 24);
                }
            }
            else
            {
                SendShowSoulGuard(chr, -1);
            }

        }

       
        public static int getProff(Character chr)
{
    switch (chr.Class)
    {
        case ClassId.OHS:
            return 0;
        case ClassId.Spear:
            return 3;
        case ClassId.THS:
            return 6;
        case ClassId.Crossbow:
            return 9;
        case ClassId.Bow:
            return 12;
        case ClassId.Balista:
            return 15;
        case ClassId.AtackMage:
            return 18;
        case ClassId.HealMage:
            return 21;
        case ClassId.SupportMage:
            return 24;
        default:
            return -1;
    }
}
[PacketHandler(RealmServerOpCode.UseSoulGuardSkill)]//6158
        public static void UseSoulGuardSkillRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            client.ActiveCharacter.IsFighting = false;
            client.ActiveCharacter.IsMoving = false;
            var skillId = packet.ReadInt16();//default : 927Len : 2
            packet.Position += 1;//nk1 default : 1Len : 1
            var x = packet.ReadInt16();//default : 100Len : 2
            var y = packet.ReadInt16();//default : 362Len : 2
            var targetType = packet.ReadByte();//default : 1Len : 1
            var targetId = packet.ReadUInt16();//default : 18Len : 4
            Spell spell = client.ActiveCharacter.Spells.GetSpellByRealId(skillId);
            if(spell==null)return;
            if(spell.SoulGuardProffLevel<1||spell.SoulGuardProffLevel>3)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to use skill as SoulguardSkill.");return;
            }
            switch (spell.SoulGuardProffLevel)
            {
                case 1:
                    if (client.ActiveCharacter.GreenCharges<5)
                    {
                        client.ActiveCharacter.SendInfoMsg("Not enougt charges.");
                        SendSetSkiillPowersStatsResponse(client.ActiveCharacter, false, 0);
                        return;
                    }
                    client.ActiveCharacter.GreenCharges -= 5;
                    break;
                case 2:
                    if (client.ActiveCharacter.BlueCharges < 5)
                    {
                        client.ActiveCharacter.SendInfoMsg("Not enougt charges.");
                        SendSetSkiillPowersStatsResponse(client.ActiveCharacter, false, 0);
                        return;
                    }
                    client.ActiveCharacter.BlueCharges -= 5;
                    break;
                case 3:
                    if (client.ActiveCharacter.RedCharges < 5)
                    {
                        client.ActiveCharacter.SendInfoMsg("Not enougt charges.");
                        SendSetSkiillPowersStatsResponse(client.ActiveCharacter, false, 0);
                        return;
                    }
                    client.ActiveCharacter.RedCharges -= 5;
                    break;
            }
            ProcessUseSkill(client, targetType, skillId, targetId);
            SendSetSkiillPowersStatsResponse(client.ActiveCharacter,false, 0);
        }
        
        [PacketHandler((RealmServerOpCode)6159)]
        public static void SGTest(IRealmClient client, RealmPacketIn packet)
        {
            if (client.ActiveCharacter.GreenCharges >= 8)
            {
                SendSGTestResponse(client);
            }
        }

        public static void SendSGTestResponse(IRealmClient client)
        {
            using (var packet = new RealmPacketOut((RealmServerOpCode)6160))
            {
                packet.WriteByte(1);
                packet.WriteByte((byte)client.ActiveCharacter.Archetype.ClassId);
                packet.WriteInt16(1);
                packet.WriteInt32(1);
                client.ActiveCharacter.SendPacketToArea(packet, true, false);
            }
        }

        public static void SendSetSkiillPowersStatsResponse(Character chr,bool animate,Int16 skillId)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.SetSkiillPowersStats))//6157
            {
                packet.WriteInt32(355335);//value name : unk4 default value : 355335Len : 4
                packet.WriteByte(animate ? 1 : 0);//{animate}default value : 0 Len : 1
                packet.WriteByte((byte) chr.Archetype.ClassId);//{casterClass}default value : 7 Len : 1
                packet.WriteInt16(skillId);//{skillId}default value : -1 Len : 2
                packet.WriteByte(chr.GreenCharges);//{green}default value : 1 Len : 1
                packet.WriteByte(chr.BlueCharges);//{blue}default value : 0 Len : 1
                packet.WriteByte(chr.RedCharges);//{red}default value : 0 Len : 1
                chr.Send(packet,addEnd: true);
            }
            CheckSoulGuard(chr, skillId);
        }


        /// <summary>
        /// Clears a single spell's cooldown
        /// </summary>
        public static void SendClearCoolDown(Character chr, SpellId spellId)
        {
            var spell = SpellHandler.Get(spellId);
            if (spell == null)
            {
                chr.SendSystemMessage(string.Format("Can't clear cooldown for {0} cause skill not exist.", spellId));
                return;
            }
            SendClearCoolDown(chr, spell.RealId);
            
        }
        public static void SendClearCoolDown(Character chr, short realId)
        {
            if(chr == null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.SkillReady))//5274
            {
                packet.WriteInt16(realId);//{skillId}default value : 586 Len : 2
                chr.Send(packet, addEnd: true);
            }

        }
        public static void SendSetSkillCooldownResponse(Character chr, Spell spell)
        {
            if(chr==null|| spell ==null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.SetSkillCooldown))//5271
            {
                packet.WriteByte(1);//{status}default value : 1 Len : 1
                packet.WriteInt16(chr.SessionId);//{sessId}default value : 42 Len : 2
                packet.WriteInt16(spell.RealId);//{skillId}default value : 586 Len : 2
                packet.WriteInt16(2);//value name : unk2 default value : 2Len : 2
                chr.Send(packet, addEnd: false);
            }
        }
        static readonly byte[] unk12 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public static void SendBuffEndedResponse(Character chr,short buffId)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.BuffEnded))//5273
            {
                packet.WriteInt16(chr.SessionId);//{sessId}default value : 45 Len : 2
                packet.WriteInt16(buffId);//{buffId}default value : 202 Len : 2
                chr.SendPacketToArea(packet);
            }
        }
        public static void SendUseSkillResultResponse(Character chr,Int16 skillId,Asda2UseSkillResult status)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.UseSkillResult))//5257
            {
                packet.WriteByte((byte)status);//{status}default value : 7 Len : 1
                packet.WriteInt16(chr.SessionId);//{casterSessId}default value : 6 Len : 2
                packet.WriteInt16(skillId);//{skillId}default value : 927 Len : 2
                packet.WriteByte(1);//value name : unk8 default value : 0Len : 1
                packet.WriteInt16(1);//value name : unk2 default value : -1Len : 2
                chr.Send(packet,addEnd: false);
            }
        }
        public static void SendMonstrUsedSkillResponse(NPC caster, short skillId, Unit initialTarget, DamageAction[] actions)
        {
            if(caster==null)return;
            //todo mass atack from NPC
            using (var packet = new RealmPacketOut(RealmServerOpCode.MonstrUsedSkill))//8012
            {
                var targetChr = initialTarget as Character;
                packet.WriteByte(0);//value name : unk5 default value : 0Len : 1
                packet.WriteInt16(skillId);//{skillId}default value : 71 Len : 2
                packet.WriteInt16(caster.UniqIdOnMap);//{mobId}default value : 243 Len : 2
                packet.WriteByte(0);//value name : unk8 default value : 0Len : 1
                packet.WriteByte(1);//value name : unk9 default value : 1Len : 1

                packet.WriteInt16(targetChr == null ? 0 : targetChr.SessionId);//{targetSessId}default value : 23 Len : 2
                var i = 0;
                if (actions != null)
                {
                    foreach (var damageAction in actions)
                    {
                        if (i > 16 || damageAction == null)
                            break;
                        targetChr = damageAction.Victim as Character;
                        packet.WriteByte(1); //value name : unk1 default value : 1Len : 1
                        packet.WriteInt16(targetChr == null ? 0 : targetChr.SessionId);
                            //{targetSessId0}default value : 23 Len : 2
                        var dmg = damageAction.ActualDamage;
                        if (dmg < 0 || dmg > 200000000)
                            dmg = 0;
                        packet.WriteInt32(actions.Length == 0 ? 0 : dmg);
                            //{damage}default value : 4218 Len : 4
                        packet.WriteByte(actions.Length == 0 ? 3 : 1); //{effectType}default value : 1 Len : 1
                        packet.WriteSkip(unk14); //value name : unk14 default value : unk14Len : 21
                        i++;
                    }
                }
                caster.SendPacketToArea(packet,false,true);
            }
        }
        static readonly byte[] unk14 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        public static void SendAnimateSkillStrikeResponse(Character caster, short spellRealId, DamageAction[] actions, Unit initialTarget)
        {
            SendSetAtackStateGuiResponse(caster);
            using (var packet = new RealmPacketOut(RealmServerOpCode.AnimateSkillStrike)) //5270
            {
                var targetNpc = initialTarget as NPC;
                var targetChr = initialTarget as Character;
                if(targetChr == null && targetNpc == null)
                {
                    caster.SendSystemMessage(string.Format("Wrong spell target {0}. can't animate cast. SpellId {1}",initialTarget,spellRealId));
                }
                packet.WriteInt16(caster.SessionId);//{sessId}default value : 42 Len : 2
                packet.WriteInt16(spellRealId);//{skillId}default value : 508 Len : 2
                packet.WriteInt16(6);//value name : unk6 default value : 6Len : 2
                packet.WriteByte(1);//value name : unk7 default value : 1Len : 2
                packet.WriteByte((byte) (targetNpc == null?Asda2SkillTargetType.Player:Asda2SkillTargetType.Monstr));//value name : targetType default value : 1Len : 2
                if(targetChr!=null && actions!= null)
                {
                    for (int i = 0; i < actions.Length; i++)
                    {
                        var action = actions[i];
                        if (action == null)
                            continue;
                        var status = SpellHitStatus.Ok;
                        if (action.IsCritical)
                            status = SpellHitStatus.Crit;
                        else if (action.Damage == 0)
                            status = SpellHitStatus.Miss;
                        else if (action.Blocked > 0)
                            status = SpellHitStatus.Bloced;
                        if (i < 16)
                        {
                            packet.WriteUInt16(targetChr.SessionId); //{targetId}default value : 82 Len : 2
                            packet.WriteInt32(action.ActualDamage); //{damage}default value : 571 Len : 4
                            packet.WriteInt32((byte) status); //{hitStatus}default value : 1 Len : 4
                            packet.WriteInt32(797); //value name : unk11 default value : 797Len : 4
                            packet.WriteSkip(unk12); //value name : unk12 default value : unk12Len : 15
                        }
                        action.OnFinished();
                    }
                }
                else if (actions != null)
                {
                    for (int i = 0; i < actions.Length; i++)
                    {
                        var action = actions[i];
                        if (action == null)
                            continue;
                        var status = SpellHitStatus.Ok;
                        if (action.IsCritical)
                            status = SpellHitStatus.Crit;
                        else if (action.Damage == 0)
                            status = SpellHitStatus.Miss;
                        else if (action.Blocked > 0)
                            status = SpellHitStatus.Bloced;
                        ushort targetId = 0;
                        if (initialTarget is NPC)
                        {
                            if (action.Victim == null || !(action.Victim is NPC))
                                targetId = ushort.MaxValue;
                            else
                            {
                                targetId = action.Victim.UniqIdOnMap;
                            }
                        }
                        if (i < 16)
                        {
                            packet.WriteUInt16(targetId); //{targetId}default value : 82 Len : 2
                            packet.WriteInt32(action.ActualDamage); //{damage}default value : 571 Len : 4
                            packet.WriteInt32((byte) status); //{hitStatus}default value : 1 Len : 4
                            packet.WriteInt32(797); //value name : unk11 default value : 797Len : 4
                            packet.WriteSkip(unk12); //value name : unk12 default value : unk12Len : 15
                        }
                        action.OnFinished();
                    }
                }
                else if(targetChr!=null)
                {
                    packet.WriteUInt16(targetChr.SessionId); //{targetId}default value : 82 Len : 2
                    packet.WriteInt32(0); //{damage}default value : 571 Len : 4
                    packet.WriteInt32(3); //{hitStatus}default value : 1 Len : 4
                    packet.WriteInt32(0); //value name : unk11 default value : 797Len : 4
                    packet.WriteSkip(unk12); //value name : unk12 default value : unk12Len : 15
                }
                caster.SendPacketToArea(packet, true, false);
            }
            //SendSetSkillCooldownResponse(caster, spell);
            //Asda2MovmentHandler.SendEndMoveByFastInstantRegularMoveResponse(caster);
        }

        public static void SendSetAtackStateGuiResponse(Character chr)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.SetAtackStateGui))//4205
            {
                packet.WriteInt16(chr.SessionId);//{sessId}default value : 7 Len : 2
                packet.WriteInt32(chr.AccId);//{accId}default value : 340701 Len : 4
                chr.SendPacketToArea(packet, true, true);
            }
            
        }
        public static void SendMonstrTakesDamageSecondaryResponse(Character chr,Character targetChr,NPC targetNpc,int damage)
        {
            if(targetChr == null && targetNpc ==null)return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.MonstrTakesDamageSecondary)) //4102
            {
                packet.WriteByte(targetNpc != null?0:1); //value name : unk4 default value : 0Len : 1
                packet.WriteInt16(targetNpc != null?(short)targetNpc.UniqIdOnMap:targetChr.SessionId); //{monstrId}default value : 300 Len : 2
                packet.WriteInt16(160); //{effectId}default value : 160 Len : 2
                packet.WriteInt32(damage); //{damage}default value : 73 Len : 4
                packet.WriteInt32(450); //value name : unk60 default value : 450Len : 4
                packet.WriteByte(1); //value name : unk7 default value : 1Len : 1
                packet.WriteInt16(66); //value name : unk8 default value : 66Len : 2
                packet.WriteByte(0); //value name : unk1 default value : 0Len : 1
                if(targetChr!=null)
                    targetChr.SendPacketToArea(packet, true, true);
                else
                    targetNpc.SendPacketToArea(packet, true, true);
            }
        }

        public static void SendCharacterBuffedResponse(Character target ,Aura aura)
        {
            if(aura.Spell == null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.CharacterBuffed))//5272
            {
                packet.WriteInt16(target.SessionId);//{sessId}default value : 42 Len : 2
                packet.WriteInt16(aura.Spell.RealId);//{effectId}default value : 161 Len : 2
                packet.WriteInt16(aura.Spell.BuffIconId);//{iconId}default value : 161 Len : 2
                packet.WriteInt16(aura.Spell.RealId);//{skillId}default value : 586 Len : 2
                packet.WriteInt16(1);//value name : unk8 default value : 1Len : 2
                packet.WriteByte(2);//value name : unk9 default value : 2Len : 1
                packet.WriteInt16((short)(aura.TimeLeft/1000));//value name : unk10 default value : 240Len : 2
                packet.WriteByte(2);//value name : unk11 default value : 2Len : 1
                packet.WriteSkip(stub14);//{stub14}default value : stub14 Len : 20
                target.SendPacketToArea(packet,true,true);
            }
        }
        static readonly byte[] stub14 = new byte[] { 0x00, 0x00, 0xC6, 0x70, 0xD3, 0x25, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 };

        [PacketHandler(RealmServerOpCode.LearnSkill)] //5253
        public static void LearnSkillRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            var skillId = packet.ReadInt16(); //default : 826Len : 2
            Console.WriteLine(skillId);
            var level = packet.ReadByte(); //default : 7Len : 1
            //Console.WriteLine(level);
            if (client.ActiveCharacter.PlayerSpells.Contains((uint)skillId + 4000))
            {
                var r = client.ActiveCharacter.PlayerSpells.TryLearnSpell(skillId, 5);
                SendSkillLearnedResponse(r, client.ActiveCharacter, (uint)skillId, 5);
                Console.WriteLine("5" + skillId);
            }
            else
            if (client.ActiveCharacter.PlayerSpells.Contains((uint)skillId + 3000))
            {
                var r = client.ActiveCharacter.PlayerSpells.TryLearnSpell(skillId, 4);
                SendSkillLearnedResponse(r, client.ActiveCharacter, (uint)skillId, 4);
                Console.WriteLine("4" + skillId);

            }
            else
            if (client.ActiveCharacter.PlayerSpells.Contains((uint)skillId + 2000))
            {
                var r = client.ActiveCharacter.PlayerSpells.TryLearnSpell(skillId, 3);
                SendSkillLearnedResponse(r, client.ActiveCharacter, (uint)skillId, 3);
                Console.WriteLine("3" + skillId);

            }
            else
            if (client.ActiveCharacter.PlayerSpells.Contains((uint)skillId + 1000))
            {
                var r = client.ActiveCharacter.PlayerSpells.TryLearnSpell(skillId, 2);
                SendSkillLearnedResponse(r, client.ActiveCharacter, (uint)skillId, 2);
                Console.WriteLine("2" + skillId);

            }
            else
            if (client.ActiveCharacter.PlayerSpells.Contains((uint)skillId + 100000))
            {
                var r = client.ActiveCharacter.PlayerSpells.TryLearnSpell(skillId, 1);
                SendSkillLearnedResponse(r, client.ActiveCharacter, (uint)skillId, 1);
                Console.WriteLine("1" + skillId);

            }
             
             
             
            

            if (firstTree.Contains(skillId + 100000) || firstTree.Contains(skillId + 1000) || firstTree.Contains(2000 + skillId) || firstTree.Contains(3000 + skillId) || firstTree.Contains(4000 + skillId) || firstTree.Contains(5000 + skillId))
            {
                client.ActiveCharacter.FirstTree++;
                if (client.ActiveCharacter.Class == ClassId.OHS || client.ActiveCharacter.Class == ClassId.THS || client.ActiveCharacter.Class == ClassId.Spear)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)47, 10,30);
                if (client.ActiveCharacter.Class == ClassId.Crossbow || client.ActiveCharacter.Class == ClassId.Balista || client.ActiveCharacter.Class == ClassId.Crossbow)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)50, 10,30);
                if (client.ActiveCharacter.Class == ClassId.AtackMage || client.ActiveCharacter.Class == ClassId.HealMage || client.ActiveCharacter.Class == ClassId.SupportMage)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)53, 10,30);
            }
            if (secondTree.Contains(skillId + 100000) || secondTree.Contains(1000 + skillId) || secondTree.Contains(2000 + skillId) || secondTree.Contains(3000 + skillId) || secondTree.Contains(4000 + skillId) || secondTree.Contains(5000 + skillId))
            { 
                client.ActiveCharacter.SecondTree++;
                if (client.ActiveCharacter.Class == ClassId.OHS || client.ActiveCharacter.Class == ClassId.THS || client.ActiveCharacter.Class == ClassId.Spear)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)48, 10, 30);
                if (client.ActiveCharacter.Class == ClassId.Crossbow || client.ActiveCharacter.Class == ClassId.Balista || client.ActiveCharacter.Class == ClassId.Crossbow)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)51, 10, 30);
                if (client.ActiveCharacter.Class == ClassId.AtackMage || client.ActiveCharacter.Class == ClassId.HealMage || client.ActiveCharacter.Class == ClassId.SupportMage)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)54, 10, 30);

            }
            if (thirdTree.Contains(skillId + 100000) || thirdTree.Contains(1000 + skillId) || thirdTree.Contains(2000 + skillId) || thirdTree.Contains(3000 + skillId) || thirdTree.Contains(4000 + skillId) || thirdTree.Contains(5000 + skillId))
            { 
                client.ActiveCharacter.ThirdTree++;
                if (client.ActiveCharacter.Class == ClassId.OHS || client.ActiveCharacter.Class == ClassId.THS || client.ActiveCharacter.Class == ClassId.Spear)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)49, 10, 30);
                if (client.ActiveCharacter.Class == ClassId.Crossbow || client.ActiveCharacter.Class == ClassId.Balista || client.ActiveCharacter.Class == ClassId.Crossbow)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)52, 10, 30);
                if (client.ActiveCharacter.Class == ClassId.AtackMage || client.ActiveCharacter.Class == ClassId.HealMage || client.ActiveCharacter.Class == ClassId.SupportMage)
                    client.ActiveCharacter.UpdateTitleCounter((Asda2TitleId)55, 10, 30);

            }
            if (fourthTree.Contains(skillId + 100000) || fourthTree.Contains(1000 + skillId) || fourthTree.Contains(2000 + skillId) || fourthTree.Contains(3000 + skillId) || fourthTree.Contains(4000 + skillId) || fourthTree.Contains(5000 + skillId))
            {
                client.ActiveCharacter.FourthTree++;
            }
            client.ActiveCharacter.CheckAddSpells();
            Asda2CharacterHandler.SendLearnedSkillsInfo(client.ActiveCharacter);
        }
        public static int[] firstTree = new int[] { 501, 502, 520, 503, 523, 581, 551, 537, 524, 542, 535, 522, 582, 514, 509, 517, 595, 594, 521, 540, 525, 545, 526, 600, 513, 510, 596, 531, 530, 597, 544, 701, 702, 723, 708, 719, 807, 737, 743, 724, 738, 799, 711, 712, 715, 729, 739, 747, 775, 762, 722, 763, 709, 703, 973, 801, 789, 728, 811, 741, 761, 905, 901, 992, 944, 908, 960, 993, 912, 556, 957, 959, 958, 907, 914, 951, 966, 991, 1007, 941, 955, 923, 911, 915, 1008, 1012, 961, 998, 964, 1013, 997, 968, 1501, 1502, 1520, 1503, 1523, 1581, 1551, 1537, 1524, 1542, 1535, 1522, 1582, 1514, 1509, 1517, 1595, 1594, 1521, 1540, 1525, 1545, 1526, 1600, 1513, 1510, 1596, 1531, 1530, 1597, 1544, 1701, 1702, 1723, 1708, 1719, 1807, 1737, 1743, 1724, 1738, 1799, 1711, 1712, 1715, 1729, 1739, 1747, 1775, 1762, 1722, 1763, 1709, 1703, 1973, 1801, 1789, 1728, 1811, 1741, 1761, 1905, 1901, 1992, 1944, 1908, 1960, 1993, 1912, 1556, 1957, 1959, 1958, 1907, 1914, 1951, 1966, 1991, 2007, 1941, 1955, 1923, 1911, 1915, 2008, 2012, 1961, 1998, 1964, 2013, 1997, 1968, 2501, 2502, 2520, 2503, 2523, 2581, 2551, 2537, 2524, 2542, 2535, 2522, 2582, 2514, 2509, 2517, 2595, 2594, 2521, 2540, 2525, 2545, 2526, 2600, 2513, 2510, 2596, 2531, 2530, 2597, 2544, 2701, 2702, 2723, 2708, 2719, 2807, 2737, 2743, 2724, 2738, 2799, 2711, 2712, 2715, 2729, 2739, 2747, 2775, 2762, 2722, 2763, 2709, 2703, 2973, 2801, 2789, 2728, 2811, 2741, 2761, 2905, 2901, 2992, 2944, 2908, 2960, 2993, 2912, 2556, 2957, 2959, 2958, 2907, 2914, 2951, 2966, 2991, 3007, 2941, 2955, 2923, 2911, 2915, 3008, 3012, 2961, 2998, 2964, 3013, 2997, 2968, 3501, 3502, 3520, 3503, 3523, 3581, 3551, 3537, 3524, 3542, 3535, 3522, 3582, 3514, 3509, 3517, 3595, 3594, 3521, 3540, 3525, 3545, 3526, 3600, 3513, 3510, 3596, 3531, 3530, 3597, 3544, 3701, 3702, 3723, 3708, 3719, 3807, 3737, 3743, 3724, 3738, 3799, 3711, 3712, 3715, 3729, 3739, 3747, 3775, 3762, 3722, 3763, 3709, 3703, 3973, 3801, 3789, 3728, 3811, 3741, 3761, 3905, 3901, 3992, 3944, 3908, 3960, 3993, 3912, 3556, 3957, 3959, 3958, 3907, 3914, 3951, 3966, 3991, 4007, 3941, 3955, 3923, 3911, 3915, 4008, 4012, 3961, 3998, 3964, 4013, 3997, 3968, 4501, 4502, 4520, 4503, 4523, 4581, 4551, 4537, 4524, 4542, 4535, 4522, 4582, 4514, 4509, 4517, 4595, 4594, 4521, 4540, 4525, 4545, 4526, 4600, 4513, 4510, 4596, 4531, 4530, 4597, 4544, 4701, 4702, 4723, 4708, 4719, 4807, 4737, 4743, 4724, 4738, 4799, 4711, 4712, 4715, 4729, 4739, 4747, 4775, 4762, 4722, 4763, 4709, 4703, 4973, 4801, 4789, 4728, 4811, 4741, 4761, 4905, 4901, 4992, 4944, 4908, 4960, 4993, 4912, 4556, 4957, 4959, 4958, 4907, 4914, 4951, 4966, 4991, 5007, 4941, 4955, 4923, 4911, 4915, 5008, 5012, 4961, 4998, 4964, 5013, 4997, 4968, 5501, 5502, 5520, 5503, 5523, 5581, 5551, 5537, 5524, 5542, 5535, 5522, 5582, 5514, 5509, 5517, 5595, 5594, 5521, 5540, 5525, 5545, 5526, 5600, 5513, 5510, 5596, 5531, 5530, 5597, 5544, 5701, 5702, 5723, 5708, 5719, 5807, 5737, 5743, 5724, 5738, 5799, 5711, 5712, 5715, 5729, 5739, 5747, 5775, 5762, 5722, 5763, 5709, 5703, 5973, 5801, 5789, 5728, 5811, 5741, 5761, 5905, 5901, 5992, 5944, 5908, 5960, 5993, 5912, 5556, 5957, 5959, 5958, 5907, 5914, 5951, 5966, 5991, 6007, 5941, 5955, 5923, 5911, 5915, 6008, 6012, 5961, 5998, 5964, 6013, 5997, 5968 };

        public static int[] secondTree = new int[] { 504, 505, 527, 555, 506, 931, 553, 567, 549, 562, 932, 511, 565, 548, 584, 599, 759, 561, 609, 760, 610, 515, 748, 750, 560, 749, 554, 764, 566, 592, 546, 704, 705, 818, 774, 713, 765, 806, 718, 752, 725, 756, 753, 785, 726, 706, 742, 744, 767, 727, 768, 815, 745, 710, 794, 816, 771, 812, 817, 766, 902, 906, 977, 927, 938, 920, 933, 1014, 953, 978, 935, 918, 994, 930, 1021, 910, 996, 940, 1029, 1016, 990, 1022, 922, 925, 1026, 943, 913, 1006, 1023, 1030, 937, 1031, 1027, 1024, 1504, 1505, 1527, 1555, 1506, 1931, 1553, 1567, 1549, 1562, 1932, 1511, 1565, 1548, 1584, 1599, 1759, 1561, 1609, 1760, 1610, 1515, 1748, 1750, 1560, 1749, 1554, 1764, 1566, 1592, 1546, 1704, 1705, 1818, 1774, 1713, 1765, 1806, 1718, 1752, 1725, 1756, 1753, 1785, 1726, 1706, 1742, 1744, 1767, 1727, 1768, 1815, 1745, 1710, 1794, 1816, 1771, 1812, 1817, 1766, 1902, 1906, 1977, 1927, 1938, 1920, 1933, 2014, 1953, 1978, 1935, 1918, 1994, 1930, 2021, 1910, 1996, 1940, 2029, 2016, 1990, 2022, 1922, 1925, 2026, 1943, 1913, 2006, 2023, 2030, 1937, 2031, 2027, 2024, 2504, 2505, 2527, 2555, 2506, 2931, 2553, 2567, 2549, 2562, 2932, 2511, 2565, 2548, 2584, 2599, 2759, 2561, 2609, 2760, 2610, 2515, 2748, 2750, 2560, 2749, 2554, 2764, 2566, 2592, 2546, 2704, 2705, 2818, 2774, 2713, 2765, 2806, 2718, 2752, 2725, 2756, 2753, 2785, 2726, 2706, 2742, 2744, 2767, 2727, 2768, 2815, 2745, 2710, 2794, 2816, 2771, 2812, 2817, 2766, 2902, 2906, 2977, 2927, 2938, 2920, 2933, 3014, 2953, 2978, 2935, 2918, 2994, 2930, 3021, 2910, 2996, 2940, 3029, 3016, 2990, 3022, 2922, 2925, 3026, 2943, 2913, 3006, 3023, 3030, 2937, 3031, 3027, 3024, 3504, 3505, 3527, 3555, 3506, 3931, 3553, 3567, 3549, 3562, 3932, 3511, 3565, 3548, 3584, 3599, 3759, 3561, 3609, 3760, 3610, 3515, 3748, 3750, 3560, 3749, 3554, 3764, 3566, 3592, 3546, 3704, 3705, 3818, 3774, 3713, 3765, 3806, 3718, 3752, 3725, 3756, 3753, 3785, 3726, 3706, 3742, 3744, 3767, 3727, 3768, 3815, 3745, 3710, 3794, 3816, 3771, 3812, 3817, 3766, 3902, 3906, 3977, 3927, 3938, 3920, 3933, 4014, 3953, 3978, 3935, 3918, 3994, 3930, 4021, 3910, 3996, 3940, 4029, 4016, 3990, 4022, 3922, 3925, 4026, 3943, 3913, 4006, 4023, 4030, 3937, 4031, 4027, 4024, 4504, 4505, 4527, 4555, 4506, 4931, 4553, 4567, 4549, 4562, 4932, 4511, 4565, 4548, 4584, 4599, 4759, 4561, 4609, 4760, 4610, 4515, 4748, 4750, 4560, 4749, 4554, 4764, 4566, 4592, 4546, 4704, 4705, 4818, 4774, 4713, 4765, 4806, 4718, 4752, 4725, 4756, 4753, 4785, 4726, 4706, 4742, 4744, 4767, 4727, 4768, 4815, 4745, 4710, 4794, 4816, 4771, 4812, 4817, 4766, 4902, 4906, 4977, 4927, 4938, 4920, 4933, 5014, 4953, 4978, 4935, 4918, 4994, 4930, 5021, 4910, 4996, 4940, 5029, 5016, 4990, 5022, 4922, 4925, 5026, 4943, 4913, 5006, 5023, 5030, 4937, 5031, 5027, 5024, 5504, 5505, 5527, 5555, 5506, 5931, 5553, 5567, 5549, 5562, 5932, 5511, 5565, 5548, 5584, 5599, 5759, 5561, 5609, 5760, 5610, 5515, 5748, 5750, 5560, 5749, 5554, 5764, 5566, 5592, 5546, 5704, 5705, 5818, 5774, 5713, 5765, 5806, 5718, 5752, 5725, 5756, 5753, 5785, 5726, 5706, 5742, 5744, 5767, 5727, 5768, 5815, 5745, 5710, 5794, 5816, 5771, 5812, 5817, 5766, 5902, 5906, 5977, 5927, 5938, 5920, 5933, 6014, 5953, 5978, 5935, 5918, 5994, 5930, 6021, 5910, 5996, 5940, 6029, 6016, 5990, 6022, 5922, 5925, 6026, 5943, 5913, 6006, 6023, 6030, 5937, 6031, 6027, 6024 };

        public static int[] thirdTree = new int[] { 507, 508, 590, 571, 518, 570, 587, 586, 574, 564, 576, 516, 604, 569, 584, 601, 591, 575, 583, 605, 606, 588, 542, 512, 579, 593, 580, 573, 589, 602, 568, 607, 707, 716, 787, 773, 717, 805, 786, 730, 784, 776, 791, 731, 793, 788, 772, 777, 779, 732, 797, 780, 736, 721, 778, 714, 792, 720, 803, 809, 733, 810, 781, 735, 819, 795, 903, 909, 533, 904, 985, 534, 976, 984, 986, 981, 982, 980, 983, 962, 916, 954, 1003, 1033, 987, 988, 924, 965, 989, 956, 974, 1034, 917, 1004, 1020, 1038, 1018, 1035, 1040, 1039, 1036, 1507, 1508, 1590, 1571, 1518, 1570, 1587, 1586, 1574, 1564, 1576, 1516, 1604, 1569, 1584, 1601, 1591, 1575, 1583, 1605, 1606, 1588, 1542, 1512, 1579, 1593, 1580, 1573, 1589, 1602, 1568, 1607, 1707, 1716, 1787, 1773, 1717, 1805, 1786, 1730, 1784, 1776, 1791, 1731, 1793, 1788, 1772, 1777, 1779, 1732, 1797, 1780, 1736, 1721, 1778, 1714, 1792, 1720, 1803, 1809, 1733, 1810, 1781, 1735, 1819, 1795, 1903, 1909, 1533, 1904, 1985, 1534, 1976, 1984, 1986, 1981, 1982, 1980, 1983, 1962, 1916, 1954, 2003, 2033, 1987, 1988, 1924, 1965, 1989, 1956, 1974, 2034, 1917, 2004, 2020, 2038, 2018, 2035, 2040, 2039, 2036, 2507, 2508, 2590, 2571, 2518, 2570, 2587, 2586, 2574, 2564, 2576, 2516, 2604, 2569, 2584, 2601, 2591, 2575, 2583, 2605, 2606, 2588, 2542, 2512, 2579, 2593, 2580, 2573, 2589, 2602, 2568, 2607, 2707, 2716, 2787, 2773, 2717, 2805, 2786, 2730, 2784, 2776, 2791, 2731, 2793, 2788, 2772, 2777, 2779, 2732, 2797, 2780, 2736, 2721, 2778, 2714, 2792, 2720, 2803, 2809, 2733, 2810, 2781, 2735, 2819, 2795, 2903, 2909, 2533, 2904, 2985, 2534, 2976, 2984, 2986, 2981, 2982, 2980, 2983, 2962, 2916, 2954, 3003, 3033, 2987, 2988, 2924, 2965, 2989, 2956, 2974, 3034, 2917, 3004, 3020, 3038, 3018, 3035, 3040, 3039, 3036, 3507, 3508, 3590, 3571, 3518, 3570, 3587, 3586, 3574, 3564, 3576, 3516, 3604, 3569, 3584, 3601, 3591, 3575, 3583, 3605, 3606, 3588, 3542, 3512, 3579, 3593, 3580, 3573, 3589, 3602, 3568, 3607, 3707, 3716, 3787, 3773, 3717, 3805, 3786, 3730, 3784, 3776, 3791, 3731, 3793, 3788, 3772, 3777, 3779, 3732, 3797, 3780, 3736, 3721, 3778, 3714, 3792, 3720, 3803, 3809, 3733, 3810, 3781, 3735, 3819, 3795, 3903, 3909, 3533, 3904, 3985, 3534, 3976, 3984, 3986, 3981, 3982, 3980, 3983, 3962, 3916, 3954, 4003, 4033, 3987, 3988, 3924, 3965, 3989, 3956, 3974, 4034, 3917, 4004, 4020, 4038, 4018, 4035, 4040, 4039, 4036, 4507, 4508, 4590, 4571, 4518, 4570, 4587, 4586, 4574, 4564, 4576, 4516, 4604, 4569, 4584, 4601, 4591, 4575, 4583, 4605, 4606, 4588, 4542, 4512, 4579, 4593, 4580, 4573, 4589, 4602, 4568, 4607, 4707, 4716, 4787, 4773, 4717, 4805, 4786, 4730, 4784, 4776, 4791, 4731, 4793, 4788, 4772, 4777, 4779, 4732, 4797, 4780, 4736, 4721, 4778, 4714, 4792, 4720, 4803, 4809, 4733, 4810, 4781, 4735, 4819, 4795, 4903, 4909, 4533, 4904, 4985, 4534, 4976, 4984, 4986, 4981, 4982, 4980, 4983, 4962, 4916, 4954, 5003, 5033, 4987, 4988, 4924, 4965, 4989, 4956, 4974, 5034, 4917, 5004, 5020, 5038, 5018, 5035, 5040, 5039, 5036, 5507, 5508, 5590, 5571, 5518, 5570, 5587, 5586, 5574, 5564, 5576, 5516, 5604, 5569, 5584, 5601, 5591, 5575, 5583, 5605, 5606, 5588, 5542, 5512, 5579, 5593, 5580, 5573, 5589, 5602, 5568, 5607, 5707, 5716, 5787, 5773, 5717, 5805, 5786, 5730, 5784, 5776, 5791, 5731, 5793, 5788, 5772, 5777, 5779, 5732, 5797, 5780, 5736, 5721, 5778, 5714, 5792, 5720, 5803, 5809, 5733, 5810, 5781, 5735, 5819, 5795, 5903, 5909, 5533, 5904, 5985, 5534, 5976, 5984, 5986, 5981, 5982, 5980, 5983, 5962, 5916, 5954, 6003, 6033, 5987, 5988, 5924, 5965, 5989, 5956, 5974, 6034, 5917, 6004, 6020, 6038, 6018, 6035, 6040, 6039, 6036 };

        public static int[] fourthTree = new int[] { 632, 617, 633, 619, 634, 635, 636, 622, 637, 623, 627, 639, 611, 624, 613, 618, 615, 626, 638, 628, 612, 630, 614, 631, 616, 841, 826, 842, 828, 843, 844, 845, 831, 846, 834, 836, 820, 833, 822, 827, 824, 835, 837, 847, 821, 839, 823, 840, 825, 1062, 1050, 1065, 1051, 1066, 1063, 1064, 1052, 1067, 1053, 1057, 1045, 1041, 1059, 1043, 1048, 1045, 1056, 1058, 1042, 1060, 1044, 1061, 1054, 1632, 1617, 1633, 1619, 1634, 1635, 1636, 1622, 1637, 1623, 1627, 1639, 1611, 1624, 1613, 1618, 1615, 1626, 1638, 1628, 1612, 1630, 1614, 1631, 1616, 1841, 1826, 1842, 1828, 1843, 1844, 1845, 1831, 1846, 1834, 1836, 1820, 1833, 1822, 1827, 1824, 1835, 1837, 1847, 1821, 1839, 1823, 1840, 1825, 2062, 2050, 2065, 2051, 2066, 2063, 2064, 2052, 2067, 2053, 2057, 2045, 2041, 2059, 2043, 2048, 2045, 2056, 2058, 2042, 2060, 2044, 2061, 2054, 2632, 2617, 2633, 2619, 2634, 2635, 2636, 2622, 2637, 2623, 2627, 2639, 2611, 2624, 2613, 2618, 2615, 2626, 2638, 2628, 2612, 2630, 2614, 2631, 2616, 2841, 2826, 2842, 2828, 2843, 2844, 2845, 2831, 2846, 2834, 2836, 2820, 2833, 2822, 2827, 2824, 2835, 2837, 2847, 2821, 2839, 2823, 2840, 2825, 3062, 3050, 3065, 3051, 3066, 3063, 3064, 3052, 3067, 3053, 3057, 3045, 3041, 3059, 3043, 3048, 3045, 3056, 3058, 3042, 3060, 3044, 3061, 3054, 3632, 3617, 3633, 3619, 3634, 3635, 3636, 3622, 3637, 3623, 3627, 3639, 3611, 3624, 3613, 3618, 3615, 3626, 3638, 3628, 3612, 3630, 3614, 3631, 3616, 3841, 3826, 3842, 3828, 3843, 3844, 3845, 3831, 3846, 3834, 3836, 3820, 3833, 3822, 3827, 3824, 3835, 3837, 3847, 3821, 3839, 3823, 3840, 3825, 4062, 4050, 4065, 4051, 4066, 4063, 4064, 4052, 4067, 4053, 4057, 4045, 4041, 4059, 4043, 4048, 4045, 4056, 4058, 4042, 4060, 4044, 4061, 4054, 4632, 4617, 4633, 4619, 4634, 4635, 4636, 4622, 4637, 4623, 4627, 4639, 4611, 4624, 4613, 4618, 4615, 4626, 4638, 4628, 4612, 4630, 4614, 4631, 4616, 4841, 4826, 4842, 4828, 4843, 4844, 4845, 4831, 4846, 4834, 4836, 4820, 4833, 4822, 4827, 4824, 4835, 4837, 4847, 4821, 4839, 4823, 4840, 4825, 5062, 5050, 5065, 5051, 5066, 5063, 5064, 5052, 5067, 5053, 5057, 5045, 5041, 5059, 5043, 5048, 5045, 5056, 5058, 5042, 5060, 5044, 5061, 5054, 5632, 5617, 5633, 5619, 5634, 5635, 5636, 5622, 5637, 5623, 5627, 5639, 5611, 5624, 5613, 5618, 5615, 5626, 5638, 5628, 5612, 5630, 5614, 5631, 5616, 5841, 5826, 5842, 5828, 5843, 5844, 5845, 5831, 5846, 5834, 5836, 5820, 5833, 5822, 5827, 5824, 5835, 5837, 5847, 5821, 5839, 5823, 5840, 5825, 6062, 6050, 6065, 6051, 6066, 6063, 6064, 6052, 6067, 6053, 6057, 6045, 6041, 6059, 6043, 6048, 6045, 6056, 6058, 6042, 6060, 6044, 6061, 6054 };

        public static void SendSkillLearnedResponse(SkillLearnStatus status, Character ownerChar, uint id, int level)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.SkillLearned)) //5254
            {
                packet.WriteByte((byte)status); //{status}default value : 1 Len : 1
                packet.WriteInt16(ownerChar.Spells.AvalibleSkillPoints); //value name : skillpoints default value : stab7Len : 2
                //packet.WriteInt32(ownerChar.Money); //{money}default value : 33198985 Len : 4
                packet.WriteInt16(id); //{skillId}default value : 830 Len : 2
                packet.WriteByte(level); //{skillLevel}default value : 2 Len : 1
                packet.WriteSkip(stab16); //value name : stab16 default value : stab16Len : 1
                packet.WriteInt16(ownerChar.Asda2Strength); //{str}default value : 166 Len : 2
                packet.WriteInt16(ownerChar.Asda2Agility); //{dex}default value : 74 Len : 2
                packet.WriteInt16(ownerChar.Asda2Stamina); //{stamina}default value : 120 Len : 2
                packet.WriteInt16(ownerChar.Asda2Spirit); //{spirit}default value : 48 Len : 2
                packet.WriteInt16(ownerChar.Asda2Intellect); //{intelect}default value : 74 Len : 2
                packet.WriteInt16(ownerChar.Asda2Luck); //{luck}default value : 48 Len : 2
                packet.WriteInt16(0); //{bonusStr}default value : 274 Len : 2
                packet.WriteInt16(0); //{bonusDex}default value : 33 Len : 2
                packet.WriteInt16(0); //{bonusStamina}default value : 18 Len : 2
                packet.WriteInt16(0); //{bonusSpirit}default value : 7 Len : 2
                packet.WriteInt16(0); //{bonusInt}default value : 0 Len : 2
                packet.WriteInt16(0); //{bonusLuck}default value : 0 Len : 2
                packet.WriteInt16(ownerChar.Asda2Strength); //{str0}default value : 166 Len : 2
                packet.WriteInt16(ownerChar.Asda2Agility); //{dex0}default value : 74 Len : 2
                packet.WriteInt16(ownerChar.Asda2Stamina); //{stamin}default value : 120 Len : 2
                packet.WriteInt16(ownerChar.Asda2Spirit); //{spirit0}default value : 48 Len : 2
                packet.WriteInt16(ownerChar.Asda2Intellect); //{int}default value : 74 Len : 2
                packet.WriteInt16(ownerChar.Asda2Luck); //{lucj}default value : 48 Len : 2
                packet.WriteInt32(ownerChar.MaxHealth); //{maxHealth}default value : 1615 Len : 4
                packet.WriteInt16(ownerChar.MaxPower); //{maxMana}default value : 239 Len : 2
                packet.WriteInt32(ownerChar.Health); //{curHealth}default value : 1615 Len : 4
                packet.WriteInt16(ownerChar.Power); //{curMp}default value : 227 Len : 2
                packet.WriteInt16((short)ownerChar.MinDamage); //{minAtack}default value : 482 Len : 2
                packet.WriteInt16((short)ownerChar.MaxDamage); //{MaxAtack}default value : 542 Len : 2
                packet.WriteInt16(ownerChar.MinMagicDamage); //{minMatack}default value : 45 Len : 2
                packet.WriteInt16(ownerChar.MaxMagicDamage); //{maxMtack}default value : 64 Len : 2
                packet.WriteInt16((short)ownerChar.Asda2MagicDefence); //{MDef}default value : 68 Len : 2
                packet.WriteInt16((short)ownerChar.Asda2Defence); //{minDef}default value : 193 Len : 2
                packet.WriteInt16((short)ownerChar.Asda2Defence); //{maxDef}default value : 207 Len : 2
                packet.WriteFloat(ownerChar.BlockChance); //{minBlock}default value : 0 Len : 4
                packet.WriteFloat(ownerChar.BlockValue); //{maxBlock}default value : 0 Len : 4
                packet.WriteInt16(15); //value name : unk41 default value : 15Len : 2
                packet.WriteInt16(7); //value name : unk42 default value : 7Len : 2
                packet.WriteInt16(4); //value name : unk43 default value : 4Len : 2
                packet.WriteSkip(stub87); //{stub87}default value : stub87 Len : 28*/
                ownerChar.Send(packet, addEnd: false);
            }
        }

        private static readonly byte[] stab7 = new byte[] {0x05, 0x00};
        private static readonly byte[] stab16 = new byte[] {0x01};

        private static readonly byte[] stub87 = new byte[28];
        public static void SendSkillLearnedFirstTimeResponse(IRealmClient client, short skillId,int cooldownSecs)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.SkillLearnedFirstTime))//6056
            {
                packet.WriteInt16(skillId);//{skillId}default value : 981 Len : 2
                packet.WriteByte(1);//value name : unk5 default value : 1Len : 1
                packet.WriteByte(1);//value name : unk6 default value : 1Len : 1
                packet.WriteInt16(cooldownSecs);//{cooldown}default value : 10000 Len : 2
                packet.WriteSkip(stab12);//value name : stab12 default value : stab12Len : 2
                packet.WriteInt16(271);//value name : unk9 default value : 271Len : 2
                packet.WriteInt32(28);//value name : unk10 default value : 28Len : 4
                packet.WriteByte(100);//value name : unk11 default value : 100Len : 1
                packet.WriteByte(100);//value name : unk12 default value : 100Len : 1
                packet.WriteInt16(8);//value name : unk2 default value : 8Len : 2
                packet.WriteSkip(stab24);//value name : stab24 default value : stab24Len : 16
                client.Send(packet, addEnd: true);
            }
        }
        static readonly byte[] stab12 = new byte[] { 0x00, 0x00 };
        static readonly byte[] stab24 = new byte[] { 0x08, 0x00, 0xE0, 0x93, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        [PacketHandler((RealmServerOpCode)5430)] //5253
        public static void U5330(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)9915)] //5253
        public static void U9915(IRealmClient client, RealmPacketIn packet)
        {
        }

        [PacketHandler(RealmServerOpCode.ConquerRequest)] //5253
        public static void ConquerRequest(IRealmClient client, RealmPacketIn packet)
        {

        }
        //[PacketHandler((RealmServerOpCode)5070)] //5253
        //public static void U5070(IRealmClient client, RealmPacketIn packet)
        //{
        //    packet.Position -= 11;
        //    var itemid = packet.ReadInt16();
        //    U5071(client);
        //}

        //public static void U5071(IRealmClient client)
        //{
        //    using (RealmPacketOut packet = new RealmPacketOut((RealmServerOpCode)5071))
        //    {
        //        packet.WriteByte(1);
        //        packet.WriteInt32(client.ActiveCharacter.AccId);
        //        //packet.WriteInt16(client.ActiveCharacter.SessionId);
        //        packet.WriteInt16(507);
        //        packet.WriteInt16(508);
        //        client.Send(packet);
        //    }
        //}
        public static void SendConquerResponse(IRealmClient client/*, Asda2Battleground btlgrnd, BattlegroundResultRecord record*/)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.ConquerResponse))
            {
                
            }
        }


        [PacketHandler((RealmServerOpCode)1010)] //5253
        public static void U1010(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)6072)] //5253
        public static void U6072(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)6084)] //5253
        public static void U6084(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)6059)] //5253
        public static void U6059(IRealmClient client, RealmPacketIn packet)
        {

        }
        
        [PacketHandler((RealmServerOpCode)6749)] //5253
        public static void U6749(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)4400)] //5253
        public static void U4400(IRealmClient client, RealmPacketIn packet)
        {
        }
        

        [PacketHandler((RealmServerOpCode)6577)] //5253
        public static void U6577(IRealmClient client, RealmPacketIn packet)
        {
        }
        [PacketHandler((RealmServerOpCode)5474)] //5253
        public static void U5474(IRealmClient client, RealmPacketIn packet)
        {
        }


        [PacketHandler(RealmServerOpCode.DiceRequest)] //5253
        public static void DiceRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 17;
            var num1 = packet.ReadByte();
            var num2 = packet.ReadByte();
            client.ActiveCharacter.SendErrorMsg("num1 = " + num1);
            client.ActiveCharacter.SendErrorMsg("num2 = " + num2);
            var result = Util.Utility.Random(num1, num2);
            SendDiceResponse(client.ActiveCharacter, num1, num2, result);
        }

        public static void SendDiceResponse(Character chr, int num1, int num2, int result)
        {
            using (RealmPacketOut packet = new RealmPacketOut(RealmServerOpCode.DiceResponse))
            {
                packet.WriteInt32(chr.AccId);
                packet.WriteInt16(chr.SessionId);
                packet.WriteInt16(num1);
                packet.WriteInt16(num2);
                packet.WriteInt16(result);
                packet.WriteFixedAsciiString(chr.Name, 20);
                chr.SendPacketToArea(packet, true, true, Locale.Any, 50f);
            }
        }

        [PacketHandler(RealmServerOpCode.CommisProd)]
        public static void CommisProdRequest(IRealmClient client, RealmPacketIn packet1)
        {
            packet1.Position -= 5;
            short slotInq = packet1.ReadInt16();
            ++packet1.Position;
            int amount = packet1.ReadInt32();
            Asda2Item regularItem = client.ActiveCharacter.Asda2Inventory.GetRegularItem(slotInq);
            //client.ActiveCharacter.SendErrorMsg(string.Format("Slot = {0} + Amount = {1}", slotInq, amount));
            if (regularItem == null || regularItem.Category != Asda2ItemCategory.Token)
                return;
            if (amount > regularItem.Amount)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Incorrect item amount", 1);
                client.ActiveCharacter.SendErrorMsg("الكمية خاطئة");
            }
            else
            {
                
                CommisProdResponse(client, regularItem, amount);
            }
        }

        public static void CommisProdResponse(IRealmClient client, Asda2Item item, int amount)
        {
            var itemid = item.ItemId;
            using (RealmPacketOut packet = new RealmPacketOut(RealmServerOpCode.CommisProdRes))
            {
                if (amount <= item.Amount)
                {
                    if (item.Amount - amount <= 0)
                    {
                        packet.WriteInt32(client.ActiveCharacter.AccId);
                        packet.WriteInt32(item.ItemId);
                        packet.WriteByte((byte)item.InventoryType);
                        packet.WriteInt32(item.Slot);
                        packet.WriteInt32(0);
                        packet.WriteByte(0);
                        packet.WriteByte(10);
                        packet.WriteSkip(stab15);
                        item.Amount -= amount;
                    }
                    else
                    {
                        item.Amount -= amount;
                        packet.WriteInt32(client.ActiveCharacter.AccId);
                        packet.WriteInt32(item.ItemId);
                        packet.WriteByte((byte)item.InventoryType);
                        packet.WriteInt32(item.Slot);
                        packet.WriteInt32(item.Amount);
                        packet.WriteByte(0);
                        packet.WriteByte(10);
                        packet.WriteSkip(stab15);
                    }
                }

                if (item.Amount <= 0)
                    Asda2InventoryHandler.ItemRemovedFromInventoryResponse(client.ActiveCharacter, item,
                      DeleteOrSellItemStatus.Ok);
                client.ActiveCharacter.Send(packet, true);
                Asda2TitleChecker.OnTokenItem(client.ActiveCharacter, itemid, amount);
            }
        }
    }

    internal enum Asda2MobSpellUseType
    {
        Damage =1,
        Buff =2,
        Debuff =3,
    }

    internal enum Asda2UseSkillResult
    {
        CannotApplyThisSkill =0,
        Ok=1,
        LowMp =2,
        WrongJob =3,
        WrongWeapon =4,
        CantBeUsedWhilePowerupingSkill =5,
        ItIsNotAnActiveSkill =6,

    }

    public enum SkillLearnStatus
    {
        Ok = 1,
        SkillLevelIsMaximum = 2,
        YouCantLevelUpThisSkill = 3,
        YouCantUseSkillPointsBecauseYouDidntHaveAllTerms = 4,
        YouCantUseYourPointsInThisSkill = 5,
        NoEnoughSpechialSkillPoints = 6,
        YouCantLearnThisSkillWithYourCurrentClass = 7,
        YouDontHaveEnoughSkillPointsToGoToThe4thProff = 8,
        FailedToUseSkillPoints = 9,
        YourInventoryHasBenExpanded = 10,
        CCHasBeedRecharged = 11,
        CannontOpenStatusWindow = 12,

    }
    public enum Asda2SkillTargetType
    {
        Player = 1,
        Monstr = 0
    }
}
