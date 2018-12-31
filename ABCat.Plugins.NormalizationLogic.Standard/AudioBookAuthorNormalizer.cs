﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.Catalog.Normalizing;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.NormalizationLogic.Standard
{
    [SingletoneComponentInfo("2.2", IsEnabled = false)]
    [UsedImplicitly]
    public class AudioBookAuthorNormalizer : IRecordsTagNormalizer
    {
        public async Task Normalize(IReadOnlyCollection<string> recordKeys, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                using (var autoSave = Context.I.DbContainerAutoSave)
                {
                    var dbContainer = autoSave.DBContainer;
                    var records = dbContainer.AudioBookSet.GetRecordsAllWithCache();

                    var sw = new Stopwatch();
                    sw.Start();

                    var genres = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

                    // Case SENSITIVE!!! As Designed.
                    var referenceGenres = new HashSet<string>();
                    var genreParts = new List<GenrePart>();

                    foreach (var genre in _referenceGenres)
                    {
                        string referenceGenre = genre;
                        var iofReference = genre.IndexOf('|');
                        if (iofReference > 0)
                            referenceGenre = genre.Substring(0, iofReference);

                        referenceGenres.Add(referenceGenre);

                        var genreVariants = genre.ToLower().ReplaceAll("", " ", "-", ".").Split('|');
                        foreach (var genreVariant in genreVariants)
                        {
                            if (GenrePart.TryParse(genreVariant, referenceGenre, out var genrePart))
                            {
                                genreParts.Add(genrePart);
                            }
                            else
                            {
                                genres[genreVariant] = referenceGenre;
                            }
                        }
                    }

                    int abQuantity = 0;

                    foreach (var audioBook in records)
                    {
                        var audioBookGenres = audioBook.GetGenres();
                        var genresNormalized = new List<string>();

                        foreach (var audioBookGenre in audioBookGenres.Where(item => item.Length > 3))
                        {
                            if (referenceGenres.Contains(audioBookGenre))
                            {
                                genresNormalized.Add(audioBookGenre);
                            }
                            else
                            {
                                var genreForCompare = audioBookGenre.ToLower().ReplaceAll("", " ", "-", ",", ".");

                                var results = new List<Tuple<string, int, string>>();
                                if (genres.TryGetValue(genreForCompare, out var referenceGenre))
                                {
                                    results.Add(Tuple.Create(referenceGenre, 0, "Variant"));
                                }
                                else
                                {
                                    foreach (var genreVariant in genres.Where(item =>
                                        Math.Abs(item.Key.Length - genreForCompare.Length) < 3))
                                    {
                                        var distance = LevenshteinDistance.Compute(genreVariant.Key, genreForCompare);

                                        if (distance <= 2)
                                        {
                                            results.Add(Tuple.Create(genreVariant.Value, distance,
                                                $"Distance to {genreVariant.Key}"));
                                        }
                                    }
                                }

                                if (results.Any())
                                {
                                    var candidate = results.OrderBy(item => item.Item2).First();
                                    genresNormalized.Add(candidate.Item1);
                                }
                                else
                                {
                                    var fitParts = genreParts.Where(gp => gp.IsFit(audioBookGenre))
                                        .OrderBy(item => item.Priority).ToArray();

                                    if (fitParts.Any())
                                    {
                                        var minPriority = fitParts.Min(item => item.Priority);
                                        var res = minPriority < int.MaxValue
                                            ? fitParts.Where(item => item.Priority == minPriority).ToArray()
                                            : fitParts;

                                        genresNormalized.Add(audioBookGenre);

                                        foreach (var genrePart in res)
                                        {
                                            genresNormalized.Add(genrePart.ReferenceGenre);
                                        }
                                    }
                                }
                            }
                        }

                        audioBook.Genre = string.Join(", ", genresNormalized.OrderBy(item => item));
                        dbContainer.AudioBookSet.AddChangedRecords(audioBook);
                        cancellationToken.ThrowIfCancellationRequested();

                        ProgressMessage.Report(abQuantity++, records.Count);

                        if (sw.Elapsed > SaveTimer)
                        {
                            dbContainer.SaveChanges();
                            sw.Restart();
                        }
                    }
                }
            }, cancellationToken);
        }

        private static readonly TimeSpan SaveTimer = TimeSpan.FromSeconds(5);

        private class GenrePart
        {
            private const string StartString = "contains[";
            public string ReferenceGenre { get; }
            public int Priority { get; }
            public IReadOnlyCollection<string> Parts { get; }

            public GenrePart(string referenceGenre, IReadOnlyCollection<string> parts, int priority)
            {
                ReferenceGenre = referenceGenre;
                Priority = priority;
                Parts = parts;
            }

            public bool IsFit(string value)
            {
                return Parts.Any(item => value.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }

            public static bool TryParse(string value, string referenceGenre, out GenrePart genrePart)
            {
                genrePart = null;

                if (value.StartsWith(StartString))
                {
                    var iof = value.LastIndexOf(']');
                    int priority = Int32.MaxValue;

                    if (value.Length > iof + 1)
                    {
                        priority = Int32.Parse(value.Substring(iof + 1, value.Length - iof - 1));
                    }

                    var parts = value.Substring(StartString.Length, iof - StartString.Length).Split("),(")
                        .Select(item => item.Trim('(', ')')).ToArray();
                    genrePart = new GenrePart(referenceGenre, parts, priority);
                    return true;
                }

                return false;
            }
        }

        public void Dispose()
        {
        }

        public bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        private readonly HashSet<string> _referenceGenres = new HashSet<string>(new[]
        {
            // ReSharper disable StringLiteralTypo
            "Антология",
            "Археология",
            "Астрология",
            "LitRPG|Литрпг|литпгр",
            "Авантюрно-приключенческий роман|Авантюрный роман|Авантюрный приключенческий роман",
            "Авантюрный детектив",
            "Автобиография|Автобиографическая повесть|Автобиографическая проза|Автобиографический роман|contains[(автобио)]",
            "Автомобили и ПДД",
            "Авторский сборник",
            "Альтернативная история",
            "Анекдоты",
            "Антиутопия",
            "Античная литература",
            "Архитектура",
            "Аудиобиблиотека школьника",
            "Аудиокнига",
            "Аудиотренинг",
            "Аудиофильм",
            "Афоризмы и цитаты|Афоризмы",
            "Арт-хаус|Трэш",
            "Басни",
            "Бизнес|Бизнес-книги|Бизнес-литература|бизнес аудиокнига|Бизнес тренинг|contains[(бизнес)]",
            "Биографии и мемуары|Биографический роман|Биография|Биографии писателей и поэтов|Биографии|contains[(биогр)]",
            "Биология|Генетика|Общая биология|Антропология",
            "Боевая фантастика",
            "Боевики",
            "Боевое фэнтези|War fantasy",
            "Будущее",
            "Бухучет налогообложение аудит",
            "Вестерны",
            "Военная история",
            "Военная проза|Военная литература|Военное дело, спецслужбы|Военный роман|Военная драма|Военный|Военные приключения|contains[(война),(войне),(военн)]",
            "Военное дело",
            "Военно-историческая фантастика|Военная фантастика",
            "Воспитание детей",
            "География",
            "Героическая фантастика",
            "Героическое фэнтези",
            "Городское фэнтези",
            "Готический роман|Готика|Готическая проза|Неоготика",
            "Деловая литература",
            "Демография",
            "Детективная фантастика|Комический детектив",
            "Детективное фэнтези",
            "Детективы|Детектив|Детективная повесть|Детективный роман|Детективные рассказы|Детектив в историческом антураже|Японский детектив|Detektiv|Детективный рассказ|Приключенческий детектив|Остросюжетный детектив|Классика детектива|Классика зарубежного детектива|Детективная|Советский детектив|Ретродетектив|Зарубежный детектив|Зарубежный классический детектив|Зарубежные детективы|Детективы: прочее|Современные детективы|Современный детектив|Современный зарубежный детектив|contains[(детектив)]",
            "Детская литература|Детская|Детская повесть|Детские развивающие аудиокниги|Детская познавательная и развивающая литература|Детская проза|Детская сказка|Детские книги|Детские рассказы|Детские сказки|Детский роман|Детский детектив|Детские приключения|Детские истории|Книги для детей|Рассказы для детей|Приключения для детей и подростков|Детективные повести|Детская классика|Детская книга|Детская классическая литература|Повесть для детей|contains[(детск),(детей),(детям)]1",
            "Детская психология",
            "Детская фантастика",
            "Детская энциклопедия",
            "Детские детективы",
            "Детские остросюжетные",
            "Детские стихи",
            "Детское фэнтези|Детское фентези",
            "Для детей и не только",
            "Дневники|Дневник",
            "Документальная литература|Документальная повесть|Документальная проза|Документалистика|Документальный роман|contains[(Документальн)]",
            "Дом и семья|Семья",
            "Домашние животные",
            "Драма",
            "Драматургия",
            "Древнерусская литература",
            "Европейская старинная литература",
            "Женская проза|Женские истории|Женский роман|Современная женская проза|Из книги «я – женщина|contains[(женск)]",
            "Женское фэнтези",
            "Женский детектив|Дамский детектив|Дамский детективный роман",
            "Жизнеописание",
            "Жизнь замечательных людей",
            "Жития",
            "Зарубежная деловая литература",
            "Зарубежная драматургия",
            "Зарубежная классика",
            "Зарубежная компьютерная литература",
            "Зарубежная литература|Зарубежная Проза|Зарубежный роман|Английская проза",
            "Зарубежная литература для детей",
            "Зарубежная образовательная литература",
            "Зарубежная прикладная и научно-популярная литература",
            "Зарубежная психология",
            "Зарубежная публицистика",
            "Зарубежная справочная литература",
            "Зарубежная старинная литература",
            "Зарубежная фантастика",
            "Зарубежная эзотерическая и религиозная литература",
            "Зарубежное фэнтези",
            "Зарубежные боевики",
            "Зарубежные детские книги",
            "Зарубежные любовные романы",
            "Зарубежные приключения",
            "Зарубежные стихи",
            "Зарубежный юмор",
            "Здоровье",
            "Зоология",
            "Изобразительное искусство, фотография",
            "Иностранная классика",
            "Иностранные языки",
            "Инсценировка",
            "Интеллектуальный детектив",
            "Интервью",
            "Интернет",
            "Ироническая проза",
            "Иронические детективы|Иронический детектив",
            "Ироническая фантастика",
            "Искусство|Искусствоведение",
            "Истории",
            "История|Отечественная история|Общая история|Историческое исследование|Историческая аудиокнига|Исторические науки|Историческое повествование|Всемирная история|История России",
            "Историческая|Историко-приключенческий роман|Историческо-приключенческий роман|Историческая драма|Историческая книга|Историческая литература|Историческая повесть|Историческая проза|Исторический|Исторический и приключенческий роман|Исторические романы|Исторический роман|Исторический авантюрный роман|Авантюрный исторический роман|contains[(историко),(историческ)]",
            "Историческая фантастика|Исторический детектив",
            "Исторические детективы",
            "Исторические любовные романы|Исторический любовный роман|Любовно-исторический роман",
            "Исторические приключения",
            "Историческое фэнтези",
            "Исторический триллер",
            "Киберпанк",
            "Кинематограф и театр",
            "Классика|Классическая литература",
            "Классика исполнения отечественной прозы",
            "Классика русской литературы",
            "Классическая библиотека приключений и фантастики",
            "Классическая и современная проза",
            "Классическая историческая литература",
            "Классическая приключенческая литература",
            "Классическая проза",
            "Классические детективы",
            "Классический детектив",
            "Книги о войне",
            "Книги о приключениях",
            "Книги о путешествиях",
            "Книги по психологии",
            "Книги про вампиров",
            "Книги про волшебников",
            "Комедия",
            "Компьютеры",
            "Контркультура",
            "Корпоративная культура",
            "Космическая опера",
            "Космическая фантастика",
            "Космоопера",
            "Космос",
            "Криминал",
            "Криминальные боевики|Криминальный боевик",
            "Криминальный детектив",
            "Криминальный роман",
            "Криптоистория",
            "Критика",
            "Кругозор",
            "Крутой детектив",
            "Кулинария",
            "Культура",
            "Культурология",
            "Легенды",
            "Лекции|Лекция|contains[(лекци)]",
            "Лирика|Лирическая проза",
            "Литература 18 века",
            "Литература 19 века",
            "Литература 20 века",
            "Литература для детей",
            "Литературно-музыкальная композиция",
            "Литературные чтения",
            "Литературоведение",
            "Личная эффективность",
            "Личностный рост",
            "Личные финансы",
            "Любовная фантастика",
            "Любовное фэнтези",
            "Любовно-фантастические романы|Любовно-фантастический роман|Любовный фантастический роман",
            "Любовные детективы|Любовный детектив|Романтический детектив",
            "Любовный роман|Любовный|Любовные романы|Короткие любовные романы|Любившая мужчину|contains[(любов),(любви)]",
            "Магические академии",
            "Магический реализм",
            "Малый бизнес",
            "Маркетинг",
            "Маркетинг, PR, реклама",
            "Математика",
            "Медицина",
            "Медицинский триллер",
            "Мелодрама",
            "Мемуары|Воспоминания|contains[(мемуар),(воспоминан)]",
            "Метафизический реализм",
            "Мировая классика|Классическая зарубежная проза",
            "Мифология",
            "Мифы",
            "Моноспектакль",
            "Монография",
            "Морские приключения",
            "Музыка и балет|Музыка",
            "Музыкальная сказка",
            "Музыкальные сказки",
            "Мюзикл",
            "Наука|Наука и образование",
            "Научно-популярная литература|научно-популярное|Научно-популярное издание|Научно-популярный|Научно-познавательная литература|Научно-популярная|Научпоп|contains[(логия),(научно-поп),(научн)]",
            "Научная фантастика|Научно-фантастических произведений|Научно-фантастическая повесть|Научно-фантастический рассказ|Научно-фантастический роман|Научно-фантастическая повесть|Научно-фантастические рассказы",
            "Начинающие авторы",
            "Наши дни",
            "Не художественная литература",
            "Недвижимость",
            "Неоклассика",
            "Новелла",
            "Новеллы",
            "Нуар",
            "О бизнесе популярно",
            "О войне|contains[(военно)]",
            "Оборотни",
            "Образование",
            "Общая психология",
            "ОС и Сети",
            "Остросюжетная проза|Остросюжетный роман",
            "Остросюжетные любовные романы|Остросюжетный любовный роман",
            "Отечественная классика",
            "Отечественная проза|Проза современная отечественная",
            "Отечественный женский детектив",
            "Отраслевые издания",
            "Очерки",
            "Пародия",
            "Педагогика",
            "Песни",
            "Письма",
            "Повести|Повести и рассказы",
            "Повесть-сказка",
            "Повседневная жизнь",
            "Повседневность",
            "Подростковая литература|contains[(подростк)]",
            "Познавательная литература",
            "Познавательная радиопередача для детей",
            "Познавательное",
            "Поиск работы, карьера",
            "Политическая литература|Политика и политология|Политология|contains[(полити)]",
            "Помоги себе сам",
            "Попаданцы|Попаданец|Попаданка|Попаданец в ВОВ|contains[(попадан)]",
            "Постапокалипсис|Постапокалиптика|Апокалипсис",
            "Постмодернизм|Постмодерн",
            "Поэзия|Поэма|Стихи|Стихотворения|Стихотворения и поэмы|contains[(поэз),(стих),(поэм)]",
            "Практическая психология|Психотренинги|Популярная психология",
            "Приключения|Приключения и путешествия|Приключенческая повесть|Приключенческий роман|Приключенческий роман в историческом антураже|Приключенческие повести|contains[(приключ),(авантюр)]",
            "Приключенческая фантастика",
            "Природа и животные",
            "Притча|Притчи",
            "Проза|Легкая проза",
            "Психология|Классики психологии|Психотренинги|Мотивация|contains[(психология)]",
            "Психологическая|Психологическая драма|Психологическая проза|Психологический роман",
            "Психологическая фантастика",
            "Психологический детектив",
            "Психологический триллер",
            "Психотерапия и консультирование",
            "Публицистика",
            "Путеводители",
            "Путешествия|Путешествия и география|Путешествия и приключения",
            "Пьеса",
            "Аудиоспектакль|Радиоинсценировка|Аудиопостановка|Радиопостановка|Радиопередача|Радиоспектакли|Радиоспектакль|Спектакль|contains[(спектакл)]",
            "Развлечения",
            "Разное",
            "Рассказы|Рассказы и повести",
            "Рассказы о животных|О животных",
            "Реализм",
            "Религия|Религиоведение|Религиозные тексты|Духовная литература|Православная литература|Православная проза|Православие|Ислам|contains[(религи),(православ),(духовн),(христианс),(богос),(библия)]0",
            "Роман|contains[(роман)]",
            "Роман о любви|Романтика|Романтическое|Романтизм",
            "Романтическая комедия",
            "Романтическое фэнтези",
            "Роман-трилогия",
            "Роман-хроника",
            "Роман-эпопея",
            "Руководства",
            "Русская и советская литература XX века",
            "Русская классика|Русская классическая литература|Русская классическая проза",
            "Русская литература|Русская проза|Российская проза|Русская современная проза",
            "Русские народные сказки|Русские сказки",
            "Сад и Огород",
            "Саморазвитие",
            "Самосовершенствование",
            "Сатира и юмор|Сатира|contains[(сатирич)]",
            "Сборник",
            "Сборник рассказов",
            "Сделай Сам",
            "Секс и семейная психология|Сексология|contains[(секс)]",
            "Семейная сага",
            "Сентиментальный роман",
            "Сетевая литература",
            "Сказка для взрослых",
            "Сказки|Сказки для детей|Сказочная повесть|Аудиосказка|contains[(сказк),(сказок),(сказоч)]",
            "Словари",
            "Советская классика|Советская классическая проза|Советская литература|Советская проза",
            "Советская фантастика",
            "Современная зарубежная литература|Современная зарубежная проза",
            "Современная историческая проза",
            "Современная литература",
            "Современная отечественная литература|Современная отечественная проза|Современная русская литература|Современная русская проза",
            "Современная украинская проза",
            "Современная проза|Современная проза современные любовные романы",
            "Современная юмористическая проза",
            "Современные любовные романы",
            "Современный любовный роман",
            "Социальная литература|contains[(социальн)]",
            "Социальная психология",
            "Социальная фантастика",
            "Социология",
            "Спецслужбы",
            "Сплаттерпанк",
            "Спорт и фитнес",
            "Справочная литература",
            "Справочники",
            "Ссср",
            "Старинная литература",
            "Статья",
            "Стимпанк",
            "Сюрреализм",
            "Тайны",
            "Театр у микрофона",
            "Техническая литература",
            "Трагедия",
            "Трагикомедия",
            "Тренинг",
            "Триллеры|Триллер|contains[(триллер)]",
            "Ужасы|Ужасы и мистика|contains[(ужас)]",
            "Украинская классика",
            "Управление и подбор персонала|Управление",
            "Утопия",
            "Учебная литература|Учебник|Учебники и пособия|Учебное пособие|contains[(учебн),(образовате)]",
            "Фантасмагория",
            "Фантастика|Отечественная фантастика|Фантастический рассказ|Фантастический роман|Российская фантастик|Фантастические повести|Фантастическая повесть|Фантастические рассказы|Fantastika|contains[(фантаст)]",
            "Фантастика (вселенная warhammer 40000)",
            "Фантастический боевик",
            "Фантастический детектив",
            "Фанфик|Fanfic|Fanfik",
            "Физика",
            "Филология",
            "Философия",
            "Философское|contains[(философск)]",
            "Философский экшн",
            "Финансы",
            "Фольклор",
            "Фотография",
            "Фэнтези|фентэзи|Фентези|Фэнтези про драконов|Русское фэнтези|Фантазия|Сказочная фантастика|Ненаучная фантастика|Fantasy|contains[(фэнтези),(фентэзи),(фэнтэзи),(fantasy),(фентези),(фентази)]",
            "Технофэнтези",
            "Тёмное фэнтези|Dark fantasy",
            "Мистика|Мистический детектив|Мистический роман|Мистический триллер|Фантастика ужасы и мистика|Парапсихология|Мистические рассказы",
            "Химия",
            "Хобби и ремесла",
            "Хоррор",
            "Христианство",
            "Хроника",
            "Хроноопера",
            "Художественная литература|Художественная проза",
            "Целительство",
            "Ценные бумаги, инвестиции",
            "Чёрный юмор|Черный юмор",
            "Школа",
            "Школьная литература",
            "Шпионские детективы|Шпионский детектив|Шпионский роман|contains[(шпион)]",
            "Эзотерика",
            "Экология",
            "Экономика|contains[(экономи)]",
            "Энциклопедии",
            "Эпическая фантастика",
            "Эпическое фэнтези|Epic fantasy",
            "Эпос|Эпические сказания|Эпическая поэма",
            "Эротическая литература|Эротические романы|Эротика|Эротический роман|contains[(эроти)]",
            "Эротическое фэнтези",
            "Эссе",
            "Этнография",
            "Этти",
            "Юмор|Юмористическая проза|Юмористические рассказы|Наш юмор|Юмористический роман|contains[(юмор)]",
            "Юмористическая фантастика",
            "Юмористические стихи",
            "Юмористическое фэнтези|Сатирическое фэнтези",
            "Юмористический детектив",
            "Юриспруденция и право|contains[(Юридич)]0",
            "Языкознание",
            // ReSharper restore StringLiteralTypo
        }, StringComparer.InvariantCultureIgnoreCase);
    }
}