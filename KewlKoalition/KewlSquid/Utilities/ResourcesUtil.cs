using System.IO;

namespace KewlSquid.Utilities
{
	public static class ResourcesUtil
	{
		public const string ResourcesPath = "Resources";

		public static string GetResourcePath(string filename)
		{
			Directory.CreateDirectory(ResourcesPath);
			return Path.Combine(ResourcesPath, filename);
		}
	}
}
