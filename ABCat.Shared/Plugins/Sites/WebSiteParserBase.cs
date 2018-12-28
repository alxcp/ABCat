﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.Catalog;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using HtmlAgilityPack;

namespace ABCat.Shared.Plugins.Sites
{
    static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }

    public class Levenshtein
    {


        ///*****************************
        /// Compute Levenshtein distance 
        /// Memory efficient version
        ///*****************************
        public int iLD(String sRow, String sCol)
        {
            int RowLen = sRow.Length;  // length of sRow
            int ColLen = sCol.Length;  // length of sCol
            int RowIdx;                // iterates through sRow
            int ColIdx;                // iterates through sCol
            char Row_i;                // ith character of sRow
            char Col_j;                // jth character of sCol
            int cost;                   // cost

            /// Test string length
            if (Math.Max(sRow.Length, sCol.Length) > Math.Pow(2, 31))
                throw (new Exception("\nMaximum string length in Levenshtein.iLD is " + Math.Pow(2, 31) + ".\nYours is " + Math.Max(sRow.Length, sCol.Length) + "."));

            // Step 1

            if (RowLen == 0)
            {
                return ColLen;
            }

            if (ColLen == 0)
            {
                return RowLen;
            }

            /// Create the two vectors
            int[] v0 = new int[RowLen + 1];
            int[] v1 = new int[RowLen + 1];
            int[] vTmp;



            /// Step 2
            /// Initialize the first vector
            for (RowIdx = 1; RowIdx <= RowLen; RowIdx++)
            {
                v0[RowIdx] = RowIdx;
            }

            // Step 3

            /// Fore each column
            for (ColIdx = 1; ColIdx <= ColLen; ColIdx++)
            {
                /// Set the 0'th element to the column number
                v1[0] = ColIdx;

                Col_j = sCol[ColIdx - 1];


                // Step 4

                /// Fore each row
                for (RowIdx = 1; RowIdx <= RowLen; RowIdx++)
                {
                    Row_i = sRow[RowIdx - 1];


                    // Step 5

                    if (Row_i == Col_j)
                    {
                        cost = 0;
                    }
                    else
                    {
                        cost = 1;
                    }

                    // Step 6

                    /// Find minimum
                    int m_min = v0[RowIdx] + 1;
                    int b = v1[RowIdx - 1] + 1;
                    int c = v0[RowIdx - 1] + cost;

                    if (b < m_min)
                    {
                        m_min = b;
                    }
                    if (c < m_min)
                    {
                        m_min = c;
                    }

                    v1[RowIdx] = m_min;
                }

                /// Swap the vectors
                vTmp = v0;
                v0 = v1;
                v1 = vTmp;

            }


            // Step 7

            /// Value between 0 - 100
            /// 0==perfect match 100==totaly different
            /// 
            /// The vectors where swaped one last time at the end of the last loop,
            /// that is why the result is now in v0 rather than in v1
            System.Console.WriteLine("iDist=" + v0[RowLen]);
            int max = System.Math.Max(RowLen, ColLen);
            return ((100 * v0[RowLen]) / max);
        }





        ///*****************************
        /// Compute the min
        ///*****************************

        private int Minimum(int a, int b, int c)
        {
            int mi = a;

            if (b < mi)
            {
                mi = b;
            }
            if (c < mi)
            {
                mi = c;
            }

            return mi;
        }

        ///*****************************
        /// Compute Levenshtein distance         
        ///*****************************

        public int LD(String sNew, String sOld)
        {
            int[,] matrix;              // matrix
            int sNewLen = sNew.Length;  // length of sNew
            int sOldLen = sOld.Length;  // length of sOld
            int sNewIdx; // iterates through sNew
            int sOldIdx; // iterates through sOld
            char sNew_i; // ith character of sNew
            char sOld_j; // jth character of sOld
            int cost; // cost

            /// Test string length
            if (Math.Max(sNew.Length, sOld.Length) > Math.Pow(2, 31))
                throw (new Exception("\nMaximum string length in Levenshtein.LD is " + Math.Pow(2, 31) + ".\nYours is " + Math.Max(sNew.Length, sOld.Length) + "."));

            // Step 1

            if (sNewLen == 0)
            {
                return sOldLen;
            }

            if (sOldLen == 0)
            {
                return sNewLen;
            }

            matrix = new int[sNewLen + 1, sOldLen + 1];

            // Step 2

            for (sNewIdx = 0; sNewIdx <= sNewLen; sNewIdx++)
            {
                matrix[sNewIdx, 0] = sNewIdx;
            }

            for (sOldIdx = 0; sOldIdx <= sOldLen; sOldIdx++)
            {
                matrix[0, sOldIdx] = sOldIdx;
            }

            // Step 3

            for (sNewIdx = 1; sNewIdx <= sNewLen; sNewIdx++)
            {
                sNew_i = sNew[sNewIdx - 1];

                // Step 4

                for (sOldIdx = 1; sOldIdx <= sOldLen; sOldIdx++)
                {
                    sOld_j = sOld[sOldIdx - 1];

                    // Step 5

                    if (sNew_i == sOld_j)
                    {
                        cost = 0;
                    }
                    else
                    {
                        cost = 1;
                    }

                    // Step 6

                    matrix[sNewIdx, sOldIdx] = Minimum(matrix[sNewIdx - 1, sOldIdx] + 1, matrix[sNewIdx, sOldIdx - 1] + 1, matrix[sNewIdx - 1, sOldIdx - 1] + cost);

                }
            }

            // Step 7

            /// Value between 0 - 100
            /// 0==perfect match 100==totaly different
            int max = System.Math.Max(sNewLen, sOldLen);
            return (100 * matrix[sNewLen, sOldLen]) / max;
        }
    }

    public abstract class WebSiteParserBase : IWebSiteParserPlugin
    {
        public Config Config { get; set; }
        private int _webSiteId = -1;
        private HashSet<string> _groupKeys;

        public int WebSiteId
        {
            get
            {
                if (_webSiteId == -1)
                {
                    var dbContainer = Context.I.DbContainer;
                    var ws = dbContainer.WebSiteSet.GetWebSitesAll()
                        .FirstOrDefault(item => item.WebSiteParserPluginName.Compare(GetType().Name));
                    if (ws == null)
                    {
                        ws = dbContainer.WebSiteSet.CreateWebSite();
                        ws.WebSiteParserPluginName = GetType().Name;
                        dbContainer.WebSiteSet.AddWebSite(ws);
                        ws = dbContainer.WebSiteSet.GetWebSitesAll()
                            .First(item => item.WebSiteParserPluginName.Compare(GetType().Name));
                    }

                    _webSiteId = ws.Id;
                }

                return _webSiteId;
            }
        }

        public HashSet<string> GetGroupKeys(bool forceRefresh)
        {
            if (_groupKeys == null || forceRefresh)
            {
                var webSiteId = WebSiteId;
                var dbContainer = Context.I.DbContainer;
                _groupKeys = dbContainer.AudioBookGroupSet.GetRecordGroupsAll()
                    .Where(item => item.WebSiteId == webSiteId).Select(item => item.Key).ToHashSet();
            }

            return _groupKeys;
        }

        public abstract Uri GetRecordPageUrl(IAudioBook record);
        public abstract string DisplayName { get; }

        public async Task DownloadRecordGroups(HashSet<string> recordGroupsKeys, CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;

                    var groupActualityPeriod = mainConfig.GroupActualityPeriod;
                    var z = 0;
                    IAudioBookGroup[] groups;
                    if (recordGroupsKeys == null) groups = GetAllRecordGroups(dbContainer).ToArray();
                    else
                        groups =
                            GetAllRecordGroups(dbContainer)
                                .Where(item => recordGroupsKeys.Contains(item.Key))
                                .ToArray();

                    foreach (var group in groups.Where(item =>
                        (DateTime.Now - item.LastUpdate).TotalDays > groupActualityPeriod))
                    {
                        try
                        {
                            ProgressMessage.ReportComplex(z, groups.Length, $"{DisplayName}: {z} / {groups.Length}");
                            DownloadRecordGroup(dbContainer, group, cancellationToken);
                            if (cancellationToken.IsCancellationRequested) break;
                            dbContainer.SaveChanges();
                            z++;
                        }
                        catch (Exception ex)
                        {
                            // ignored
                        }
                    }
                }

            }, cancellationToken);
        }

        public async Task DownloadRecords(HashSet<string> recordsKeys, PageSources pageSource,
            CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                var recordActualityPeriod = mainConfig.RecordActualityPeriod;

                using(var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;

                    IAudioBook[] records;

                    if (recordsKeys == null)
                    {
                        if (pageSource == PageSources.CacheOnly)
                        {
                            records = dbContainer.AudioBookSet.GetRecordsByWebSite(WebSiteId).ToArray();
                        }
                        else
                        {
                            records =
                                dbContainer.AudioBookSet.GetRecordsUpdatedBefore(WebSiteId,
                                    DateTime.Now.Subtract(TimeSpan.FromDays(recordActualityPeriod))).ToArray();
                        }
                    }
                    else
                    {
                        records = dbContainer.AudioBookSet.GetRecordsByKeys(recordsKeys).ToArray();
                    }

                    var sw = new Stopwatch();
                    sw.Start();

                    var normalizerPlugin =
                        Context.I.ComponentFactory.GetCreators<INormalizationLogicPlugin>()
                            .First()
                            .GetInstance<INormalizationLogicPlugin>();

                    var waitingForSave = new List<IAudioBook>();

                    for (var z = 0; z < records.Length; z++)
                    {
                        try
                        {
                            var record = records[z];
                            ProgressMessage.ReportComplex(z, records.Length, $"{DisplayName}: {z} / {records.Length}");
                            DownloadRecord(dbContainer, record, pageSource, cancellationToken);
                            record.LastUpdate = DateTime.Now;
                            waitingForSave.Add(record);

                            if (record.Created == default(DateTime))
                            {
                                record.Created = DateTime.Now;
                            }

                            if (cancellationToken.IsCancellationRequested) break;
                            if (sw.Elapsed > TimeSpan.FromSeconds(30) || z == records.Length - 1)
                            {
                                sw.Restart();
                                normalizerPlugin.Normalize(waitingForSave, dbContainer);

                                if (cancellationToken.IsCancellationRequested) break;
                                dbContainer.AudioBookSet.AddChangedRecords(waitingForSave.ToArray());
                                dbContainer.SaveChanges();
                                if (cancellationToken.IsCancellationRequested) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Context.I.MainLog.Error(ex);
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        dbContainer.SaveChanges();
                    }
                    else dbContainer.AutoSaveChanges = false;
                }
            }, cancellationToken);
        }

        private readonly HashSet<string> Genres = new[]
        {
            "Классика",
            "Русская классика",
            "Зарубежная классика",
            "Мифы. Легенды. Эпос",
            "Украинская классика",
            "Античная литература",
            "Классическая историческая литература",
            "Литература 18 века",
            "Литература 19 века",
            "Литература 20 века",
            "Советская литература",
            "Древнерусская литература",
            "Европейская старинная литература",
            "Зарубежная старинная литература",
            "Старинная литература: прочее",
            "Классическая проза",
            "Старинная литература",
            "Современная проза",
            "Современная украинская проза",
            "Современная историческая проза",
            "Современная русская литература",
            "Современная зарубежная литература",
            "Начинающие авторы",
            "Контркультура",
            "Книги о войне",
            "Поэзия, драматургия",
            "Драматургия",
            "Поэзия",
            "Зарубежная драматургия",
            "Зарубежные стихи",
            "Детские книги",
            "Детская проза",
            "Сказки",
            "Детские приключения",
            "Детские стихи",
            "Детская фантастика",
            "Учебная литература",
            "Зарубежные детские книги",
            "Детские детективы",
            "Книги для детей: прочее",
            "Русские сказки",
            "Бизнес-книги",
            "Маркетинг, PR, реклама",
            "Поиск работы, карьера",
            "Управление, подбор персонала",
            "Экономика",
            "Личные финансы",
            "О бизнесе популярно",
            "Отраслевые издания",
            "Зарубежная деловая литература",
            "Корпоративная культура",
            "Бухучет, налогообложение, аудит",
            "Малый бизнес",
            "Недвижимость",
            "Ценные бумаги, инвестиции",
            "Любовные романы",
            "Короткие любовные романы",
            "Эротическая литература",
            "Современные любовные романы",
            "Зарубежные любовные романы",
            "Исторические любовные романы",
            "Любовно-фантастические романы. Любовное фэнтези",
            "Остросюжетные любовные романы",
            "Детективы",
            "Исторические детективы",
            "Современные детективы",
            "Зарубежные детективы",
            "Иронические детективы",
            "Политические детективы",
            "Крутой детектив",
            "Полицейские детективы",
            "Фантастика",
            "Научная фантастика",
            "Попаданцы",
            "Космическая фантастика",
            "Киберпанк",
            "Боевая фантастика",
            "Зарубежная фантастика",
            "Юмористическая фантастика",
            "Героическая фантастика",
            "Детективная фантастика",
            "Социальная фантастика",
            "Фэнтези",
            "Книги про вампиров",
            "Книги про волшебников",
            "Ужасы и мистика",
            "Любовное фэнтези",
            "Боевое фэнтези",
            "Юмористическое фэнтези",
            "Зарубежное фэнтези",
            "Фэнтези про драконов",
            "Историческое фэнтези",
            "Городское фэнтези",
            "Русское фэнтези",
            "Религия",
            "Православие",
            "Христианство",
            "Религия: прочее",
            "Религиоведение",
            "Ислам",
            "Зарубежная эзотерическая и религиозная литература",
            "Эзотерика",
            "Религиозные тексты",
            "Юмор",
            "Анекдоты",
            "Юмористические стихи",
            "Юмористическая проза",
            "Зарубежный юмор",
            "Юмор: прочее",
            "Книги по психологии",
            "Личностный рост",
            "Общая психология",
            "Детская психология",
            "Зарубежная психология",
            "Социальная психология",
            "Секс и семейная психология",
            "Психотерапия и консультирование",
            "Классики психологии",
            "Наука, образование",
            "Техническая литература",
            "Химия",
            "Иностранные языки",
            "Музыка",
            "Педагогика",
            "Математика",
            "Физика",
            "История",
            "Медицина",
            "Учебники и пособия",
            "Философия",
            "Юриспруденция, право",
            "Биология",
            "Зарубежная образовательная литература",
            "Социология",
            "Культурология",
            "Языкознание",
            "Политика, политология",
            "География",
            "Справочники",
            "Путеводители",
            "Руководства",
            "Словари",
            "Зарубежная справочная литература",
            "Энциклопедии",
            "Справочная литература",
            "Публицистика",
            "Биографии и мемуары",
            "Военное дело, спецслужбы",
            "Зарубежная публицистика",
            "Афоризмы и цитаты",
            "Документальная литература",
            "Публицистика: прочее",
            "Приключения",
            "Приключения для детей и подростков",
            "Исторические приключения",
            "Морские приключения",
            "Классическая приключенческая литература",
            "Книги о путешествиях",
            "Зарубежные приключения",
            "Вестерны",
            "Приключения: прочее",
            "Дом, семья",
            "Здоровье",
            "Воспитание детей",
            "Хобби и ремесла",
            "Самосовершенствование",
            "Кулинария",
            "Развлечения",
            "Спорт, фитнес",
            "Зарубежная прикладная и научно-популярная литература",
            "Сад и Огород",
            "Автомобили и ПДД",
            "Домашние животные",
            "Сделай Сам",
            "Природа и животные",
            "Дом и семья: прочее",
            "Целительство",
            "Искусство",
            "Архитектура",
            "Изобразительное искусство, фотография",
            "Кинематограф, театр",
            "Критика",
            "Музыка, балет",
            "Искусство: прочее",
            "Школьная литература",
            "Зарубежная литература",
            "Зарубежные боевики",
            "Зарубежное: прочее",
            "Боевики",
            "Триллеры",
            "Криминальные боевики",
            "Боевики: Прочее",
            "Компьютеры",
            "Зарубежная компьютерная литература",
            "Программы",
            "ОС и Сети",
            "Интернет",
            "Компьютеры: прочее",
            "Повести, рассказы",
            "Рассказы",
            "Повести",
            "Эссе",
            "Очерки",
            "Биография",
            "Лирика",
            "Мифология",
            "Женская проза",
            "Ссср",
            "Демография",
            "Филология",
            "Зоология",
            "Психология",
            "Экология",
            "Истории",
            "Жития",
            "Проза",
            "Роман",
            "Мистика",
            "Исторический Роман",
            "Драма",
            "Ужасы",
            "Зарубежная Проза",
            "Мемуары",
            "Комедия",
            "Радиоспектакль",
            "Героическое фэнтези",
            "Детективный роман",
            "Иронический детектив",
            "Любовный роман",
            "Военная проза",
            "Постапокалипсис",
            "Историческая книга",
            "Сатира",
            "Исторический детектив",
            "Альтернативная история",
            "Бизнес",
            "Историческая проза",
            "Полицейский детектив",
            "Романтика",
            "Аудиоспектакль",
            "Научно-популярная литература",
            "Художественная литература",
            "Приключенческий роман",
            "Историческая литература",
            "LitRPG",
            "Пьеса",
            "Хоррор",
            "Аудиокнига",
            "Русская литература",
            "Стихи",
            "Фантастический боевик",
            "Воспоминания",
            "Фантастика (вселенная warhammer 40000)",
            "Фантастический роман",
            "Военные приключения",
            "Шпионский детектив",
            "Детский детектив",
            "Литературные чтения",
            "Постапокалиптика",
            "Отечественная проза",
            "Исторический",
            "Детективное фэнтези",
            "Современная отечественная проза",
            "Русская классическая проза",
            "Авторский сборник",
            "Современная зарубежная проза",
            "Криминальный детектив",
            "Повседневность",
            "Русская проза",
            "Историко-приключенческий роман",
            "Повесть-сказка",
            "Классический детектив",
            "Путешествия",
            "Жизнь замечательных людей",
            "Познавательная литература",
            "Мистический триллер",
            "Современная литература",
            "Антиутопия",
            "Политология",
            "Новелла",
            "Автобиография",
            "Сказочная повесть",
            "Зарубежный детектив",
            "Магический реализм",
            "Саморазвитие",
            "Фантастическая повесть",
            "Притча",
            "Детская",
            "Остросюжетный роман",
            "Политика",
            "Сюрреализм",
            "Новеллы",
            "Психологический триллер",
            "Детективная повесть",
            "Отечественная классика",
            "Мелодрама",
            "Поэма",
            "Авантюрный детектив",
            "Современный любовный роман",
            "Исторический любовный роман",
            "Историческая повесть",
            "Спектакль",
            "Сборник",
            "Практическая психология",
            "Аудиоспектакли",
            "Литературоведение",
            "Бизнес-литература",
            "Фанфик",
            "Фантастический рассказ",
            "Криминальный роман",
            "Сборник рассказов",
            "Дневники",
            "Зарубежный роман",
            "Историческая",
            "Приключенческий роман в историческом антураже",
            "Реализм",
            "Этнография",
            "Трагедия",
            "Познавательное",
            "Аудиосказка",
            "Любовные детективы",
            "Авантюрный роман",
            "Путешествия и география",
            "Детское фэнтези",
            "Роман-хроника",
            "Зарубежный классический детектив",
            "Советская проза",
            "Документальная повесть",
            "Сентиментальный роман",
            "Дамский детективный роман",
            "Детские сказки",
            "Мистический роман",
            "Современный зарубежный детектив",
            "Общая история",
            "Эпическая фантастика",
            "Мифы",
            "Моноспектакль",
            "Автобиографическая проза",
            "Маркетинг",
            "Трагикомедия",
            "Женские истории",
            "Классика исполнения отечественной прозы",
            "Любовный детектив",
            "Дневник",
            "Литература для детей",
            "Стихотворения",
            "Будущее",
            "Любовно-фантастический роман",
            "Лекция",
            "Приключенческая повесть",
            "Культура",
            "Фантазия",
            "Детская повесть",
            "Пародия",
            "Стимпанк",
            "Классическая библиотека приключений и фантастики",
            "Русская классическая литература",
            "Научно-фантастический роман",
            "Авантюрно-приключенческий роман",
            "Детективы: прочее",
            "Криминал",
            "Притчи",
            "Готический роман",
            "Сказки для детей",
            "Классика зарубежного детектива",
            "Автобиографическая повесть",
            "Романтическая комедия",
            "Психологический детектив",
            "Современный детектив",
            "Школа",
            "Любовная фантастика",
            "Книги для детей",
            "Русские народные сказки",
            "Русская современная проза",
            "Военная литература",
            "Сатира и юмор",
            "Эротические романы",
            "Черный юмор",
            "Роман-эпопея",
            "Остросюжетный любовный роман",
            "Фантастические рассказы",
            "Радиопередача",
            "Басни",
            "Фантасмагория",
            "Рассказы и повести",
            "Роман о любви",
            "Учебник",
            "Психологическая проза",
            "Юмор и сатира",
            "Автобиографический роман",
            "Мюзикл",
            "Космос",
            "Научно-познавательная литература",
            "Медицинский триллер",
            "Современная проза современные любовные романы",
            "Научно-фантастических произведений",
            "Финансы",
            "Чёрный юмор",
            "Фантастический детектив",
            "Лекции",
            "Военный роман",
            "Художественная проза",
            "Мировая классика",
            "Советская классика",
            "Классика детектива",
            "Роман-трилогия",
            "Кругозор",
            "Детское фентези",
            "Интервью",
            "Отечественный женский детектив",
            "Детская энциклопедия",
            "Этти",
            "Ироническая проза",
            "Афоризмы",
            "Приключенческая фантастика",
            "Детективные рассказы",
            "Детская сказка",
            "Историческая драма",
            "Детские рассказы",
            "Юмористические рассказы",
            "Русская и советская литература xx века",
            "Современная русская проза",
            "Наука",
            "Повседневная жизнь",
            "Отечественная история",
            "Разное",
            "Театр у микрофона",
            "Аудиобиблиотека школьника",
            "О войне",
            "Психологическая драма",
            "Рассказы для детей",
            "Биографический роман",
            "Мистический детектив",
            "Управление",
            "Остросюжетная проза",
            "Литературно-музыкальная композиция",
            "Легенды",
            "Военная драма",
            "Научно-популярный",
            "Познавательная радиопередача для детей",
            "Нуар",
            "Радиопостановка",
            "Учебное пособие",
            "Военная история",
            "детская литература",
            "литрпг",
            "историческая фантастика",
            "романтическое фэнтези",
            "музыкальная сказка",
            "магические академии",
            "советская классическая проза",
            "попаданец",
            "политический детектив",
            "женский роман",
            "классические детективы",
            "космоопера",
            "любовно-фантастические романы",
            "жизнеописание",
            "инсценировка",
            "история россии",
            "рассказы о животных",
            "detektiv",
            "научно-фантастический рассказ",
            "фольклор",
            "сплаттерпанк",
            "ретродетектив",
            "семейная сага",
            "эротическое фэнтези",
            "образование",
            "классическая и современная проза",
            "психологическая",
            "исторический и приключенческий роман",
            "современная юмористическая проза",
            "эпос",
            "философский экшн",
            "военное дело",
            "постмодернизм",
            "научно-популярное издание",
            "книги о приключениях",
            "наши дни",
            "криптоистория",
            "спецслужбы",
            "шпионские детективы",
            "письма",
            "романтическое",
            "фентэзи",
            "из книги «я – женщина",
            "любившая мужчину",
            "научно-популярная",
            "личная эффективность",
            "современная отечественная литература",
            "путешествия и приключения",
            "деловая литература",
            "остросюжетный детектив",
            "тренинг",
            "аудиофильм",
            "стихотворения и поэмы",
            "психологическая фантастика",
            "историческое исследование",
            "космическая опера",
            "о животных",
            "интеллектуальный детектив",
            "тайны",
            "военно-историческая фантастика",
            "эпическое фэнтези",
            "оборотни",
            "музыкальные сказки",
            "помоги себе сам",
            "статья",
            "популярная психология",
            "детектив в историческом антураже",
            "хроноопера",
            "классика русской литературы",
            "аудиотренинг",
            "приключения и путешествия",
            "для детей и не только",
            "научно-фантастическая повесть",
            "метафизический реализм",
            "детская познавательная и развивающая литература",
            "зарубежная литература для детей",
            "иностранная классика",
            "детские остросюжетные",
            "сказка для взрослых",

        }.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        public async Task OrganizeKeywords(CancellationToken cancellationToken)
        {
            var mainConfig = Config.Load<MainConfig>();

            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;
                    var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();

                    var sw = new Stopwatch();
                    sw.Start();

                    var allGenres = records.SelectMany(item => item.GetGenres())
    .Where(item => item.Length > 3)
    .GroupBy(item => item.ToLower())
    .ToDictionary(item => item.Key,
        item => Tuple.Create(item.Key.ReplaceAll(new[] { " ", "-", ",", ".", "\"" }, ""), item.Count()));

                    //var topMost = allGenres.Where(item => item.Value.Item2 >= 10 && item.Key.Length > 3).OrderByDescending(item => item.Value)
                    //    .ToDictionary(item => item.Key, item => item.Value);

                    //symbolicDistancePlugin.SetData(topMost.Select(item => Tuple.Create(item.Key, item.Key)).ToList());

                    var sb = new StringBuilder();
                    sb.AppendLine($"AudioBook.Genre\tQuantity\tReferenceGenre\tDistance\tDistance1");
                    var sbNotFound = new StringBuilder();
                    sb.AppendLine("AudioBook.Genre\tQuantity");

                    var l = new Levenshtein();
                    var genres = Genres.ToDictionary(item => item, item => item.ReplaceAll(new[] { " ", "-", ",", "." }, ""), StringComparer.InvariantCultureIgnoreCase);

                    //var g = string.Join("", Genres.Select(item => $"\"{item.ChangeCase(Extensions.CaseTypes.FirstWord, true, true)}\",\r\n"));

                    foreach (var genre in allGenres)
                    {
                        if (!genres.ContainsKey(genre.Key))
                        {
                            List<Tuple<string, int, int>> results = new List<Tuple<string, int, int>>();

                            foreach (var referenceGenre in genres.Where(item => Math.Abs(item.Key.Length - genre.Value.Item1.Length) < 4))
                            {
                                var distance = LevenshteinDistance.Compute(referenceGenre.Value, genre.Value.Item1);
                                var distance1 = l.iLD(referenceGenre.Value, genre.Value.Item1);

                                if (distance <= 2 || distance1 < 10)
                                {
                                    results.Add(Tuple.Create(referenceGenre.Key, distance, distance1));
                                }
                            }

                            if (results.Any())
                            {
                                foreach (var result in results.OrderByDescending(item => item.Item2))
                                {
                                    sb.AppendLine(
                                        $"{genre.Key}\t{genre.Value.Item2}\t{result.Item1}\t{result.Item2}\t{result.Item3}");
                                }
                            }
                            else
                            {
                                sbNotFound.AppendLine($"{genre.Key}\t{genre.Value.Item2}");
                            }
                        }
                    }

                    //var allGenres = records.SelectMany(item => item.GetGenres())
                    //    .Where(item => item.Length > 3)
                    //    .GroupBy(item => item.ToLower())
                    //    .ToDictionary(item => item.Key,
                    //        item => Tuple.Create(item.Key.ReplaceAll(new[] {" ", "-", ",", ".", "\""}, ""), item.Count()));

                    //var topMost = allGenres.Where(item => item.Value.Item2 >= 10 && item.Key.Length > 3).OrderByDescending(item => item.Value)
                    //    .ToDictionary(item => item.Key, item => item.Value);

                    //var sb = new StringBuilder();
                    //sb.AppendLine($"AudioBook.Genre\tQuantity\tReferenceGenre\tDistance\tDistance1");
                    //var sbNotFound = new StringBuilder();
                    //sb.AppendLine("AudioBook.Genre\tQuantity");

                    //var l = new Levenshtein();
                    //var genres = Genres.ToDictionary(item=>item, item => item.ReplaceAll(new[] {" ", "-", ",", "."}, ""), StringComparer.InvariantCultureIgnoreCase);

                    ////var g = string.Join("", Genres.Select(item => $"\"{item.ChangeCase(Extensions.CaseTypes.FirstWord, true, true)}\",\r\n"));

                    //foreach (var audioBook in records)
                    //{
                    //    var audioBookGenres = audioBook.GetGenres();

                    //    foreach (var audioBookGenre in audioBookGenres)
                    //    {
                    //        if (!genres.ContainsKey(audioBookGenre))
                    //        {
                    //            List<Tuple<string, int, int>> results = new List<Tuple<string, int, int>>();

                    //            foreach (var referenceGenre in genres.Where(item =>
                    //                Math.Abs(item.Key.Length - genre.Value.Item1.Length) < 4))
                    //            {
                    //                var distance = LevenshteinDistance.Compute(referenceGenre.Value, genre.Value.Item1);
                    //                var distance1 = l.iLD(referenceGenre.Value, genre.Value.Item1);

                    //                if (distance <= 2 || distance1 < 10)
                    //                {
                    //                    results.Add(Tuple.Create(referenceGenre.Key, distance, distance1));
                    //                }
                    //            }

                    //            if (results.Any())
                    //            {
                    //                foreach (var result in results.OrderByDescending(item => item.Item2))
                    //                {
                    //                    sb.AppendLine(
                    //                        $"{genre.Key}\t{genre.Value.Item2}\t{result.Item1}\t{result.Item2}\t{result.Item3}");
                    //                }
                    //            }
                    //            else
                    //            {
                    //                sbNotFound.AppendLine($"{genre.Key}\t{genre.Value.Item2}");
                    //            }
                    //        }
                    //    }
                    //}

                    var s = sb.ToString() + sbNotFound.ToString();
                    var s1 = s;

                    //var waitingForSave = new List<IAudioBook>();

                    //for (var z = 0; z < records.Count; z++)
                    //{
                    //    try
                    //    {
                    //        var record = records[z];
                    //        totalProgressCallback(z, records.Count, "{0} из {1}".F(z, records.Count()));
                    //        DownloadRecord(dbContainer, record, pageSource, smallProgressCallback,
                    //            cancellationToken);
                    //        record.LastUpdate = DateTime.Now;
                    //        waitingForSave.Add(record);

                    //        if (record.Created == default(DateTime))
                    //        {
                    //            record.Created = DateTime.Now;
                    //        }

                    //        if (cancellationToken.IsCancellationRequested) break;
                    //        if (sw.Elapsed > TimeSpan.FromSeconds(30) || z == records.Count - 1)
                    //        {
                    //            sw.Restart();
                    //            normalizerPlugin.Normalize(waitingForSave, dbContainer);

                    //            if (cancellationToken.IsCancellationRequested) break;
                    //            dbContainer.AudioBookSet.AddChangedRecords(waitingForSave.ToArray());
                    //            dbContainer.SaveChanges();
                    //            if (cancellationToken.IsCancellationRequested) break;
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Context.I.Logger.Error(ex);
                    //    }
                    //}

                    //if (!cancellationToken.IsCancellationRequested)
                    //{
                    //    dbContainer.SaveChanges();
                    //}
                    //else dbContainer.AutoSaveChanges = false;
                }
            }, cancellationToken);
        }

        protected virtual long GetSizeInBytes(string sizeString)
        {
            long result = 0;
            var subSize = sizeString.Split(' ');
            subSize[0] = subSize[0].Replace(',', '.');

            if (subSize.Length >= 2 && float.TryParse(subSize[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out var size))
            {
                int multiplier = 0;

                switch (subSize[1])
                {
                    case "Гб":
                    case "GB":
                        multiplier = 1024 * 1024 * 1024;
                        break;
                    case "Мб":
                    case "MB":
                        multiplier = 1024 * 1024;
                        break;
                    case "Кб":
                    case "KB":
                        multiplier = 1024;
                        break;
                    default:
                        var t = 0;
                        break;
                }

                result = (long)(size * multiplier);
            }

            return result;
        }

        public async Task<string> DownloadRecordSourcePage(IAudioBook audioBook, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() => GetRecordSourcePageString(audioBook, PageSources.CacheOrWeb, cancellationToken), cancellationToken);
        }

        public abstract bool CheckForConfig(bool correct, out Config incorrectConfigs);
        protected abstract string[] RecordPageJunkIdList { get; }

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        public abstract IEnumerable<IAudioBookGroup> GetAllRecordGroups(IDbContainer container);
        private static readonly Regex HtmlSpecCharRegex = new Regex("&#[0-9]+;");
        private static readonly Regex HtmlEscCharRegex = new Regex("&[A-Za-z]+;");

        protected virtual string CleanupRecordValue(string value, bool allowMultiLine, int maxLength)
        {
            var result = allowMultiLine ? value : value.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)[0];

            do
            {
                value = result;
                result = value.Trim().Trim('\r', '\n', ':', '-', '=');
            } while (result != value);

            if (maxLength > 0 && result.Length > maxLength)
            {
                result = result.Substring(0, maxLength - 1) + "…";
            }

            result = HtmlSpecCharRegex.Replace(result, HtmlSpecCharRegexReplacer);
            result = HtmlEscCharRegex.Replace(result, HtmlSpecCharRegexReplacer);

            return result;
        }

        protected virtual string CleanupHumanNameValue(string value, int maxLength)
        {
            return CleanupRecordValue(value, false, maxLength).TrimStart('©', '…', '—', ' ', '๖', 'ۣ', 'ۜ')
                .ChangeCase(Extensions.CaseTypes.AllWords, true, true);
        }

        static string HtmlSpecCharRegexReplacer(Match m)
        {
            return WebUtility.HtmlDecode(m.Value);
        }

        /// <summary>
        ///     Очистить страницу описания торрента от лишних элементов (заголовок, меню, скрипты и др.)
        /// </summary>
        /// <param name="document">Документ</param>
        protected virtual void CleanupRecordPage(HtmlDocument document)
        {
            foreach (var script in document.GetNodes("script", "type", str => true).ToArray())
            {
                script.ParentNode.RemoveChild(script);
            }

            document.RemoveNodesByIds(RecordPageJunkIdList);
        }

        protected abstract void DownloadRecord(IDbContainer db, IAudioBook record, PageSources pageSource, CancellationToken cancellationToken);

        protected abstract void DownloadRecordGroup(IDbContainer db, IAudioBookGroup recordGroup, CancellationToken cancellationToken);

        protected abstract string GetRecordSourcePageString(IAudioBook audioBook, PageSources pageSource,
            CancellationToken cancellationToken);

        protected abstract void ParseRecord(IDbContainer db, IAudioBook record, string postBodyHtml);


        //private static class KeysCollection
        //{
        //    private static Stopwatch _sw = new Stopwatch();
        //    private static readonly Dictionary<string, HashSet<string>> _values = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);

        //    public static void Add(string key, string value)
        //    {
        //        if (!_values.TryGetValue(key, out var values))
        //        {
        //            values = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        //            _values.Add(key, values);
        //        }

        //        values.Add(value);

        //        if (!_sw.IsRunning)
        //            _sw.Start();

        //        if (_sw.Elapsed.TotalSeconds > 15)
        //        {
        //            Debug.WriteLine("===============================");
        //            var top50 = _values.Where(item => item.Value.Count > 10);
        //            foreach (var keyValuePair in top50.OrderByDescending(item=>item.Value.Count))
        //            {
        //                Debug.WriteLine($"{keyValuePair.Key}\t{keyValuePair.Value.Count}");
        //            }

        //            Debug.WriteLine("===============================");
        //            _sw.Restart();
        //        }
        //    }
        //}

        protected virtual void FillRecordElement(IAudioBook record, string key, string value)
        {
            key = key.Trim(' ', '[', ']', '<', '>', ';', '\n', '\t', '.', ',', '"', '(');

            //#if DEBUG
            //            KeysCollection.Add(key, value);
            //#endif

            switch (key.ToLower())
            {
                case "автор":
                case "авторы":
                case "фамилия, имя автора":
                case "фамилии авторов":
                case "авторы произведений":
                    record.Author = CleanupHumanNameValue(value, 500);
                    break;
                case "фамилия автора":
                    record.AuthorSurnameForParse = CleanupHumanNameValue(value, 250);
                    break;
                case "имя автора":
                    record.AuthorNameForParse = CleanupHumanNameValue(value, 250);
                    break;
                case "издательство":
                    record.Publisher =
                        CleanupRecordValue(value, false, 500).ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "исполнитель":
                case "исполнители":
                case "запись и обработка":
                    record.Reader = CleanupHumanNameValue(value, 500);
                    break;
                case "жанр":
                    record.Genre =
                        CleanupRecordValue(value, false, 200)
                            .ChangeCase(Extensions.CaseTypes.AfterSplitter, false, true);
                    break;
                case "битрейт":
                case "битрейт аудио":
                    record.Bitrate = CleanupRecordValue(value, false, 100)
                        .ChangeCase(Extensions.CaseTypes.FirstWord, true, true);
                    break;
                case "длительность":
                case "прдолжительность":
                case "продолжительность":
                case "продолжительность звучания":
                case "продолжительность книги":
                case "общее время звучания":
                case "общее время воспроизведения":
                case "bремя звучания":
                case "время звучания":
                case "время воспроизведения":
                case "время чтения":
                case "общая продолжительность":
                case "продолжительность аудиокниги":
                case "продолжительность (время звучания)":
                case "время":
                case "длина записи":
                    record.Length = CleanupRecordValue(value, false, 500);
                    break;
                case "описание":
                case "аннотация":
                    record.Description = CleanupRecordValue(value, true, 1000);
                    break;
                case "доп. информация":
                    if (ParseKeyValue(value, out var secondKey, out var secondValue))
                    {
                        FillRecordElement(record, secondKey, secondValue);
                    }

                    break;
            }
        }

        protected bool ParseKeyValue(string keyValue, out string key, out string value)
        {
            var result = false;

            key = null;
            value = null;

            if (!keyValue.IsNullOrEmpty())
            {
                var iofColon = keyValue.IndexOf(':');
                if (iofColon > 0 && iofColon < keyValue.Length - 1)
                {
                    key = keyValue.Substring(0, iofColon).ToLower();
                    var iofSemiColon = key.LastIndexOf(';');
                    if (iofSemiColon > 0)
                    {
                        key = key.Substring(iofSemiColon + 1, key.Length - iofSemiColon - 1);
                    }

                    key = key.TrimStart('-', '\n', '.').Trim(' ');

                    if (key != string.Empty)
                    {
                        value = keyValue.Substring(iofColon + 1, keyValue.Length - iofColon - 1);
                        result = true;
                    }
                }
            }

            return result;
        }
    }
}