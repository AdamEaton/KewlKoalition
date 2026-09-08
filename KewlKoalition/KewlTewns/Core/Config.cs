using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using KewlKommon.Core;
using KewlKommon.Extensions;

namespace KewlTewns.Core
{
	public class GuildConfig : KewlGuildConfig
	{
		Dictionary<string, CustomCommand> _customCommands = new Dictionary<string, CustomCommand>(StringComparer.InvariantCultureIgnoreCase);
		public IReadOnlyList<CustomCommand> customCommands
		{
			get
			{
				return [.. _customCommands.Values];
			}
		}
		public bool TryGetCustomCommand(string? name, [NotNullWhen(true)] out CustomCommand? command)
		{
			if (string.IsNullOrEmpty(name))
			{
				command = null;
				return false;
			}

			return _customCommands.TryGetValue(name, out command);
		}

		public override bool Extract(JsonNode? node)
		{
			try
			{
				if (!base.Extract(node))
					return false;

				_customCommands.Clear();
				if (node.TryGetArray("commands", out var commands))
				{
					foreach (var command in commands)
					{
						if (!command.TryGetString("name", out var name))
							continue;
						if (!command.TryGetString("description", out var description))
							continue;
						if (!command.TryGetArray("body", out var body))
							continue;

						_customCommands[name] = new CustomCommand(name, description, body.Select(y => y?.ToString()).OfType<string>());
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public class CustomCommand
		{
			public string name { get; private init; }
			public string description { get; private init; }
			List<string> _body;
			public IList<string> body
			{
				get
				{
					return _body.AsReadOnly();
				}
			}

			public CustomCommand(string name, string description, IEnumerable<string> body)
			{
				this.name = name;
				this.description = description;
				_body = [.. body];
			}
		}
	}
}