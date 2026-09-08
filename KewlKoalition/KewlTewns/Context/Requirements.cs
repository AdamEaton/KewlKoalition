using KewlKommon.Context;
using KewlTewns.Utilities;

namespace KewlTewns.Context
{
	public static class TewnsRequirement
	{
		public static readonly Requirement Dj = new RequiresDj();
	}
	public class RequiresDj : RequiresSpecialRole
	{
		public static readonly RequiresDj Instance = new RequiresDj();

		public RequiresDj()
			: base(TewnsUtil.DjRoleKey)
		{

		}
	}
}