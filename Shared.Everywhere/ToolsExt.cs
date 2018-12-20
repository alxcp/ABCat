using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static System.String;

// ReSharper disable once CheckNamespace
public static class ToolsExt
{
    private static readonly Random Rnd = new Random();

    public static double Epsilon => 0.00049999999999;

    public static bool InheritsFrom(this Type t, Type baseType)
    {
        var cur = t.BaseType;

        while (cur != null)
        {
            if (cur == baseType)
            {
                return true;
            }

            cur = cur.BaseType;
        }

        return false;
    }

    public static bool TryRemove<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key)
    {
        return dictionary.TryRemove(key, out _);
    }

    public static void AddOrIgnore<T>(this List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }

    public static void AddOrRemoveOrIgnore<T>(this List<T> list, T item, bool add)
    {
        if (add)
        {
            list.AddOrIgnore(item);
        }
        else
        {
            list.RemoveOrIgnore(item);
        }
    }

    public static void AddOrRemoveOrIgnore<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value,
        bool add)
    {
        if (add)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
        else if (dictionary.ContainsKey(key))
        {
            dictionary.Remove(key);
        }
    }

    public static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key] = value;
        }
        else
        {
            dictionary.Add(key, value);
        }
    }

    public static void AddRange<T>(this ISet<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            set.Add(item);
        }
    }

    public static void AddRange<T>(this ObservableCollection<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            list.Add(item);
        }
    }

    public static void AddRange<T>(this List<T> list, params T[] newItems)
    {
        if (newItems.AnySafe())
        {
            list.AddRange(newItems);
        }
    }

    public static bool AnySafe<T>(this IEnumerable<T> collection, Func<T, bool> predicate = null)
    {
        return collection != null && (predicate == null ? collection.Any() : collection.Any(predicate));
    }

    public static T[] AsArray<T>(this T element)
    {
        return new[]
        {
            element
        };
    }

    public static T[] AsArray<T>(this T element, params T[] elements)
    {
        var result = new T[elements.Length + 1];
        result[0] = element;
        result.ToArray().CopyTo(result, 1);
        return result;
    }

    public static T[] AsArray<T>(this IEnumerable<T> elements)
    {
        var result = elements as T[] ?? elements.ToArray();
        return result;
    }

    public static T DeserializeFromXml<T>(this string xml)
    {
        var xs = new XmlSerializer(typeof(T));
        var result = (T) xs.Deserialize(new StringReader(xml));
        return result;
    }

    public static void Each<T>(this IEnumerable<T> items, Action<T> action)
    {
        if (items == null)
        {
            return;
        }

        var itemsArray = items as T[] ?? items.ToArray();
        if (!itemsArray.AnySafe())
        {
            return;
        }

        foreach (var item in itemsArray)
        {
            action(item);
        }
    }

    /// <summary>
    ///     Replaces the format item in a specified string with the string representation of a corresponding object in a
    ///     specified array.
    /// </summary>
    /// <param name="format">A composite format string</param>
    /// <param name="args">An object array that contains zero or more objects to format</param>
    /// <returns>
    ///     A copy of format in which the format items have been replaced by the string representation of the
    ///     corresponding objects in args.
    /// </returns>
    public static string F(this string format, params object[] args)
    {
        return String.IsNullOrEmpty(format) ? format : Format(format, args);
    }

    public static void Fire(this EventHandler eventHandler, object sender, EventArgs e)
    {
        if (eventHandler == null)
        {
            return;
        }

        eventHandler(sender, e);
    }

    public static void Fire<T>(this EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
    {
        eventHandler?.Invoke(sender, e);
    }

    public static TValue GetOrNew<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out var result))
        {
            result = new TValue();
            dictionary.Add(key, result);
        }

        return result;
    }

    public static T GetRandom<T>(this IList<T> collection)
    {
        return collection[Rnd.Next(collection.Count)];
    }

    public static bool In<T>(this T value, params T[] candidates)
    {
        if (!candidates.AnySafe())
        {
            return false;
        }

        if (typeof(T) == typeof(double))
        {
            var doubleValue = Convert.ToDouble(value);
            return candidates.Cast<double>().Any(item => item.IsEquals(doubleValue));
        }

        return candidates.Any(item => Equals(value, item));
    }

    public static bool IsEquals(this double value1, double value2)
    {
        return Math.Abs(value1 - value2) < Epsilon;
    }

    public static bool IsEquals(this double value1, double? value2)
    {
        return value2.HasValue && Math.Abs(value1 - value2.Value) < Epsilon;
    }

    public static bool IsEquals(this double? value1, double value2)
    {
        return value1.HasValue && Math.Abs(value1.Value - value2) < Epsilon;
    }

    public static bool IsEquals(this double? value1, double? value2)
    {
        return !value1.HasValue && !value2.HasValue
               || value1.HasValue && value2.HasValue && Math.Abs(value1.Value - value2.Value) < Epsilon;
    }

    public static bool IsGreater(this double value1, double value2)
    {
        return value1 - value2 > Epsilon;
    }

    public static bool IsLess(this double value1, double value2)
    {
        return value2 - value1 > Epsilon;
    }

    public static bool IsNotEquals(this double value1, double value2)
    {
        return !value1.IsEquals(value2);
    }

    public static bool IsNotEquals(this double value1, double? value2)
    {
        return !value1.IsEquals(value2);
    }

    public static bool IsNotEquals(this double? value1, double value2)
    {
        return !value1.IsEquals(value2);
    }

    public static bool IsNotEquals(this double? value1, double? value2)
    {
        return !value1.IsEquals(value2);
    }

    public static bool IsNullOrEmpty(this string value)
    {
        return String.IsNullOrEmpty(value);
    }

    public static string ReplaceAll(this string value, IReadOnlyCollection<string> oldStrings, string newString)
    {
        foreach (var oldString in oldStrings)
        {
            value = value.Replace(oldString, newString);
        }

        return value;
    }

    public static bool ContainsAny(this string value, IReadOnlyCollection<string> valuesForSearch, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return valuesForSearch.Any(v => value.IndexOf(v, comparison) >= 0);
    }

    public static bool IsTrue(this bool? value)
    {
        return value.HasValue && value.Value;
    }

    public static int Random(this int value)
    {
        return Rnd.Next(value);
    }

    public static string Read(this Stream stream)
    {
        string result;
        var position = stream.Position;
        stream.Position = 0;

        using (var streamReader = new StreamReader(stream))
        {
            result = streamReader.ReadToEnd();
        }

        stream.Position = position;
        return result;
    }

    public static void RemoveOrIgnore<T>(this List<T> list, T item)
    {
        if (list.Contains(item))
        {
            list.Remove(item);
        }
    }

    public static DateTime Round(this DateTime date, TimeSpan span)
    {
        var ticks = (date.Ticks + span.Ticks / 2 + 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks);
    }

    public static string SerializeToXmlString<T>(this T obj)
    {
        using (var ms = new MemoryStream())
        {
            var xs = new XmlSerializer(typeof(T));
            xs.Serialize(ms, obj);
            ms.Position = 0;
            using (var sr = new StreamReader(ms))
            {
                return sr.ReadToEnd();
            }
        }
    }

    public static string[] Split(this string value, int count, params char[] separators)
    {
        return value.Split(separators, count);
    }

    public static string[] Split(this string value, StringSplitOptions options, params string[] separators)
    {
        return value.Split(separators, options);
    }

    public static string[] Split(this string value, params string[] separators)
    {
        return value.Split(separators, StringSplitOptions.None);
    }

    public static string[] Split(this string value, StringSplitOptions options, params char[] separators)
    {
        return value.Split(separators, options);
    }

    public static string ToCurrentCultureString(this decimal value)
    {
        return value.ToString(CultureInfo.CurrentCulture.NumberFormat);
    }

    public static string ToCurrentCultureString(this decimal value, string format)
    {
        return value.ToString(format, CultureInfo.CurrentCulture.NumberFormat);
    }

    public static string ToCurrentCultureString(this double value)
    {
        return value.ToString(CultureInfo.CurrentCulture.NumberFormat);
    }

    public static string ToCurrentCultureString(this double value, string format)
    {
        return value.ToString(format, CultureInfo.CurrentCulture.NumberFormat);
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> list)
    {
        var result = list as ObservableCollection<T>;
        return result ?? new ObservableCollection<T>(list);
    }

    /// <summary>Format numeric value to current culture string with N</summary>
    /// <returns>Formatted value</returns>
    public static string ToStringN(this double value, int digitsCount)
    {
        return value.ToString("N" + digitsCount.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);
    }

    /// <summary>Format numeric value to current culture string with N</summary>
    /// <returns>Formatted value</returns>
    public static string ToStringN(this decimal value, int digitsCount)
    {
        return value.ToString("N" + digitsCount.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);
    }

    public static string ToStringOrEmpty(this object obj)
    {
        return obj == null ? Empty : obj.ToString();
    }

    public static bool Compare(this string value1, string value2, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        return String.Compare(value1, value2, comparison) == 0;
    }
}