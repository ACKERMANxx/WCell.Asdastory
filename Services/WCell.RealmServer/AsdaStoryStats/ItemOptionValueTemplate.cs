using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Core.Initialization;
using WCell.RealmServer.Entities;
using WCell.Util.Data;
using WCell.Util.Variables;

namespace WCell.RealmServer.AsdaStoryStats
{
    [DataHolder]
    public class ItemOptionValueTemplate : IDataHolder
    {
        [NotVariable]
        //public static ItemOptionValueTemplate[] Templates = new ItemOptionValueTemplate[37530];
        public static Dictionary<int, List<ItemOptionValueTemplate>> ItemOptionValueTemplates = new Dictionary<int, List<ItemOptionValueTemplate>>();

        public int Id { get; set; }
        public int Guid { get; set; }
        public int Type { get; set; }
        public int ValueOne { get; set; }
        public int ValueTwo { get; set; }

        public void FinalizeDataHolder()
        {
            if (ItemOptionValueTemplates.ContainsKey(Guid))
            {
                ItemOptionValueTemplates[Guid].Add(this);
            }
            else
            {
                ItemOptionValueTemplates.Add(Guid, new List<ItemOptionValueTemplate>());
                ItemOptionValueTemplates[Guid].Add(this);
            }

        }

    }
}
