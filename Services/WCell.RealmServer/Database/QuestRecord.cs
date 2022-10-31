using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using WCell.Core.Database;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Global;
using System.Linq;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Access = PropertyAccess.Property)]
    public class QuestRecord : WCellRecord<QuestRecord>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(QuestRecord), "QuestRecordId");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>
        public static long NextId()
        {
            return _idGenerator.Next();
        }

        [Field("QuestTemplateId", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _questTemplateId;

        [Field("QuestFileId", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _questFileId;

        [Field("Item1Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id1;

        [Field("Item2Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id2;

        [Field("Item3Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id3;

        [Field("Item4Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id4;

        [Field("Item5Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id5;

        [Field("Monster1", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mon1;

        [Field("Monster2", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mon2;

        [Field("Monster3", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mon3;

        [Field("Monster4", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mon4;

        [Field("Monster5", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mon5;

        [Field("Item1Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount1;

        [Field("Item2Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount2;

        [Field("Item3Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount3;

        [Field("Item4Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount4;

        [Field("Item5Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount5;

        [Field("OwnerId", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _ownerId;

        [Field("CompleteStatus", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _cs;

        [Field("QuestStage", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _qs;

        [Field("QuestType", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _qt;

        public QuestRecord(int qId, int qfID, uint ownerId, int slot, int item1, int item2, int item3, int item4, int item5, int item1a, int item2a, int item3a, int item4a, int item5a, int mon1, int mon2, int mon3, int mon4, int mon5, int qt, int cs)
        {
            QuestTemplateId = qId;
            QuestFileId = qfID;
            _ownerId = ownerId;
            Slot = slot;
            Item1Id = item1;
            Item2Id = item2;
            Item3Id = item3;
            Item4Id = item4;
            Item5Id = item5;
            Item1Amount = item1a;
            Item2Amount = item2a;
            Item3Amount = item3a;
            Item4Amount = item4a;
            Item5Amount = item5a;
            CompleteStatus = cs;
            QuestStage = 2;
            Monster1 = mon1;
            Monster2 = mon2;
            Monster3 = mon3;
            Monster4 = mon4;
            Monster5 = mon5;
            QuestType = qt;
            QuestRecordId = NextId();
        }

        public QuestRecord()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long QuestRecordId
        {
            get;
            set;
        }

        public uint OwnerId
        {
            get { return (uint)_ownerId; }
            set { _ownerId = value; }
        }

        public int QuestTemplateId
        {
            get { return _questTemplateId; }
            set { _questTemplateId = value; }
        }

        public int QuestFileId
        {
            get { return _questFileId; }
            set { _questFileId = value; }
        }

        [Property(NotNull = true)]
        public int Slot
        {
            get;
            set;
        }

        public int Item1Id
        {
            get { return _id1; }
            set { _id1 = value; }
        }
        public int Item1Amount
        {
            get { return _amount1; }
            set { _amount1 = value; }
        }
        public int Item2Id
        {
            get { return _id2; }
            set { _id2 = value; }
        }
        public int Item2Amount
        {
            get { return _amount2; }
            set { _amount2 = value; }
        }
        public int Item3Id
        {
            get { return _id3; }
            set { _id3 = value; }
        }
        public int Item3Amount
        {
            get { return _amount3; }
            set { _amount3 = value; }
        }
        public int Item4Id
        {
            get { return _id4; }
            set { _id4 = value; }
        }
        public int Item4Amount
        {
            get { return _amount4; }
            set { _amount4 = value; }
        }
        public int Item5Id
        {
            get { return _id5; }
            set { _id5 = value; }
        }
        public int Item5Amount
        {
            get { return _amount5; }
            set { _amount5 = value; }
        }

        public int CompleteStatus
        {
            get { return _cs; }
            set { _cs = value; }
        }

        public int QuestStage
        {
            get { return _qs; }
            set { _qs = value; }
        }

        public int Monster1
        {
            get { return _mon1; }
            set { _mon1 = value; }
        }

        public int Monster2
        {
            get { return _mon2; }
            set { _mon2 = value; }
        }

        public int Monster3
        {
            get { return _mon3; }
            set { _mon3 = value; }
        }

        public int Monster4
        {
            get { return _mon4; }
            set { _mon4 = value; }
        }

        public int Monster5
        {
            get { return _mon5; }
            set { _mon5 = value; }
        }

        public int QuestType
        {
            get { return _qt; }
            set { _qt = value; }
        }

        public static QuestRecord[] GetQuestRecordForCharacter(uint chrId)
        {
            return FindAllByProperty("_ownerId", (long)chrId);
        }

        public static QuestRecord GetQuestRecordBySlot(int slot)
        {
            return Find(Restrictions.Eq("Slot", slot));
        }

        public static QuestRecord GetQuestRecord(uint owner, int questId)
        {
            var chrQuests = GetQuestRecordForCharacter(owner);
            var RecId = chrQuests.FirstOrDefault(qi => qi.QuestTemplateId == questId);
            return RecId;
        }
    }
}