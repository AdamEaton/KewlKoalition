using System.Threading.Tasks;
using KewlKommon.Context;
using KewlKommon.Modules;
using KewlKommon.Utilities;
using KewlTewns.Utilities;

namespace KewlTewns.Modules
{
	public abstract class KewlTewnsModule : KewlModule
	{
		public async Task<bool> PerformStandardDownloadResponse(ParsedContextInfo contextInfo, YoutubeUtil.DownloadedVideoInfo downloadInfo, bool includeReply)
		{
			if (downloadInfo.errorCode == YoutubeUtil.DownloadedVideoInfo.ErrorCode.None)
				return true;

			ConsoleUtil.WriteLine(YoutubeUtil.GetStandardVideoLog(downloadInfo));
			if (includeReply)
				await SendReply(YoutubeUtil.GetStandardVideoReply(contextInfo, downloadInfo));
			return false;
		}
	}
}