using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCat.DB.Entity.PageStorage
{
	public partial class Page
	{
		public static void AddOrReplace(PageStorageContainer container, string id, byte[] data)
		{
			Page page = container.PageSet.FirstOrDefault(item => item.Id == id);

			if (page == null)
			{
				page = new Page();
				page.Id = id;
				container.PageSet.Add(page);
			}

			page.PageData = data;
			container.SaveChanges();
		}

		public static void AddOrReplace(PageStorageContainer container, string id, string data, bool compress)
		{
			byte[] realData;

			if (compress) realData = Ionic.Zlib.GZipStream.CompressString(data);
			else realData = Encoding.Default.GetBytes(data);

			AddOrReplace(container, id, realData);
		}

		public static string GetString(PageStorageContainer container, string id, bool uncompress)
		{
			string result = null;

			Page page = container.PageSet.FirstOrDefault(item => item.Id == id);

			if (page != null)
			{
				result = page.GetString(uncompress);
			}

			return result;
		}

		public string GetString(bool uncompress)
		{
			if (uncompress) return Ionic.Zlib.GZipStream.UncompressString(PageData);
			return Encoding.Default.GetString(PageData);
		}

		public void SetString(string pageContent, bool compress)
		{
			if (compress) PageData = Ionic.Zlib.GZipStream.CompressString(pageContent);
			else PageData = Encoding.Default.GetBytes(pageContent);
		}
	}
}
