using System.IO;

namespace KewlSquid.Core
{
	static class Config
	{
		public static string? Token = null;
		public static string? ServerIdentifier = null;

		public static string GetFilePath(string filename) { return Path.Combine("Configs", filename); }

		public static bool LoadConfig(string filename)
		{
			try
			{
				using (var file = File.Open(GetFilePath(filename), FileMode.Open))
				using (var reader = new StreamReader(file))
				{
					Token = reader.ReadLine();
					ServerIdentifier = reader.ReadLine();
					return true;
				}
			}
			catch
			{
				Token = null;
				ServerIdentifier = null;
				return false;
			}
		}
	}
}
