using System.Data.Entity;
using System.Data.Entity.SqlServerCompact;

namespace ABCat.DB.Entity.Catalog
{
	public class MyConfiguration : DbConfiguration
	{
		public MyConfiguration()
		{
			SetProviderServices(
				SqlCeProviderServices.ProviderInvariantName,
				SqlCeProviderServices.Instance);
		}
	}
}