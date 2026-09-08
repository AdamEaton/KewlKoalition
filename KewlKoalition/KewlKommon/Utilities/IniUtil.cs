using System.Collections.Generic;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace KewlKommon.Utilities
{
	public static class IniUtil
	{
		public const string ConfigsPath = "Configs";
		public const string Extension = ".ini";

		public static string FormatPath(string path)
		{
			path = path.Trim();
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			while (path.StartsWith(Path.DirectorySeparatorChar))
				path = path[1..];
			if (!path.StartsWith(ConfigsPath))
				path = Path.Combine(ConfigsPath, path);
			if (!path.EndsWith(Extension, System.StringComparison.InvariantCultureIgnoreCase))
				path += Extension;

			return path;
		}

		public static bool FileExists(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return false;

			path = FormatPath(path);
			return File.Exists(path);
		}

		public static string Get(string path, string key)
		{
			path = FormatPath(path);

			if (!FileExists(path))
				throw new FileNotFoundException("Config file '" + path + "' not found!");

			foreach (var line in File.ReadLines(path))
				if (TryParseLine(line, out var k, out var v) && key.Equals(k, System.StringComparison.InvariantCultureIgnoreCase))
					return v;

			throw new KeyNotFoundException("The key '" + key + "' was not found in the file '" + path + "'!");
		}
		public static bool TryGet(string file, string key, [NotNullWhen(true)] out string? value)
		{
			try
			{
				value = Get(file, key);
				return true;
			}
			catch
			{
				value = null;
				return false;
			}
		}
		public static Dictionary<string, string> GetAll(string path)
		{
			path = FormatPath(path);

			if (!FileExists(path))
				throw new FileNotFoundException("Config file '" + path + "' not found!");

			var output = new Dictionary<string, string>();
			foreach (var line in File.ReadLines(path))
				if (TryParseLine(line, out var k, out var v))
					output[k] = v;
			return output;
		}
		public static bool TryGetAll(string file, [NotNullWhen(true)] out Dictionary<string, string>? value)
		{
			try
			{
				value = GetAll(file);
				return true;
			}
			catch
			{
				value = null;
				return false;
			}
		}

		public static bool TryParseLine(string line, [NotNullWhen(true)] out string? key, [NotNullWhen(true)] out string? value)
		{
			var index = line.IndexOf('=');
			if (index < 0)
			{
				key = null;
				value = null;
				return false;
			}

			key = line[..index].Trim();
			value = line[(index + 1)..].Trim();
			return true;
		}
	}
}
