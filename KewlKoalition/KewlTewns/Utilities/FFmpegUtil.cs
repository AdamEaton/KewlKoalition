using System.Diagnostics;
using System.Threading.Tasks;

namespace KewlTewns.Utilities
{
	public static class FFmpegUtil
	{
		public static Process? CreateStream(string path)
		{
			return Process.Start(
				new ProcessStartInfo
				{
					FileName = "ffmpeg",
					Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
					UseShellExecute = false,
					RedirectStandardOutput = true,
				});
		}
		public static async Task CopyFileWithMetadata(string? inputPath, string? outputPath, params (string? key, string? value)[] metadata)
		{
			var args = $"-hide_banner -loglevel panic -i \"{inputPath}\" -codec copy";
			foreach (var md in metadata)
				args += " -metadata " + md.key + "=\"" + md.value + "\"";
			args += " \"" + outputPath + "\"";

			using (var process = Process.Start(
				new ProcessStartInfo
				{
					FileName = "ffmpeg",
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = false,
				}))
			{
				while (process != null && !process.HasExited)
					await Task.Delay(100);
			}
		}
	}
}