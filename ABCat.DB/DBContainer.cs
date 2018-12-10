using System;

using ABCat.DB.Entity.Catalog;
using ABCat.DB.Entity.PageStorage;
using ABCat.DB.Entity.ProcessingSettings;
using ABCat.DB.Entity.UserData;

namespace ABCat.DB
{
	public class DBContainer : IDisposable
	{
		#region + Fields +

		public readonly ObjectContextFactory ObjectContextFactory;

		private CatalogContainer _catalog;
		private PageStorageContainer _pageStorageContainer;
		private ProcessingSettingsContainer _parserSettingsContainer;
		private UserDataContainer _userData;

		#endregion
		#region + Properties +

		public bool AutoSaveChanges		{get; set;}
		public CatalogContainer Catalog
		{
			get
			{
				return _catalog ?? (_catalog = ObjectContextFactory.CreateDbContext<CatalogContainer>());
			}
		}

		public PageStorageContainer PageStorageContainer
		{
			get
			{
				return _pageStorageContainer ?? (_pageStorageContainer = ObjectContextFactory.CreateDbContext<PageStorageContainer>());
			}
		}

		public ProcessingSettingsContainer ProcessingSettings
		{
			get
			{
				return _parserSettingsContainer ?? (_parserSettingsContainer = ObjectContextFactory.CreateDbContext<ProcessingSettingsContainer>());
			}
		}

		public UserDataContainer UserData
		{
			get
			{
				return _userData ?? (_userData = ObjectContextFactory.CreateDbContext<UserDataContainer>());
			}
		}

		#endregion
		#region + Ctor +

		public DBContainer(ObjectContextFactory objectContextFactory, bool autoSaveChanges)
		{
			ObjectContextFactory = objectContextFactory;
			AutoSaveChanges = autoSaveChanges;
		}

		#endregion
		#region + Logic +

		public void Dispose()
		{
			if (AutoSaveChanges) SaveChanges();
			DisposeContainers();
		}

		public void Refresh(bool forceSaveChanges = true)
		{
			if (forceSaveChanges || AutoSaveChanges) SaveChanges();
			DisposeContainers();
		}

		public void SaveChanges()
		{
			if (_userData != null) _userData.SaveChanges();
			if (_pageStorageContainer != null) _pageStorageContainer.SaveChanges();
			if (_catalog != null) _catalog.SaveChanges();
			if (_parserSettingsContainer != null) _parserSettingsContainer.SaveChanges();
		}

		private void DisposeContainers()
		{
			if (_userData != null)
			{
				var layouts = _userData;
				_userData = null;
				layouts.Dispose();
			}
			if (_pageStorageContainer != null)
			{
				var pageStorage = _pageStorageContainer;
				_pageStorageContainer = null;
				pageStorage.Dispose();
			}
			if (_catalog != null)
			{
				var catalogContainer = _catalog;
				_catalog = null;
				catalogContainer.Dispose();
			}
			if (_parserSettingsContainer != null)
			{
				var parserSettingsContainer = _parserSettingsContainer;
				_parserSettingsContainer = null;
				parserSettingsContainer.Dispose();
			}
		}

		#endregion
	}
}