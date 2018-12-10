using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCat.DB.Entity.Catalog
{
	public partial class Record
	{
		public static string GenreDefaultName = "<Не указано>";
		public static string AuthorDefaultName = "<Не указано>";

		#region + Fields +

		private string _lastParsedLenght;
		private TimeSpan _parsedLenght;

		#endregion
		#region + Properties +

		[NotMapped]
		[Browsable(false)]
		public string AuthorNameForParse { get; set; }
		[NotMapped]
		[Browsable(false)]
		public string AuthorSurnameForParse { get; set; }

		[DisplayName("Длительность (парсинг)")]
		public TimeSpan ParsedLenght
		{
			get
			{
				if (_parsedLenght == TimeSpan.MinValue || !Equals(Lenght, _lastParsedLenght))
				{
					_parsedLenght = Lenght.ToTimeSpan();
					_lastParsedLenght = Lenght;
				}
				return _parsedLenght;
			}
		}

		#endregion
		#region + Logic +

		public void ClearMetaInfo()
		{
			Author = null;
			Bitrate = null;
			Publisher = null;
			Reader = null;
			Description = null;
			Lenght = null;
			Genre = null;
		}

		public string GetPageID()
		{
			return "TP_{0}_{1}".F(RecordGroupID, Key);
		}

		public string GetPageMetaID()
		{
			return "TPM_{0}_{1}".F(RecordGroupID, Key);
		}

		public override string ToString()
		{
			return "[{0}] {1}".F(Key, Title);
		}

		#endregion
	}
}