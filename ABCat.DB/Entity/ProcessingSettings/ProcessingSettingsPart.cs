using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABCat.DB.Entity.Catalog;

namespace ABCat.DB.Entity.ProcessingSettings
{
	[DbConfigurationType(typeof(MyConfiguration))]
	public partial class ProcessingSettingsContainer
	{
		public ProcessingSettingsContainer(DbConnection connection)
			: base(connection, true)
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
		}
	}

}
