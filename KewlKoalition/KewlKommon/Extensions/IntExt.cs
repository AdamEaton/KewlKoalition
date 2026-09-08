using System.Collections.Generic;

namespace KewlKommon.Extensions
{
	public static class IntExt
	{
		public static IEnumerable<int> Enumerate(this int n)
		{
			for (int i = 0; i < n; i++)
			{
				yield return i;
			}
		}
	}
}