using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "OccupyingRecord", Access = PropertyAccess.Property)]
    public class OccupyingRecord : WCellRecord<OccupyingRecord>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(OccupyingRecord), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _id;

        [Field("Faction", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _faction;

        [Field("CrystalsToday", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _crystals;

        [Field("Tax", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _tax;

        [Field("MvpChar", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private string _mvp;

        public OccupyingRecord(long id, int faction, int tax, int crystals, string mvp)
        {
            Id = id;
            Faction = faction;
            Tax = tax;
            CrystalsToday = crystals;
            MvpChar = mvp;
        }

        public OccupyingRecord()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int Faction
        {
            get { return _faction; }
            set { _faction = value; }
        }

        public int CrystalsToday
        {
            get { return _crystals; }
            set { _crystals = value; }
        }

        public int Tax
        {
            get { return _tax; }
            set { _tax = value; }
        }

        public string MvpChar
        {
            get { return _mvp; }
            set { _mvp = value; }
        }

        public static OccupyingRecord findById(long Id)
        {
            return FindOne(Restrictions.Eq("Id", Id));
        }
    }
}