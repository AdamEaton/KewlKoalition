using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace KewlKommon.Extensions
{
	public static class JsonNodeExt
	{
		public static bool TryGetNode(this JsonNode? node, string key, [NotNullWhen(true)] out JsonNode? output)
		{
			if (node != null
				&& node.AsObject() is JsonObject obj
				&& obj.ContainsKey(key)
				&& obj[key] is JsonNode outputNode)
			{
				output = outputNode;
				return true;
			}

			output = null;
			return false;
		}
		public static bool TryGetValue(this JsonNode? node, string key, [NotNullWhen(true)] out JsonValue? output)
		{
			try
			{
				if (node.TryGetNode(key, out var outputNode)
					&& outputNode.AsValue() is JsonValue outputVal)
				{
					output = outputVal;
					return true;
				}
			}
			catch { }

			output = null;
			return false;
		}
		public static bool TryGetObject(this JsonNode? node, string key, [NotNullWhen(true)] out JsonObject? output)
		{
			try
			{
				if (node.TryGetNode(key, out var outputNode)
					&& outputNode.AsObject() is JsonObject outputObj)
				{
					output = outputObj;
					return true;
				}
			}
			catch { }

			output = null;
			return false;
		}
		public static bool TryGetArray(this JsonNode? node, string key, [NotNullWhen(true)] out JsonArray? output)
		{
			try
			{
				if (node.TryGetNode(key, out var outputNode)
					&& outputNode.AsArray() is JsonArray outputArr)
				{
					output = outputArr;
					return true;
				}
			}
			catch { }

			output = null;
			return false;
		}
		public static bool TryGetString(this JsonNode? node, string key, [NotNullWhen(true)] out string? output)
		{
			try
			{
				if (node.TryGetNode(key, out var outputNode)
					&& outputNode.ToString() is string outputStr)
				{
					output = outputStr;
					return true;
				}
			}
			catch { }

			output = null;
			return false;
		}

		public static ulong AsUlong(this JsonNode? node)
		{
			try
			{
				return node?.GetValue<ulong>() ?? 0UL;
			}
			catch { }

			return 0UL;
		}
	}
}
