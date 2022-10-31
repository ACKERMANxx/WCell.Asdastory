using System;
using System.Linq;
using WCell.Constants;
using WCell.Constants.Items;
using WCell.Core;
using WCell.Core.Network;
using WCell.RealmServer.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Network;
using WCell.Util;

namespace WCell.RealmServer.Asda2PetSystem
{
    public static class Asda2PetHandler
    {
        //TODO Add change name cert system
        [PacketHandler(RealmServerOpCode.ChangePetName)]//6118
        public static void ChangePetNameRequest(IRealmClient client, RealmPacketIn packet)
        {
            var petGuid = packet.ReadInt32();//default : 54857Len : 4
            var petName = packet.ReadAsdaString(16, Locale.En);//default : Len : 17
            packet.Position += 1;
            var petCertSlot = packet.ReadInt16();
            if (!client.ActiveCharacter.OwnedPets.ContainsKey(petGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to summon not existing pet.", 20);
                SendPetNameChangedResponse(client, Asda2PetNamehangeResult.AbnormalPetInfo, null, null);
                return;
            }
            var pet = client.ActiveCharacter.OwnedPets[petGuid];

            var changeNameItem = client.ActiveCharacter.Asda2Inventory.GetShopShopItem(petCertSlot);
            if (!pet.CanChangeName)
            {
                if (changeNameItem == null)
                {
                    SendPetNameChangedResponse(client, Asda2PetNamehangeResult.YouMustHavePremiumItemToChangePetName,
                                               pet, null);
                    return;
                }
                changeNameItem.ModAmount(-1);
            }
            pet.Name = petName;
            pet.CanChangeName = false;
            SendPetNameChangedResponse(client, Asda2PetNamehangeResult.Ok, pet, changeNameItem);
            GlobalHandler.UpdateCharacterPetInfoToArea(client.ActiveCharacter);
        }
        public static void SendPetNameChangedResponse(IRealmClient client, Asda2PetNamehangeResult status, Asda2PetRecord pet, Asda2Item changeNameItem)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetNameChanged))//6119
            {
                packet.WriteByte((byte)status);//{status}default value : 1 Len : 1
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 355335 Len : 4
                packet.WriteInt32(pet == null ? 0 : pet.Guid);//{petId}default value : 54857 Len : 4
                packet.WriteFixedAsciiString(pet == null ? "" : pet.Name, 16);//{petName}default value :  Len : 21
                Asda2InventoryHandler.WriteItemInfoToPacket(packet, changeNameItem, false);
                client.Send(packet, addEnd: true);
            }
        }
        static readonly byte[] stub26 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        [PacketHandler(RealmServerOpCode.SummonPet)]//6100
        public static void SummonPetRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 5;
            var petGuid = packet.ReadInt32();//default : 54857Len : 4
            client.ActiveCharacter.SendErrorMsg(string.Format("PetGuid = {0}", petGuid));
            if (!client.ActiveCharacter.OwnedPets.ContainsKey(petGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to summon not existing pet.", 20);
                SendPetSummonOrUnSummondResponse(client, null);
                return;
            }
            var pet = client.ActiveCharacter.OwnedPets[petGuid];
            if (pet.HungerPrc == 0)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to summon dead pet.", 50);
                SendPetSummonOrUnSummondResponse(client, null);
                return;
            }
            /*if (pet.Template.MinimumUsingLevel > client.ActiveCharacter.Level)
            {
                client.ActiveCharacter.SendInfoMsg("You cant summon pet with grade higher than your level.");
                SendPetSummonOrUnSummondResponse(client, null);
                return;
            }*/
            if (client.ActiveCharacter.Asda2Pet != null)
            {
                pet = client.ActiveCharacter.Asda2Pet;
                client.ActiveCharacter.Asda2Pet.RemoveStatsFromOwner();
                client.ActiveCharacter.Asda2Pet = null;
                GlobalHandler.UpdateCharacterPetInfoToArea(client.ActiveCharacter);
                SendPetSummonOrUnSummondResponse(client, pet);

                Asda2CharacterHandler.SendUpdateStatsResponse(client);
                Asda2CharacterHandler.SendUpdateStatsOneResponse(client);
                return;
            }
            client.ActiveCharacter.Asda2Pet = pet;
            client.ActiveCharacter.Asda2Pet.AddStatsToOwner();
            client.ActiveCharacter.LastPetExpGainTime = (uint)Environment.TickCount + 60000;
            GlobalHandler.UpdateCharacterPetInfoToArea(client.ActiveCharacter);

            Asda2CharacterHandler.SendUpdateStatsResponse(client);
            Asda2CharacterHandler.SendUpdateStatsOneResponse(client);
            SendPetSummonOrUnSummondResponse(client, pet);
        }

        public static void SendPetSummonOrUnSummondResponse(IRealmClient client, Asda2PetRecord pet)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetSummonOrUnSummond))//6101
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 355335 Len : 4
                packet.WriteByte(pet == null ? 0 : 1);//{status}default value : 1 Len : 1
                packet.WriteInt32(pet == null ? 0 : pet.Guid);//{petId}default value : 54857 Len : 4
                client.Send(packet, addEnd: true);
            }
        }
        public static void SendUpdatePetExpResponse(IRealmClient client, Asda2PetRecord pet, bool levelUped = false)
        {
            if (pet == null || client.ActiveCharacter == null) return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.UpdatePetExp))//6105
            {
                packet.WriteInt16(client.ActiveCharacter.SessionId);//{sessId}default value : 15 Len : 2
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 355335 Len : 4
                packet.WriteInt32(pet.Guid);//{petId}default value : 54857 Len : 4
                packet.WriteInt16(pet.Expirience);//{petExp}default value : 83 Len : 4
                packet.WriteByte(levelUped ? 1 : 0);//value name : unk1 default value : 0Len : 1
                packet.WriteByte(pet.Level);
                packet.WriteByte(0);
                client.Send(packet, addEnd: true);
            }
        }
        public static void SendUpdatePetHungerResponse(IRealmClient client, Asda2PetRecord pet)
        {
            if (!client.IsGameServerConnection || client.ActiveCharacter == null || pet == null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.UpdatePetHunger))//6104
            {
                packet.WriteInt16(client.ActiveCharacter.SessionId);//{sessId}default value : 15 Len : 2
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 355335 Len : 4
                packet.WriteInt32(pet.Guid);//{petGuid}default value : 54857 Len : 4
                packet.WriteByte(pet.HungerPrc);//{HungerPrc}default value : 58 Len : 1
                packet.WriteInt16((short)pet.Stat1Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat1Value);//{stat1Value}default value : 31 Len : 4
                packet.WriteInt16((short)pet.Stat2Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat2Value);//{stat1Value}default value : 31 Len : 4
                packet.WriteInt16((short)pet.Stat3Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat3Value);//{stat1Value}default value : 31 Len : 4
                client.Send(packet, addEnd: true);
            }
        }
        public static void SendPetGoesSleepDueStarvationResponse(IRealmClient client, Asda2PetRecord pet)
        {
            if (client == null || client.ActiveCharacter == null || pet == null)
                return;
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetGoesSleepDueStarvation))//6107
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 355335 Len : 4
                packet.WriteInt32(pet.Guid);//{petGuid}default value : 54857 Len : 4
                client.Send(packet, addEnd: true);
            }
        }

        [PacketHandler(RealmServerOpCode.HatchEgg)]//6102
        public static void HatchEggRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 5;
            var invNumInq = packet.ReadByte();//default : 2Len : 1
            var slotInq = packet.ReadInt16();//default : 5Len : 2
            var invEgg = packet.ReadByte();//default : 2Len : 1
            var slotEgg = packet.ReadInt16();//default : 11Len : 2
            var invSupl = packet.ReadByte();//default : 0Len : 1
            var slotSupl = packet.ReadInt16();//default : -1Len : 2
            Console.WriteLine("invNumInq: " + invNumInq.ToString());
            Console.WriteLine("invEgg: " + invEgg.ToString());
            var err = client.ActiveCharacter.Asda2Inventory.HatchEgg(slotInq, slotEgg, slotSupl, invNumInq, invEgg);
            if (err != HatchEggStatus.Ok)
            {
                SendEggHatchedResponse(client, err, invNumInq, slotInq, invEgg, slotEgg, invSupl, slotSupl);
            }
            else
            {
                SendEggHatchedResponse(client, err, invNumInq, slotInq, invEgg, slotEgg, invSupl, slotSupl);
            }
        }
        public static void SendEggHatchedResponse(IRealmClient client, HatchEggStatus status, byte invInq = 0, short inqSlot = -1, byte invEgg = 0, short slotEgg = -1, byte invSupl = 0, short slotSupl = -1)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.EggHatched))//6103
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 361343 Len : 4
                packet.WriteByte((byte)status);//{status}default value : 1 Len : 1
                packet.WriteByte(invInq);//{invInq}default value : 2 Len : 1
                packet.WriteInt16(inqSlot);//{inqSlot}default value : 29 Len : 2
                packet.WriteByte(invEgg);//{invEgg}default value : 2 Len : 1
                packet.WriteInt16(slotEgg);//{slotEgg}default value : 23 Len : 2
                packet.WriteByte(invSupl);//{invSupl}default value : 0 Len : 1
                packet.WriteInt16(slotSupl);//{slotSupl}default value : -1 Len : 2
                packet.WriteInt32(client.ActiveCharacter.Asda2Inventory.Weight);//{weight}default value : 11275 Len : 4
                client.Send(packet);
            }
        }

        public static void SendInitPetInfoOnLoginResponse(IRealmClient client, Asda2PetRecord pet)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.InitPetInfoOnLogin))//6106
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 361343 Len : 4
                packet.WriteByte(1);//value name : unk6 default value : 0Len : 1
                packet.WriteInt32(pet.Guid);//{petGuid}default value : 69831 Len : 4
                packet.WriteInt16(pet.Id);//{petId}default value : 52 Len : 2
                packet.WriteFixedAsciiString(pet.Name, 16);//{PetName}default value :  Len : 16
                packet.WriteByte(pet.HungerPrc);//{hugr}default value : 100 Len : 1
                packet.WriteByte(pet.Level);//{curLvl}default value : 1 Len : 1
                packet.WriteByte(pet.MaxLevel);//{maxLvl}default value : 3 Len : 1
                packet.WriteInt16(pet.Expirience);//{exp}default value : 0 Len : 2
                packet.WriteByte(pet.Level);//{curPetLevel}default value : 1 Len : 1
                packet.WriteInt16((short)pet.Stat1Type);//{stat1Type}default value : 3 Len : 2
                packet.WriteInt32(pet.Stat1Value);//{stat1Value}default value : 71 Len : 4
                packet.WriteInt16((short)pet.Stat2Type);//{stat1Type}default value : 3 Len : 2
                packet.WriteInt32(pet.Stat2Value);//{stat1Value}default value : 71 Len : 4
                packet.WriteInt16((short)pet.Stat3Type);//{stat1Type}default value : 3 Len : 2
                packet.WriteInt32(pet.Stat3Value);//{stat1Value}default value : 71 Len : 4
                //packet.WriteByte(client.ActiveCharacter.Asda2Pet == null ? 0 : client.ActiveCharacter.Asda2Pet.Guid == pet.Guid ? 1 : 0);
                client.Send(packet);
            }
        }

        [PacketHandler(RealmServerOpCode.ResurectPet)]//6108
        public static void ResurectPetRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 5;
            var petGuid = packet.ReadInt32();//default : 68412Len : 4
            var invNum = packet.ReadByte();//default : 2Len : 1
            var slot = packet.ReadInt16();//default : 59Len : 2
            //client.ActiveCharacter.SendErrorMsg(string.Format("PetGuid = {0}", petGuid));
            //client.ActiveCharacter.SendErrorMsg(string.Format("invNum = {0}", invNum));
            //client.ActiveCharacter.SendErrorMsg(string.Format("slot = {0}", slot));

            if (!client.ActiveCharacter.OwnedPets.ContainsKey(petGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to resurect not existing pet", 20);
                return;
            }
            var item = client.ActiveCharacter.Asda2Inventory.GetRegularItem(slot);
            if (item == null)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to resurect pet with not existint resurect item.", 10);
                return;
            }
            var pet = client.ActiveCharacter.OwnedPets[petGuid];
            if (item.Category == Asda2ItemCategory.PetFoodMeat || item.Category == Asda2ItemCategory.PetFoodVegetable || item.Category == Asda2ItemCategory.PetFoodOil)
            {
                if (client.ActiveCharacter.Asda2Pet != null)
                {
                    client.ActiveCharacter.Asda2Pet.Feed(item.Template.ValueOnUse / 2);
                    item.ModAmount(-1);
                    SendPetResurectedResponse(client, pet, item);
                }
                else
                {
                    client.ActiveCharacter.SendInfoMsg("You must summon pet to feed.");
                }
                return;
            }
            if (item.Category != Asda2ItemCategory.PetResurect)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to resurect pet with not a resurect item.", 50);
                return;
            }
            if (pet.HungerPrc != 0)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to resurect alive pet.", 50);
                return;
            }
            pet.HungerPrc = 5;
            item.ModAmount(-1);
            SendPetResurectedResponse(client, pet, item);
        }
        public static void SendPetResurectedResponse(IRealmClient client, Asda2PetRecord pet, Asda2Item resurectPetItem)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetResurected))//6109
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 361343 Len : 4
                packet.WriteInt32(pet.Guid);//{petGuid}default value : 68412 Len : 4
                packet.WriteInt32(resurectPetItem.ItemId);//{itemId}default value : 31981 Len : 4
                packet.WriteByte(2);//{inv}default value : 2 Len : 1
                packet.WriteInt16(resurectPetItem.Slot);//{slot}default value : 8 Len : 2
                packet.WriteInt16(resurectPetItem.IsDeleted ? -1 : 0);//{minusOneIfDelete}default value : -1 Len : 2
                packet.WriteInt32(resurectPetItem.Amount);//{amount}default value : 0 Len : 4
                packet.WriteByte(0);//value name : stab25 default value : stab25Len : 1
                packet.WriteInt16(resurectPetItem.Amount);//{amount0}default value : 0 Len : 2
                packet.WriteSkip(stab28);//value name : stab28 default value : stab28Len : 41
                packet.WriteByte(1);//value name : unk1 default value : 1Len : 1
                packet.WriteByte(pet.HungerPrc);//{HungerPrc}default value : 58 Len : 1
                packet.WriteInt32(client.ActiveCharacter.Asda2Inventory.Weight);//{invWeight}default value : 0 Len : 4
                packet.WriteInt16((short)pet.Stat1Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat1Value);//{stat1Value}default value : 31 Len : 4
                packet.WriteInt16((short)pet.Stat2Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat2Value);//{stat1Value}default value : 31 Len : 4
                packet.WriteInt16((short)pet.Stat2Type);//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet.Stat2Value);//{stat1Value}default value : 31 Len : 4
                client.Send(packet, addEnd: true);
            }
        }

        [PacketHandler(RealmServerOpCode.RemovePet)]//6110
        public static void RemovePetRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 5;
            var petGuid = packet.ReadInt32(); //default : 68411Len : 4
            if (!client.ActiveCharacter.OwnedPets.ContainsKey(petGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying delete not existing pet.", 10);
                return;
            }
            if (client.ActiveCharacter.Asda2Pet != null && client.ActiveCharacter.Asda2Pet.Guid == petGuid)
            {
                client.ActiveCharacter.SendInfoMsg(".لا تستطيع حذف اليف قيد الإستدعاء");
                return;
            }
            var pet = client.ActiveCharacter.OwnedPets[petGuid];
            client.ActiveCharacter.OwnedPets.Remove(petGuid);
            SendPetRemovedResponse(client, pet.Guid);
            pet.DeleteLater();
        }

        public static void SendPetRemovedResponse(IRealmClient client, int petGuid)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetRemoved))//6111
            {
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 361343 Len : 4
                packet.WriteInt32(petGuid);//{petGuId}default value : 68411 Len : 4
                packet.WriteByte(1);//value name : unk1 default value : 1Len : 1
                client.Send(packet, addEnd: true);
            }
        }

        [PacketHandler(RealmServerOpCode.PetSyntes)]//6112
        public static void PetSyntesRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 3;
            var pet1Guid = packet.ReadInt32();//default : 69837Len : 4
            var pet2Guid = packet.ReadInt32();//default : 69831Len : 4
            var pet3Guid = packet.ReadInt32();//default : 69831Len : 4
            client.ActiveCharacter.SendErrorMsg(string.Format("Pet1 : {0}", pet1Guid));
            client.ActiveCharacter.SendErrorMsg(string.Format("Pet2 : {0}", pet2Guid));
            client.ActiveCharacter.SendErrorMsg(string.Format("Pet3 : {0}", pet3Guid));

            if (!client.ActiveCharacter.OwnedPets.ContainsKey(pet1Guid) || !client.ActiveCharacter.OwnedPets.ContainsKey(pet2Guid) || !client.ActiveCharacter.OwnedPets.ContainsKey(pet3Guid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Tries to sysnes with not existing pets.", 20);
                SendPetSyntesResultResponse(client, PetSynethisResult.AbnormalPetInfo);
                return;
            }
            if (client.ActiveCharacter.Asda2Pet != null && (client.ActiveCharacter.Asda2Pet.Guid == pet1Guid || client.ActiveCharacter.Asda2Pet.Guid == pet2Guid || client.ActiveCharacter.Asda2Pet.Guid == pet3Guid))
            {
                SendPetSyntesResultResponse(client, PetSynethisResult.CantUseCurrentlySummonedPet);
                return;
            }
            var pet1 = client.ActiveCharacter.OwnedPets[pet1Guid];
            var pet2 = client.ActiveCharacter.OwnedPets[pet2Guid];
            var pet3 = client.ActiveCharacter.OwnedPets[pet3Guid];
            if (pet1.Level < 4 && pet2.Level < 3 && pet3.Level < 3)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Tries to sysnes with low level pets.", 50);
                SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                return;
            }
            if(pet1.Template.Id == 13 && pet2.Template.Id == 1 && pet3.Template.Id == 10)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(21);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 13 && pet2.Template.Id == 2 && pet3.Template.Id == 9)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(22);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 13 && pet2.Template.Id == 15 && pet3.Template.Id == 7)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(23);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 14 && pet2.Template.Id == 4 && pet3.Template.Id == 8)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(27);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 14 && pet2.Template.Id == 5 && pet3.Template.Id == 7)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(28);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 14 && pet2.Template.Id == 1 && pet3.Template.Id == 10)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(29);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 20 && pet2.Template.Id == 3 && pet3.Template.Id == 9)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(25);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 20 && pet2.Template.Id == 2 && pet3.Template.Id == 5)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(24);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else if (pet1.Template.Id == 20 && pet2.Template.Id == 3 && pet3.Template.Id == 4)
            {
                var resultPetTemplate = pet1.Template.GetPetTemplateId(26);
                if (resultPetTemplate == null)
                {
                    SendPetSyntesResultResponse(client, PetSynethisResult.LowPetLevel);
                    return;
                }
                var resultPet = client.ActiveCharacter.AddAsda2Pet(resultPetTemplate, true);
                client.ActiveCharacter.OwnedPets.Remove(pet1Guid);
                SendPetRemovedResponse(client, pet1Guid);
                pet1.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet2Guid);
                SendPetRemovedResponse(client, pet2Guid);
                pet2.DeleteLater();
                client.ActiveCharacter.OwnedPets.Remove(pet3Guid);
                SendPetRemovedResponse(client, pet3Guid);
                pet3.DeleteLater();
                SendPetSyntesResultResponse(client, PetSynethisResult.Ok, resultPet, pet1.Guid, pet2.Guid, pet3.Guid);
                SendInitPetInfoOnLoginResponse(client, resultPet);
            }
            else
            {
                SendPetSyntesResultResponse(client, PetSynethisResult.AbnormalPetInfo);
                return;
            }
        }
        public static void SendPetSyntesResultResponse(IRealmClient client, PetSynethisResult result, Asda2PetRecord resultPet = null,  int removedPet1Guid = 0, int removedPet2Guid = 0, int removedPet3Guid = 0)
        {
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetSyntesResult))//6113
            {
                packet.WriteByte((byte)result);//{result}default value : 1 Len : 1
                packet.WriteInt32(client.ActiveCharacter.AccId);//{accId}default value : 361343 Len : 4
                packet.WriteInt32(resultPet == null ? 0 : resultPet.Guid);//{resultPetGuid}default value : 69819 Len : 4
                packet.WriteInt16(resultPet == null ? 0 : resultPet.Id);//{resultPetId}default value : 2118 Len : 2
                packet.WriteFixedAsciiString(resultPet == null ? "" : resultPet.Name, 16);//{resultPetName}default value :  Len : 16
                packet.WriteByte(resultPet == null ? 0 : resultPet.HungerPrc);//{resultPetHunger}default value : 100 Len : 1
                packet.WriteByte(resultPet == null ? 0 : resultPet.Level);//{rpCurLvl}default value : 1 Len : 1
                packet.WriteByte(resultPet == null ? 0 : resultPet.MaxLevel);//{rpMaxLvl}default value : 3 Len : 1
                packet.WriteSkip(unk13);//value name : unk13 default value : unk13Len : 3
                packet.WriteInt16((byte)(resultPet == null ? 0 : resultPet.Stat1Type));//{rpStat1Type}default value : 16 Len : 2
                packet.WriteInt32(resultPet == null ? 0 : resultPet.Stat1Value);//{rpStat1Val}default value : 32 Len : 4
                packet.WriteInt16((byte)(resultPet == null ? 0 : resultPet.Stat2Type));//{rpStat1Type}default value : 16 Len : 2
                packet.WriteInt32(resultPet == null ? 0 : resultPet.Stat2Value);//{rpStat1Val}default value : 32 Len : 4
                packet.WriteInt16((byte)(resultPet == null ? 0 : resultPet.Stat3Type));//{rpStat1Type}default value : 16 Len : 2
                packet.WriteInt32(resultPet == null ? 0 : resultPet.Stat3Value);//{rpStat1Val}default value : 32 Len : 4
                packet.WriteInt32(removedPet1Guid);//{removedPet1Guid}default value : 68367 Len : 4
                packet.WriteInt32(removedPet2Guid);//{removedPet2Guid}default value : 68412 Len : 4
                packet.WriteInt32(removedPet3Guid);//{removedPet3Guid}default value : 68412 Len : 4
                packet.WriteInt32(client.ActiveCharacter.Asda2Inventory.Weight);//{weight}default value : 10788 Len : 4
                /*for (int i = 0; i < 3; i += 1)
                {
                    var item = items[i];
                    packet.WriteInt32(item == null ? 0 : item.ItemId);//{itemId}default value : 31981 Len : 4
                    packet.WriteByte((byte)(item == null ? 0 : item.InventoryType));//{inv}default value : 2 Len : 1
                    packet.WriteInt16(item == null ? -1 : item.Slot);//{slot}default value : 8 Len : 2
                    packet.WriteInt16(item == null ? -1 : item.IsDeleted ? -1 : 0);//{minusOneIfDelete}default value : -1 Len : 2
                    packet.WriteInt32(item == null ? 0 : item.Amount);//{amount}default value : 0 Len : 4
                    packet.WriteSkip(stab25);//value name : stab25 default value : stab25Len : 1
                    packet.WriteInt16(item == null ? 0 : item.Amount);//{amount0}default value : 0 Len : 2
                    packet.WriteSkip(stab28);//value name : stab28 default value : stab28Len : 41

                }*/
                client.Send(packet);
            }
        }
        static readonly byte[] unk13 = new byte[] { 0x00, 0x00, 0x00 };
        [PacketHandler(RealmServerOpCode.BreakPetLvlLimit)]//6148
        public static void BreakPetLvlLimitRequest(IRealmClient client, RealmPacketIn packet)
        {
            packet.Position -= 9;
            var petGuid = packet.ReadInt32();//default : 68412Len : 4
            var itemId = packet.ReadInt32();//default : 37813Len : 4
            packet.Position -= 2;
            var inv2 = packet.ReadByte();//default : 2Len : 1
            packet.Position -= 3;
            var lvlBreakItemSlot = packet.ReadInt16();//default : 3Len : 2
            //var petLevelBreakAmount = packet.ReadInt32();//default : -1Len : 4
            var inv20 = packet.ReadByte();//default : 0Len : 1
            var lsuplItemSlot = packet.ReadInt16();//default : -1Len : 2
            client.ActiveCharacter.SendErrorMsg("petGuid = " + petGuid);
            client.ActiveCharacter.SendErrorMsg("itemId = " + itemId);
            client.ActiveCharacter.SendErrorMsg("inv2 = " + inv2);
            client.ActiveCharacter.SendErrorMsg("lvlBreakItemSlot = " + lvlBreakItemSlot);
            //client.ActiveCharacter.SendErrorMsg("petLevelBreakAmount = " + petLevelBreakAmount);
            client.ActiveCharacter.SendErrorMsg("inv20 = " + inv20);
            client.ActiveCharacter.SendErrorMsg("lsuplItemSlot = " + lsuplItemSlot);

            if (!client.ActiveCharacter.OwnedPets.ContainsKey(petGuid))
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet lvl limit not existing pet.", 20);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.AbnormalPetInfo);
                return;
            }
            var item = client.ActiveCharacter.Asda2Inventory.GetRegularItem(lvlBreakItemSlot);
            if (item == null)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet lvl limit with not existint break lvl item.", 10);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.PetLimitPotionInfoAbnormal);
                return;
            }
            if (item.Category != Asda2ItemCategory.PetLevelBreakPotion)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to resurect pet with not a resurect item.", 50);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.PetLimitPotionInfoAbnormal);
                return;
            }
            var pet = client.ActiveCharacter.OwnedPets[petGuid];
            if (!pet.IsMaxExpirience)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet level limit with not 100prc exp.", 50);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.Not100PrcExp);
                return;
            }
            if (pet.Level != pet.MaxLevel)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet level limit with not maxed level.", 50);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.TheLevelOfLimitBreakItemIsTooLow);
                return;
            }
            if (pet.Level >= 10)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet level limit more than 10 lvl.", 50);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.MaximumBreakLvlLimitReached);
                return;
            }
            var success = CharacterFormulas.CalcPetLevelBreakSuccess();
            var suplItem = client.ActiveCharacter.Asda2Inventory.GetShopShopItem(lsuplItemSlot);
            if (suplItem != null && suplItem.Category != Asda2ItemCategory.PetLevelProtection)
            {
                client.ActiveCharacter.YouAreFuckingCheater("Trying to break pet level limit with incorect category supl.", 50);
                SendPetLevelLimitBreakedResponse(client, PetLimitBreakStatus.MaximumBreakLvlLimitReached);
                return;
            }
            if (success)
            {
                pet.MaxLevel++;
                pet.Level++;
            }
            else
                pet.RemovePrcExp(suplItem == null ? 50 : 10);
            if (suplItem != null)
                suplItem.ModAmount(-1);
            item.ModAmount(-1);
            SendPetLevelLimitBreakedResponse(client,
                                             success ? PetLimitBreakStatus.Ok : PetLimitBreakStatus.FailedRedusedBy50,
                                             pet, item, suplItem);
        }

        public static void SendPetLevelLimitBreakedResponse(IRealmClient client, PetLimitBreakStatus status, Asda2PetRecord pet = null, Asda2Item lvlBreakItem = null, Asda2Item suplItem = null)
        {
            var items = new Asda2Item[2];
            items[0] = lvlBreakItem;
            items[1] = suplItem;
            using (var packet = new RealmPacketOut(RealmServerOpCode.PetLevelLimitBreaked))//6149
            {
                packet.WriteByte((byte)status);//{curstatus}default value : 1 Len : 1
                packet.WriteInt32(pet == null ? 0 : pet.Guid);//{petGuid}default value : 68412 Len : 4
                packet.WriteInt16(pet == null ? 0 : pet.Id);//{petId}default value : 2759 Len : 2
                packet.WriteFixedAsciiString(pet == null ? "" : pet.Name, 16);//{petName}default value :  Len : 16
                packet.WriteByte(pet == null ? 0 : pet.HungerPrc);//{hunger}default value : 97 Len : 1
                packet.WriteByte(pet == null ? 0 : pet.Level);//{curLvl}default value : 5 Len : 1
                packet.WriteByte(pet == null ? 0 : pet.MaxLevel);//{maxLvl}default value : 5 Len : 1
                packet.WriteInt16(pet == null ? 0 : pet.Expirience);//{exp}default value : 2000 Len : 2
                packet.WriteByte(1);//value name : unk13 default value : 1Len : 1
                packet.WriteInt16((short)(pet == null ? 0 : pet.Stat1Type));//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet == null ? 0 : pet.Stat1Value);//{stat1Value}default value : 77 Len : 4
                packet.WriteInt16((short)(pet == null ? 0 : pet.Stat2Type));//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet == null ? 0 : pet.Stat2Value);//{stat1Value}default value : 77 Len : 4
                packet.WriteInt16((short)(pet == null ? 0 : pet.Stat3Type));//{stat1Type}default value : 2 Len : 2
                packet.WriteInt32(pet == null ? 0 : pet.Stat3Value);//{stat1Value}default value : 77 Len : 4
                packet.WriteInt32(client.ActiveCharacter.Asda2Inventory.Weight);//{inwWeight}default value : 9988 Len : 4
                for (int i = 0; i < 2; i += 1)
                {
                    var item = items[i];
                    packet.WriteInt32(item == null ? 0 : item.ItemId);//{itemId}default value : 31981 Len : 4
                    packet.WriteByte((byte)(item == null ? 0 : item.InventoryType));//{inv}default value : 2 Len : 1
                    packet.WriteInt16(item == null ? -1 : item.Slot);//{slot}default value : 8 Len : 2
                    packet.WriteInt16(item == null ? -1 : item.IsDeleted ? -1 : 0);//{minusOneIfDelete}default value : -1 Len : 2
                    packet.WriteInt32(item == null ? 0 : item.Amount);//{amount}default value : 0 Len : 4
                    packet.WriteSkip(stab25);//value name : stab25 default value : stab25Len : 1
                    packet.WriteInt16(item == null ? 0 : item.Amount);//{amount0}default value : 0 Len : 2
                    packet.WriteSkip(stab28);//value name : stab28 default value : stab28Len : 41

                } client.Send(packet);
            }
        }
        static readonly byte[] stab25 = new byte[] { 0x00 };
        static readonly byte[] stab28 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };


    }
    public enum PetSynethisResult
    {
        Ok = 1,
        AvalibleOnlyFor40LvlAndHigher = 2,
        AbnormalPetInfo = 3,
        LowPetLevel = 4,
        CantUseCurrentlySummonedPet = 5,
        SuplimentInfoAbnormal = 6,
        NotEnoghtSystSupl = 7,
        IncorrectSuplimentLevel = 8,
        RandomSuplientError = 9,
        NotEnoghtRandowmSupliment = 10,

    }
    public enum PetLimitBreakStatus
    {
        FailedRedusedBy50 = 0,
        Ok = 1,
        AbnormalPetInfo = 2,
        YouCantUseSummonedPet = 3,
        Not100PrcExp = 4,
        MaximumBreakLvlLimitReached = 5,
        PetLimitPotionInfoAbnormal = 6,
        NotEnoghtLimitBreakPotions = 7,
        TheLevelOfLimitBreakItemIsTooLow = 8,
        SuplimentInfoAbnormal = 9,

    }
    public enum HatchEggStatus
    {
        Fail = 0,
        Ok = 1,
        YouAreNoLongerAllowedToUsePet = 2,
        InqubatorItemError = 3,
        NoEgg = 4,
        SuplimentError = 5,
        HatchingProbablilityFailed = 7,
        PetHatchingFailed = 9,
        LowLevel = 10,
    }

    public enum Asda2PetNamehangeResult
    {
        Ok = 1,
        BadWordsInName = 2,
        YouMustHavePremiumItemToChangePetName = 3,
        AbnormalPetInfo = 4,
        FunctionalItemDoesNotExist = 5,


    }
    public enum Asda2PetStatType
    {
        None = 0,
        Strength = 1,
        Stamina = 2,
        Intellect = 3,
        Energy = 4,
        Dexterity = 5,
        Luck = 6,
        AllCapabilities = 7,
        MinAtack = 8,
        MaxAtack = 9,
        MinMaxAtack = 10,
        MinMagicAtack = 11,
        MaxMagicAtack = 12,
        MinMaxMagicAtack = 13,
        MagicDeffence = 14,
        MinDeffence = 15,
        MaxDeffence = 16,
        MinMaxDeffence = 17,
        DodgePrc = 18,
        CriticalPrc = 19,
        ItemSellingPricePrc = 20,
        StrengthPrc = 21,
        StaminaPrc = 22,
        IntellectPrc = 23,
        EnergyPrc = 24,
        DexterityPrc = 25,
        LuckPrc = 26,
        AllCapabilitiesPrc = 27,
        MinAtackPrc = 28,
        MaxAtackPrc = 29,
        MinMaxAtackPrc = 30,
        MinMagicAtackPrc = 31,
        MaxMagicAtackPrc = 32,
        MinMaxMagicAtackPrc = 33,
        MagicDeffencePrc = 34,
        MinDeffencePrc = 35,
        MaxDeffencePrc = 36,
        MinMaxDeffencePrc = 37,



    }
}