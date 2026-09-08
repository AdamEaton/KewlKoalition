using KewlKommon.Core;

namespace KewlKommon.Extensions
{
	public static class StringExt
	{
		public static string Nicify(this string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;

			string output = s[..1];
			for (int i = 1; i < s.Length; i++)
			{
				if (char.IsUpper(s[i]) && !char.IsWhiteSpace(s[i - 1]))
					output += " ";
				output += s[i];
			}

			return output.Trim();
		}
		public static string? MakeComponentId(this string? componentName)
		{
			return KewlProgram.Bot.ComponentNameToId(componentName);
		}
		public static string? ExtractComponentName(this string? componentId)
		{
			return KewlProgram.Bot.ComponentIdToName(componentId);
		}
	}
}