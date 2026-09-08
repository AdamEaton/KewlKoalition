using System;
using Discord.Interactions;

namespace KewlKommon.Modules
{
	public interface IHelpInfoAttribute
	{
		string Name { get; }
		string Description { get; }
		int Priority { get; }
	}
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class HelpInfoAttribute : Attribute, IHelpInfoAttribute
	{
		public string Name { get; private set; }
		public string Description { get; private set; }
		public int Priority { get; private set; }

		public HelpInfoAttribute(string title, string summary, int priority = HelpPriorities.Default)
		{
			Name = title;
			Description = summary;
			Priority = priority;
		}
	}
	public class HelpInfoGroupAttribute : GroupAttribute, IHelpInfoAttribute
	{
		public int Priority { get; private set; }

		public HelpInfoGroupAttribute(string title, string summary, int priority = 0)
			: base(title, summary)
		{
			Priority = priority;
		}
	}
}
