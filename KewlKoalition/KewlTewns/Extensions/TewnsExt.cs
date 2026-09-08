using System.Threading.Tasks;
using KewlKommon.Context;
using KewlKommon.Interaction;
using KewlKommon.Utilities;
using KewlTewns.Utilities;

namespace KewlTewns.Extensions
{
	public static class TewnsExt
	{
		public static async Task<bool> PerformStandardDownloadResponse(this IResponseHandler responseHandler, ParsedContextInfo contextInfo, YoutubeUtil.DownloadedVideoInfo downloadInfo, bool includeReply)
		{
			if (downloadInfo.errorCode == YoutubeUtil.DownloadedVideoInfo.ErrorCode.None)
				return true;

			ConsoleUtil.WriteLine(YoutubeUtil.GetStandardVideoLog(downloadInfo));
			if (includeReply)
				await responseHandler.SendReply(YoutubeUtil.GetStandardVideoReply(contextInfo, downloadInfo));
			return false;
		}
	}
}
