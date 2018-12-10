//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ABCat.DB.Entity.Catalog
//{
//	public partial class Genre : IComparable
//	{
//		public static string GenreDefaultName = "<Не указано>";
//		public static string AuthorDefaultName = "<Не указано>";
//		public override string ToString()
//		{
//			return Name;
//		}

//		public int CompareTo(object obj)
//		{
//			var other = obj as Genre;
//			if (other == null) return -1;
//			return String.Compare(Name, other.Name, StringComparison.Ordinal);
//		}

//		public static Genre GetGenreDefault(DBContainer dbContainer)
//		{
//			Genre result = dbContainer.Catalog.GenreSet.FirstOrDefault(genre => genre.Name == GenreDefaultName);

//			if (result == null)
//			{
//				result = dbContainer.Catalog.GenreSet.Create();
//				result.Name = GenreDefaultName;
//				dbContainer.Catalog.GenreSet.Add(result);
//			}

//			return result;
//		}
//	}
//}
