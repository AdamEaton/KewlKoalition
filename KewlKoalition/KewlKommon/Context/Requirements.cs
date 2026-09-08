using System.Linq;
using KewlKommon.Extensions;
using KewlKommon.Utilities;

namespace KewlKommon.Context
{
	public abstract class Requirement
	{
		public static readonly Requirement None = new RequirementSet();
		public static readonly Requirement Admin = new RequiresAdmin();
		public static readonly Requirement VoiceConnection = new RequiresVoiceConnection();

		public abstract ParsedContextInfo.ErrorCode Check(ParsedContextInfo contextInfo);

		public static RequirementSet operator |(Requirement lhs, Requirement rhs)
		{
			return new RequirementSet(lhs, rhs);
		}
	}
	public class RequirementSet : Requirement
	{
		Requirement[] requirements;
		public override ParsedContextInfo.ErrorCode Check(ParsedContextInfo contextInfo)
		{
			return requirements
				.Select(x => x.Check(contextInfo))
				.FirstOrDefault(x => x != ParsedContextInfo.ErrorCode.None);
		}

		public RequirementSet(params Requirement[] requirements)
		{
			this.requirements = [.. requirements];
		}

		public static RequirementSet operator |(RequirementSet lhs, RequirementSet rhs)
		{
			return new RequirementSet([.. lhs.requirements.Concat(rhs.requirements)]);
		}
		public static RequirementSet operator |(RequirementSet lhs, Requirement rhs)
		{
			return new RequirementSet([.. lhs.requirements.Concat([rhs])]);
		}
		public static RequirementSet operator |(Requirement lhs, RequirementSet rhs)
		{
			return new RequirementSet([.. Enumerable.Concat([lhs], rhs.requirements)]);
		}
	}
	public class RequiresSpecialRole : Requirement
	{
		public string roleKey { get; private set; }
		public bool invert { get; private set; }

		public RequiresSpecialRole(string roleKey, bool invert = false)
		{
			this.roleKey = roleKey;
			this.invert = invert;
		}

		public override ParsedContextInfo.ErrorCode Check(ParsedContextInfo contextInfo)
		{
			return (contextInfo.user?.HasSpecialRole(roleKey) ?? false) != invert
				? ParsedContextInfo.ErrorCode.None
				: ParsedContextInfo.ErrorCode.UserNotAuthorized;
		}
	}
	public class RequiresAdmin : RequiresSpecialRole
	{
		public RequiresAdmin()
			: base(KewlUtil.AdminRoleKey)
		{

		}
	}
	public class RequiresVoiceConnection : Requirement
	{
		public static readonly RequiresVoiceConnection Instance = new RequiresVoiceConnection();

		public override ParsedContextInfo.ErrorCode Check(ParsedContextInfo contextInfo)
		{
			return contextInfo.voiceChannel != null
				? ParsedContextInfo.ErrorCode.None
				: ParsedContextInfo.ErrorCode.UserNotConnected;
		}
	}
}
