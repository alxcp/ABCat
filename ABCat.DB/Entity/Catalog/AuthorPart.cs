//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ABCat.DB.Entity.Catalog
//{
//	public partial class Author : IComparable
//	{
//		public static string AuthorDefaultName = "<Не указано>";

//		public override string ToString()
//		{
//			return FullName;
//		}

//		public int CompareTo(object obj)
//		{
//			var other = obj as Author;
//			if (other == null) return -1;
//			return String.Compare(FullName, other.FullName, StringComparison.Ordinal);
//		}

//		public static Author GetAuthorDefault(DBContainer dbContainer)
//		{
//			Author result = dbContainer.Catalog.AuthorSet.FirstOrDefault(author => author.FullName == AuthorDefaultName);

//			if (result == null)
//			{
//				result = dbContainer.Catalog.AuthorSet.Create();
//				result.FullName = AuthorDefaultName;
//				dbContainer.Catalog.AuthorSet.Add(result);
//			}

//			return result;
//		}

//	}
//}
