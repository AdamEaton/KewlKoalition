using KewlKommon.Context;

namespace KewlBot.Utilities
{
	public static class BotUtil
	{
		public const string ConvictRoleKey = "convict";
		public const string TempCategoryChannelKey = "tempCategory";
		public const string JailChannelKey = "jail";

		public class RequiresNonConvict : RequiresSpecialRole
		{
			public RequiresNonConvict()
				: base(ConvictRoleKey, true)
			{

			}
		}
	}
}
