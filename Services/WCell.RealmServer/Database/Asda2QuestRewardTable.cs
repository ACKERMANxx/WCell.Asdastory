using System;
using Castle.ActiveRecord;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "Asda2QuestRewardTable", Access = PropertyAccess.Property)]
    public class Asda2QuestRewardTable : WCellRecord<Asda2QuestRewardTable>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(Asda2QuestRewardTable), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _questId;

        [Field("Gold", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _gold;

        [Field("Exp", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _exp;

        [Field("Item1Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id1;

        [Field("Item2Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id2;

        [Field("Item3Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id3;

        [Field("Item4Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _id4;

        [Field("Item1Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount1;

        [Field("Item2Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount2;

        [Field("Item3Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount3;

        [Field("Item4Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount4;

        public Asda2QuestRewardTable(int id, int gold, int exp, int item1, int item2, int item3, int item4, int item1a, int item2a, int item3a, int item4a)
        {
            Id = id;
            Gold = gold;
            Exp = exp;
            Item1Id = item1;
            Item1Amount = item1a;
            Item2Id = item2;
            Item2Amount = item2a;
            Item3Id = item3;
            Item3Amount = item3a;
            Item4Id = item4;
            Item4Amount = item4a;
        }

        public Asda2QuestRewardTable()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _questId; }
            set { _questId = value; }
        }

        [Property(NotNull = false)]
        public int Exp
        {
            get { return _exp; }
            set { _exp = value; }
        }

        [Property(NotNull = false)]
        public int Gold
        {
            get { return _gold; }
            set { _gold = value; }
        }

        [Property(NotNull = false)]
        public int Item1Id
        {
            get { return _id1; }
            set { _id1 = value; }
        }

        [Property(NotNull = false)]
        public int Item1Amount
        {
            get { return _amount1; }
            set { _amount1 = value; }
        }

        [Property(NotNull = false)]
        public int Item2Id
        {
            get { return _id2; }
            set { _id2 = value; }
        }

        [Property(NotNull = false)]
        public int Item2Amount
        {
            get { return _amount2; }
            set { _amount2 = value; }
        }

        [Property(NotNull = false)]
        public int Item3Id
        {
            get { return _id3; }
            set { _id3 = value; }
        }

        [Property(NotNull = false)]
        public int Item3Amount
        {
            get { return _amount3; }
            set { _amount3 = value; }
        }

        [Property(NotNull = false)]
        public int Item4Id
        {
            get { return _id4; }
            set { _id4 = value; }
        }

        [Property(NotNull = false)]
        public int Item4Amount
        {
            get { return _amount4; }
            set { _amount4 = value; }
        }

        public static Asda2QuestRewardTable GetRecordByID(long id)
        {
            return FindOne(Restrictions.Eq("Id", id));
        }
    }
}