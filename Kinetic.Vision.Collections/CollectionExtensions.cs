using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        private readonly static Random _Random = new Random();

        public static IList<T> Shuffle<T>(this IEnumerable<T> items)
        {
            return items.OrderBy(o => Guid.NewGuid()).ToList();
        }

        public static T GetRandom<T>(this IEnumerable<T> items)
        {
            IList<T> list = items.ToList();
            return list.ElementAt(_Random.Next(0, list.Count));
        }

        public static T GetRandom<T>(this IList<T> items)
        {
            return items.ElementAt(_Random.Next(0, items.Count));
        }

        public static LinkedListNode<T> NextOrFirst<T>(this LinkedListNode<T> current)
        {
            if (current.Next == null)
                return current.List.First;
            return current.Next;
        }

        public static LinkedListNode<T> PreviousOrLast<T>(this LinkedListNode<T> current)
        {
            if (current.Previous == null)
                return current.List.Last;
            return current.Previous;
        }

        /// <summary>
        /// Do any of the items in this IEnumerable match the predicate provided.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="list">The list to traverse</param>
        /// <param name="predicate">A predicate that performs an action for each item returning true or false depending on if the condition required is found</param>
        /// <returns>True if a item matches the predicate, False otherwise</returns>
        public static bool Exists<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            return list.Any(predicate);
        }

        /// <summary>
        /// Do any of the items in this IList match the predicate provided.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="list">The list to traverse</param>
        /// <param name="predicate">A predicate that performs an action for each item returning true or false depending on if the condition required is found</param>
        /// <returns>True if a item matches the predicate, False otherwise</returns>
        public static bool Exists<T>(this IList<T> list, Func<T, bool> predicate)
        {
            return list.Any(predicate);
        }

        /// <summary>
        /// Accepts an IEnumerable of T and returns a hashset.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="source">Any IEnumerable of T</param>
        /// <returns>Returns HashSet of T</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        /// <summary>
        /// Accepts any two IEnumerables and returns only the items in the first that do not appear in the second
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="value">The IEnumerable to inspect</param>
        /// <param name="compareTo">The IEnumerable to compareTo</param>
        /// <param name="compareFieldPredicate">A predicate that is invoked for each item in both collections which performs the logical comparison</param>
        /// <returns>An IEnumerable that contains only the distinct items</returns>
        public static IEnumerable<T> Distinct<T, TProp>(this IEnumerable<T> value, IEnumerable<T> compareTo, Func<T, TProp> compareFieldPredicate) where TProp : IEquatable<TProp>
        {
            return value.Where(o => !compareTo.Exists(p => compareFieldPredicate.Invoke(p).Equals(compareFieldPredicate.Invoke(o))));
        }

        /// <summary>
        /// Accepts any type of T and compares it to an arbitrarily long param array of T to determine if any of the items in param array of T exist in the value of T
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="value">Any instance of T</param>
        /// <param name="list">Arbitrarily long param array of T</param>
        /// <returns>If param array of T 'Is In' instance of T</returns>
        public static bool ExistsIn<T>(this T value, params T[] list)
        {
            if (value == null)
                throw new ArgumentNullException("Value");
            return list.Contains(value);
        }

        public static bool ExistsIn(this string value, IEnumerable<string> list)
        {
            bool result = false;
            foreach (string itm in list)
            {
                if (itm == value)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// A closure based ForEach loop
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="enumerable">Any enumerable</param>
        /// <param name="action">An action to perform for each enumerable item</param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T itm in enumerable)
                action.Invoke(itm);
        }

        /// <summary>
        /// Provides the ability to take any property of an object collection and return a comma separated list of the values (IE: string.Join() for collections of T).
        /// </summary>
        /// <typeparam name="T">Any T</typeparam>
        /// <param name="values">Any IEnumerable OF T</param>
        /// <param name="property">A lambda returning the value of the property you wish to Concate</param>
        /// <param name="separator">The separator to use for the concatenation</param>
        /// <returns>A string containing the values in the object separated by the designated separator</returns>
        public static string ConcateWith<T>(this IEnumerable<T> values, Func<T, string> property, string separator)
        {
            return string.Join(separator, values.Select(o => property(o)));
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> sequence, int size)
        {
            List<T> partition = new List<T>(size);
            foreach (var item in sequence)
            {
                partition.Add(item);
                if (partition.Count == size)
                {
                    yield return partition;
                    partition = new List<T>(size);
                }
            }
            if (partition.Count > 0)
                yield return partition;
        }

    }
}
