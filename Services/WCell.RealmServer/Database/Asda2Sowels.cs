using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "Asda2Sowels", Access = PropertyAccess.Property)]
    public class Asda2Sowels : WCellRecord<Asda2Sowels>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(Asda2Sowels), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _id;

        [Field("Quality", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _quality;

        [Field("Level", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _level;

        [Field("Type", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _type;

        [Field("Name", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private string _name;

        public Asda2Sowels(long id, int quality, int level, int type, string name)
        {
            Id = _id;
            Quality = quality;
            Level = level;
            Type = type;
            Name = name;
        }

        public Asda2Sowels()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int Quality
        {
            get { return _quality; }
            set { _quality = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public int Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public static int GetSowel(int quality, int level, int type)
        {
            var sql = string.Format("SELECT {0} FROM {1} WHERE {2} = {3} AND {4} = {5} AND {6} = {7} LIMIT 1",
                DatabaseUtil.Dialect.QuoteForColumnName("Id"),
                DatabaseUtil.Dialect.QuoteForTableName(typeof(Asda2Sowels).Name),
                DatabaseUtil.Dialect.QuoteForColumnName("Quality"), quality,
                DatabaseUtil.Dialect.QuoteForColumnName("Level"), level,
                DatabaseUtil.Dialect.QuoteForColumnName("Type"), type);
            var query = new ScalarQuery<int>(typeof(Asda2Sowels), QueryLanguage.Sql, sql);
            return (int)query.Execute();
        }
    }
}