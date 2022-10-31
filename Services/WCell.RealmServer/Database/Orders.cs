using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate.Criterion;
using WCell.Core.Database;

namespace WCell.RealmServer.Database
{
    [ActiveRecord(Table = "Orders", Access = PropertyAccess.Property)]
    public class Orders : WCellRecord<Orders>
    {
        private static readonly NHIdGenerator _idGenerator =
            new NHIdGenerator(typeof(Orders), "Id");

        /// <summary>
        /// Returns the next unique Id for a new Record
        /// </summary>

        [Field("id", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private long _id;

        [Field("itemid", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _item;

        [Field("accountid", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _acc;

        [Field("itemamount", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private int _amount;

        [Field("charactername", NotNull = true, Access = PropertyAccess.FieldCamelcase)]
        private string _name;

        public Orders(int itemid, int accId, int amount, string cname)
        {
            Id = _id;
            ItemId = itemid;
            AccountId = accId;
            Amount = amount;
            CharacterName = cname;
        }

        public Orders()
        {
        }

        [PrimaryKey(PrimaryKeyType.Assigned)]
        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public int ItemId
        {
            get { return _item; }
            set { _item = value; }
        }

        public int AccountId
        {
            get { return _acc; }
            set { _acc = value; }
        }

        public int Amount
        {
            get { return _amount; }
            set { _amount = value; }
        }

        public string CharacterName
        {
            get { return _name; }
            set { _name = value; }
        }

        public static Orders[] GetOrders(int accountid)
        {
            return FindAllByProperty("_acc", accountid);
        }
    }
}