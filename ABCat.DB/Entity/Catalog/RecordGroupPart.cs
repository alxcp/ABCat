using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCat.DB.Entity.Catalog
{
	public partial class RecordGroup
	{
		#region + Fields +

		private HashSet<string> _recordsCache;
		private Queue<string> _waitForParse;

		#endregion
		#region + Properties +

		[NotMapped]
		[Browsable(false)]
		public int LastPageCount		{get; set;}

		[Browsable(false)]
		public HashSet<string> RecordsCache
		{
			get
			{
				return _recordsCache ?? (_recordsCache = new HashSet<string >());
			}
		}

		[Browsable(false)]
		public Queue<string> WaitForParse
		{
			get
			{
				return _waitForParse ?? (_waitForParse = new Queue<string>());
			}
		}

		#endregion
		#region + Logic +

		public override string ToString()
		{
			return Title;
		}

		#endregion
	}
}