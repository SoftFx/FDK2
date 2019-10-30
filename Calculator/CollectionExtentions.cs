using System;
using System.Collections.Generic;
using System.Linq;

namespace TickTrader.FDK.Calculator
{
    public static class CollectionExtentions
    {
        #region Dictionary

        public static T GetOrAdd<K, T>(this IDictionary<K, T> dictionary, K key, Func<T> valueProvider)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
                return value;

            value = valueProvider();
            dictionary[key] = value;
            return value;
        }

        public static T GetOrAdd<K, T>(this IDictionary<K, T> dictionary, K key, Func<K, T> valueProvider)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
                return value;

            value = valueProvider(key);
            dictionary[key] = value;
            return value;
        }

        public static T GetOrAdd<K, T>(this IDictionary<K, T> dictionary, K key)
            where T : new()
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
                return value;

            value = new T();
            dictionary[key] = value;
            return value;
        }

        public static T GetOrDefault<K, T>(this IDictionary<K, T> dictionary, K key)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
                return value;

            return default(T);
        }

        public static T AddOrModify<K, T>(this IDictionary<K, T> dictionary, K key, Func<T> addFunc, Action<T> modifyAction)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
            {
                modifyAction(value);
                return value;
            }

            value = addFunc();
            dictionary[key] = value;
            return value;
        }

        #endregion

        #region IEnumerable

        public static void Foreach<T>(this IEnumerable<T> enumerable, Action<T> itemAction)
        {
            foreach (T item in enumerable)
                itemAction(item);
        }

        #endregion

        #region IList

        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, int size)
        {
            for (int i = 0; i < Math.Ceiling(source.Count / (double)size); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }

        public static void BatchForEach<T>(this IList<T> enumerable, int batchSize, Action<List<T>> batchAction)
        {
            foreach (List<T> itemsBatch in Partition<T>(enumerable, batchSize))
                batchAction(itemsBatch);
        }

        public static List<T> ToList<T>(this IList<T> srcList)
        {
            List<T> newList = new List<T>(srcList.Count);
            newList.AddRange(srcList);
            return newList;
        }

        public static T[] ToArray<T>(this IList<T> srcList)
        {
            T[] array = new T[srcList.Count];
            srcList.CopyTo(array, 0);
            return array;
        }

        public static T First<T>(this IList<T> srcList)
        {
            return srcList[0];
        }

        public static T FirstOrDefault<T>(this IList<T> srcList)
        {
            return srcList.Count == 0 ? default(T) : srcList[0];
        }

        public static T Last<T>(this IList<T> srcList)
        {
            return srcList[srcList.Count - 1];
        }

        public static T LastOrDefault<T>(this IList<T> srcList)
        {
            return srcList.Count == 0 ? default(T) : srcList[srcList.Count - 1];
        }

        /// <summary>
        /// Warning! It's a heavy operation O(n).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="srcList"></param>
        public static void RemoveFisrt<T>(this IList<T> srcList)
        {
            srcList.RemoveAt(srcList.Count - 1);
        }

        public static void RemoveLast<T>(this IList<T> srcList)
        {
            srcList.RemoveAt(srcList.Count - 1);
        }

        #endregion

        #region List

        public static List<T> With<T>(this List<T> list, T item)
        {
            list.Add(item);
            return list;
        }

        #endregion List

        #region LinkedList

        public static IEnumerable<T> Reverse<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> i = list.Last;

            while (i != null)
            {
                yield return i.Value;
                i = i.Previous;
            }
        }

        public static IEnumerable<LinkedListNode<T>> Nodes<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> i = list.First;

            while (i != null)
            {
                yield return i;
                i = i.Next;
            }
        }

        public static IEnumerable<LinkedListNode<T>> ReverseNodes<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> i = list.Last;

            while (i != null)
            {
                yield return i;
                i = i.Previous;
            }
        }

        public static LinkedListNode<T> Find<T>(this LinkedList<T> list, Predicate<T> match)
        {
            foreach (LinkedListNode<T> node in list.Nodes())
            {
                if (match(node.Value))
                    return node;
            }

            return null;
        }

        public static LinkedListNode<T> FindLast<T>(this LinkedList<T> list, Predicate<T> match)
        {
            foreach (LinkedListNode<T> node in list.ReverseNodes())
            {
                if (match(node.Value))
                    return node;
            }

            return null;
        }

        #endregion

        #region ICollection

        public static void Add<T>(this ICollection<T> collection, params T[] items)
        {
            foreach (T item in items)
                collection.Add(item);
        }

        #endregion
    }

    public static class Collection
    {
        public static IEnumerable<T> FromValues<T>(params T[] values)
        {
            return values;
        }

        public static IEnumerable<T> ListOfValues<T>(params T[] values)
        {
            return values.ToList();
        }
    }
}
