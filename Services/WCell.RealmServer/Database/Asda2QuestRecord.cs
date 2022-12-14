using System;
using Castle.ActiveRecord;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "asda2questrecord", Access = PropertyAccess.Property)]
    public class Asda2QuestRecord : WCellRecord<Asda2QuestRecord>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(Asda2QuestRecord), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _Id;

        [Field("Level", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _Lv;

        [Field("Exp", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _exp;

        [Field("Gold", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _gold;

        [Field("Monster1Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mid1;

        [Field("Monster2Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mid2;

        [Field("Monster3Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mid3;

        [Field("Monster4Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mid4;

        [Field("Monster5Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mid5;

        /*[Field("Monster1Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mamount1;

        [Field("Monster2Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mamount2;

        [Field("Monster3Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mamount3;

        [Field("Monster4Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mamount4;

        [Field("Monster5Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _mamount5;*/

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

        /*[Field("Reward1Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew1;

        [Field("Reward1Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew1a;

        [Field("Reward2Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew2;

        [Field("Reward2Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew2a;

        [Field("Reward3Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew3;

        [Field("Reward3Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew3a;

        [Field("Reward4Id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew4;

        [Field("Reward4Amount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _rew4a;*/

        [Field("QuestName", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private string _qn;

        [Field("Item1ReqAmount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _reqamount1;

        [Field("Item2ReqAmount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _reqamount2;

        [Field("Item3ReqAmount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _reqamount3;

        [Field("Item4ReqAmount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _reqamount4;

        [Field("Item5ReqAmount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _reqamount5;

        [Field("QuestType", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _qt;

        public Asda2QuestRecord(long id, int Lv, int exp, int gold, int monst1, int monst2, int monst3, int monst4, int monst5, int monst1a, int monst2a, int monst3a, int monst4a, int monst5a, int item1, int item2, int item3, int item4, int item5, int item1a, int item2a, int item3a, int item4a, int item5a, int repeat, int rew1, int rew1a, int rew2, int rew2a, int rew3, int rew3a, int rew4, int rew4a)
        {
            Id = id;
            Level = Lv;
            Exp = exp;
            Gold = gold;
            Monster1Id = monst1;
            Monster2Id = monst2;
            Monster3Id = monst3;
            Monster4Id = monst4;
            Monster5Id = monst5;
            Item1Id = item1;
            Item1Amount = item1a;
            Item2Id = item2;
            Item2Amount = item2a;
            Item3Id = item3;
            Item3Amount = item3a;
            Item4Id = item4;
            Item4Amount = item4a;
            Item5Id = item5;
            Item5Amount = item5a;
        }

        public Asda2QuestRecord()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        [Property(NotNull = false)]
        public string QuestName
        {
            get { return _qn; }
            set { _qn = value; }
        }

        [Property(NotNull = false)]
        public int Level
        {
            get { return _Lv; }
            set { _Lv = value; }
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
        public int Monster1Id
        {
            get { return _mid1; }
            set { _mid1 = value; }
        }

        /*[Property(NotNull = false)]
        public int Monster1Amount
        {
            get { return _mamount1; }
            set { _mamount1 = value; }
        }*/

        [Property(NotNull = false)]
        public int Monster2Id
        {
            get { return _mid2; }
            set { _mid2 = value; }
        }

        /*[Property(NotNull = false)]
        public int Monster2Amount
        {
            get { return _mamount2; }
            set { _mamount2 = value; }
        }*/

        [Property(NotNull = false)]
        public int Monster3Id
        {
            get { return _mid3; }
            set { _mid3 = value; }
        }

        /*[Property(NotNull = false)]
        public int Monster3Amount
        {
            get { return _mamount3; }
            set { _mamount3 = value; }
        }*/

        [Property(NotNull = false)]
        public int Monster4Id
        {
            get { return _mid4; }
            set { _mid4 = value; }
        }

        /*[Property(NotNull = false)]
        public int Monster4Amount
        {
            get { return _mamount4; }
            set { _mamount4 = value; }
        }*/

        [Property(NotNull = false)]
        public int Monster5Id
        {
            get { return _mid5; }
            set { _mid5 = value; }
        }

        /*[Property(NotNull = false)]
        public int Monster5Amount
        {
            get { return _mamount5; }
            set { _mamount5 = value; }
        }*/

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

        [Property(NotNull = false)]
        public int Item5Id
        {
            get { return _id5; }
            set { _id5 = value; }
        }

        [Property(NotNull = false)]
        public int Item5Amount
        {
            get { return _amount5; }
            set { _amount5 = value; }
        }

        [Property(NotNull = false)]
        public int Item1ReqAmount
        {
            get { return _reqamount1; }
            set { _reqamount1 = value; }
        }

        [Property(NotNull = false)]
        public int Item2ReqAmount
        {
            get { return _reqamount2; }
            set { _reqamount2 = value; }
        }

        [Property(NotNull = false)]
        public int Item3ReqAmount
        {
            get { return _reqamount3; }
            set { _reqamount3 = value; }
        }

        [Property(NotNull = false)]
        public int Item4ReqAmount
        {
            get { return _reqamount4; }
            set { _reqamount4 = value; }
        }

        [Property(NotNull = false)]
        public int Item5ReqAmount
        {
            get { return _reqamount5; }
            set { _reqamount5 = value; }
        }

        [Property(NotNull = false)]
        public int QuestType
        {
            get { return _qt; }
            set { _qt = value; }
        }

        public bool isCompleted()
        {
            if (Item1Amount >= Item1ReqAmount && Item2Amount >= Item2ReqAmount && Item3Amount >= Item3ReqAmount && Item4Amount >= Item4ReqAmount && Item5Amount >= Item5ReqAmount)
                return true;
            else
                return false;
        }

        public static Asda2QuestRecord GetRecordByID(long id)
        {
            return FindOne(Restrictions.Eq("Id", id));
        }

        public static Asda2QuestRecord GetRecordByMonster1Id(int id)
        {
            return FindOne(Restrictions.Eq("Monster1Id", id));
        }

        public static Asda2QuestRecord GetRecordByMonster2Id(int id)
        {
            return FindOne(Restrictions.Eq("Monster2Id", id));
        }

        public static Asda2QuestRecord GetRecordByMonster3Id(int id)
        {
            return FindOne(Restrictions.Eq("Monster3Id", id));
        }

        public static Asda2QuestRecord GetRecordByMonster4Id(int id)
        {
            return FindOne(Restrictions.Eq("Monster4Id", id));
        }

        public static Asda2QuestRecord GetRecordByMonster5Id(int id)
        {
            return FindOne(Restrictions.Eq("Monster5Id", id));
        }
    }
}