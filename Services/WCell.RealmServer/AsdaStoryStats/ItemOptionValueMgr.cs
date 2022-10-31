
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Core.Initialization;
using WCell.RealmServer.Content;

namespace WCell.RealmServer.AsdaStoryStats
{
    class ItemOptionValueMgr
    {

    [Initialization(InitializationPass.Tenth, Name = "Asda Story item Options.")]
    public static void Init()
    {
        ContentMgr.Load<ItemOptionValueTemplate>();
        
    }

    }

}
