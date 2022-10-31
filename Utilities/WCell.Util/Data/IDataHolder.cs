using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using WCell.Util.Data;

namespace WCell.Util.Data
{
	/// <summary>
	/// Marks a Type to be persistent.
	/// Each implementing class can have an optional static method
	/// <![CDATA[IEnumerable<IDataHolder>GetAllDataHolders()]]>
	/// that is used for caching
	/// </summary>
	public interface IDataHolder
	{
		void FinalizeDataHolder();
	}

	public abstract class DataHolderBase : IDataHolder
	{
		public abstract void FinalizeDataHolder();
	}
    public partial class IQuestTemplate
	{
		private const uint MaxId = 200000;
        
    }

}