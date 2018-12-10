using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlServerCe;
using System.IO;

using ABCat.DB.Entity.Catalog;
using ABCat.DB.Entity.PageStorage;
using ABCat.DB.Entity.ProcessingSettings;
using ABCat.DB.Entity.UserData;

namespace ABCat.DB
{
	public class ObjectContextFactory
	{
		#region + Fields +

		private readonly Dictionary<Type, Func<ObjectContext>> _contextList = new Dictionary<Type, Func<ObjectContext>>();
		private readonly Dictionary<Type, Func<DbContext>> _dbContextList = new Dictionary<Type, Func<DbContext>>();

		#endregion
		#region + Properties +

		public string DBPath		{get; set;}

		#endregion
		#region + Ctor +

		public ObjectContextFactory(string dbPath, bool createIfNotExists)
		{
			DBPath = dbPath;
			RegisterCreators(createIfNotExists);
			//ImportPagesFromOld();
			//CompactDB(@"Data Source={0};".F(Path.Combine(DBPath, "Catalog.sdf")));
		}

		//private void ImportPagesFromOld()
		//{
		//	using (var dbContainer = new DBContainer(this, true))
		//	{
		//		using (var sqlConnection = new SqlCeConnection(GetConnectionString(@"D:\Audiobooks\Catalog\Temp\PageStorage.sdf")))
		//		{
		//			var cmd = new SqlCeCommand("SELECT * FROM PageSet");
		//			cmd.Connection = sqlConnection;
		//			sqlConnection.Open();
		//			using (var reader = cmd.ExecuteReader())
		//			{
		//				int z = 0;

		//				while (reader.Read())
		//				{
		//					var key = reader.GetString(0);
		//					var data = reader.GetValue(1);
		//					dbContainer.PageStorageContainer.PageSet.Add(new Page() { Id = key, PageData = (byte[])data });

		//					z++;

		//					if (z % 100 == 0) dbContainer.SaveChanges();
		//				}
		//			}
		//		}
		//	}
		//}

		#endregion
		#region + Logic +

		public T CreateDbContext<T>()
			where T : DbContext
		{
			Func<DbContext> creator;
			if (!_dbContextList.TryGetValue(typeof(T), out creator))
			{
				throw new Exception("Unknown ObjectContext Type: '{0}'".F(typeof(T).Name));
			}

			var result = creator();
			if (!(result is T)) throw new Exception("Incorrect ObjectContext Creator. Object '{0}' not is '{1}'".F(result.GetType().Name, typeof(T).Name));
			return (T)creator();
		}

		public void CreateNewDatabaseIfNotExists(DbContext dbContext)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(dbContext.Database.Connection.DataSource));
			if (!File.Exists(dbContext.Database.Connection.DataSource))
			{
				dbContext.Database.Create();
			}
		}

		public T CreateObjectContext<T>()
			where T : ObjectContext
		{
			Func<ObjectContext> creator;
			if (!_contextList.TryGetValue(typeof(T), out creator))
			{
				throw new Exception("Unknown ObjectContext Type: '{T}'".F(typeof(T).Name));
			}

			var result = creator();
			if (!(result is T)) throw new Exception("Incorrect ObjectContext Creator. Object '{0}' not is '{1}'".F(result.GetType().Name, typeof(T).Name));
			return (T)creator();
		}

		private static void CompactDB(string connectionString)
		{
			using (var e = new SqlCeEngine(connectionString))
			{
				e.Compact(connectionString);
			}
		}

		private string GetConnectionString(string dbPath, string dbName, int maxDbSize = 4000)
		{
			return GetConnectionString(Path.Combine(dbPath, dbName), maxDbSize);
		}

		private string GetConnectionString(string dbFileName, int maxDbSize = 4000)
		{
			return "Data Source={0};Max Database Size={1}".F(dbFileName, maxDbSize);
		}

		private void Register<T>(Func<DbContext> creator, bool createIfNotExists)
		{
			_dbContextList.Add(typeof(T), creator);
			if (createIfNotExists) CreateNewDatabaseIfNotExists(creator());
		}

		private void RegisterCreators(bool createIfNotExists)
		{
			//String providerName = "System.Data.SqlServerCe.4.0";

			//DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);
			//if (factory == null)
			//	throw new Exception("Unable to locate factory for " + providerName);



			Register<CatalogContainer>(() =>
				{
				 var connection = SqlCeProviderFactory.Instance.CreateConnection();
					connection.ConnectionString = @"Data Source={0};".F(Path.Combine(DBPath, "Catalog.sdf"));
					var result = new CatalogContainer(connection);
					return result;
				}, createIfNotExists);

			Register<ProcessingSettingsContainer>(() =>
				{
					var connection = SqlCeProviderFactory.Instance.CreateConnection();
					connection.ConnectionString = @"Data Source={0};".F(Path.Combine(DBPath, "ProcessingSettings.sdf"));
					var result = new ProcessingSettingsContainer(connection);
					return result;
				}, createIfNotExists);

			Register<UserDataContainer>(() =>
				{
					var connection = SqlCeProviderFactory.Instance.CreateConnection();
					connection.ConnectionString = @"Data Source={0};".F(Path.Combine(DBPath, "UserData.sdf"));
					var result = new UserDataContainer(connection);
					return result;
				}, createIfNotExists);

			Register<PageStorageContainer>(() =>
			{
				var connection = SqlCeProviderFactory.Instance.CreateConnection();
				connection.ConnectionString = @"Data Source={0};".F(Path.Combine(DBPath, "PageStorage.sdf"));
				var result = new PageStorageContainer(connection);
				return result;
			}, createIfNotExists);
		}

		#endregion
	}
}