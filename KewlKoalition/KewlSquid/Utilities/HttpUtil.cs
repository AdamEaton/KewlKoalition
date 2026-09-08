using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace KewlSquid.Utilities
{
	public static class HttpUtil
	{
		public static HttpClient Client { get; private set; } = new HttpClient();
		public const string BaseUri = "https://splatoon.oatmealdome.me/api/v1/";

		static HttpUtil()
		{
			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		public static async Task<HttpRequestMessage> NewGetMessage(string relativePath, params (string, string)[] args)
		{
			return await NewGetMessage(relativePath, null, false, args);
		}
		public static async Task<HttpRequestMessage> NewGetMessage(string relativePath, string? eTag, bool eTagIsWeak, params (string, string)[] args)
		{
			var output = new HttpRequestMessage();

			output.Method = HttpMethod.Get;
			var uri = BaseUri + relativePath;
			if (!string.IsNullOrEmpty(eTag))
				output.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(eTag, eTagIsWeak));

			if (args != null && args.Length > 0)
			{
				var argsDict = new Dictionary<string, string>();
				foreach (var arg in args)
				{
					argsDict[arg.Item1] = arg.Item2;
				}
				uri += "?" + await new FormUrlEncodedContent(argsDict).ReadAsStringAsync();
			}
			output.RequestUri = new Uri(uri);

			return output;
		}
	}
}
