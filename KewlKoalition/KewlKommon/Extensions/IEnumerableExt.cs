using System;
using System.Collections.Generic;
using System.Linq;

namespace KewlKommon.Extensions
{
	public static class IEnumerableExt
	{
		public static bool ContainsAny<T>(this IEnumerable<T> t, IEnumerable<T> items)
		{
			return items.Any(t.Contains);
		}
		public static bool ContainsAll<T>(this IEnumerable<T> t, IEnumerable<T> items)
		{
			return items.All(t.Contains);
		}
		public static IEnumerable<T> Shuffled<T>(this IEnumerable<T> t)
		{
			var random = new Random();

			var indices = t.Count().Enumerate().ToArray();
			int index;
			int swap;
			for (int i = indices.Length - 1; i >= 0; i--)
			{
				index = random.Next(i);
				swap = indices[i];
				indices[i] = indices[index];
				indices[index] = swap;
			}

			for (int i = 0; i < indices.Length; i++)
			{
				yield return t.ElementAt(indices[i]);
			}
		}
	}
}