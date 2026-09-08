using System;
using System.Collections.Generic;
using System.Linq;

namespace KewlKommon.Utilities
{
	public static class EnumUtil
	{
		public static IEnumerable<T> GetValues<T>() where T : Enum
		{
			foreach (var output in Enum.GetValues(typeof(T)).Cast<T>())
			{
				yield return output;
			}
		}
	}
}