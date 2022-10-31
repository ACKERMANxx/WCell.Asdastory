using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "Asda2QuestRewardNpc", Access = PropertyAccess.Property)]
    public class Asda2QuestRewardNpc : WCellRecord<Asda2QuestRewardNpc>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(Asda2QuestRewardNpc), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _id;

        [Field("npcid", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _npcid;

        [Field("questnum", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _questnum;

        [Field("questid", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _questid;

        [Field("questname", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private string _questname;

        public Asda2QuestRewardNpc(long id, int npcId, int questNum, int questId, string questName)
        {
            Id = _id;
            NpcId = npcId;
            QuestNum = questNum;
            QuestId = questId;
            QuestName = questName;
        }

        public Asda2QuestRewardNpc()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int NpcId
        {
            get { return _npcid; }
            set { _npcid = value; }
        }

        public int QuestNum
        {
            get { return _questnum; }
            set { _questnum = value; }
        }

        public int QuestId
        {
            get { return _questid; }
            set { _questid = value; }
        }

        public string QuestName
        {
            get { return _questname; }
            set { _questname = value; }
        }

        public static Asda2QuestRewardNpc GetRecordByID(long Id)
        {
            return FindOne(Restrictions.Eq("npcid", Id));
        }

        public static int GetQuest(int npcId, int questNum)
        {
            var sql = string.Format("SELECT {0} FROM {1} WHERE {2} = {3} AND {4} = {5} LIMIT 1",
                DatabaseUtil.Dialect.QuoteForColumnName("questid"),
                DatabaseUtil.Dialect.QuoteForTableName(typeof(Asda2QuestRewardNpc).Name),
                DatabaseUtil.Dialect.QuoteForColumnName("npcid"),
                npcId,
                DatabaseUtil.Dialect.QuoteForColumnName("questnum"),
                questNum);
            var query = new ScalarQuery<int>(typeof(Asda2QuestRewardNpc), QueryLanguage.Sql, sql);
            return (int)query.Execute();
        }
    }
}