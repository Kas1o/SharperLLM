using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharperLLM.FunctionCalling;
using SharperLLM.Util;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SharperLLM.API
{
	public class KoboldAPI : ILLMAPI
	{
		public class KoboldAPIConf : ICloneable
		{
			public int?					  max_context_length { get; set; } = 8192;
			public int?					  max_length         { get; set; } = 1024;
			public float?				  rep_pen            { get; set; } = 1.15f;
			public int?					  rep_pen_range      { get; set; } = 512;
			public int?					  sampler_seed       { get; set; }
			public string[]				  stop_sequence      { get; set; } = new string[] { "</s>", "<|im_end|>" };
			public float?				  temperature        { get; set; } = 0.7f;
			public float?				  tfs                { get; set; } //无尾
			public float?				  top_a              { get; set; } = 0;
			public int?					  top_k              { get; set; } = 20;
			public float?				  top_p              { get; set; }
			public float?				  min_p              { get; set; }
			public float?				  typical            { get; set; } = 1; //典型
			public float?				  dynatemp_range     { get; set; } = 0;
			public float?				  smoothing_factor   { get; set; } = 0;
			public float?				  dynatemp_exponent  { get; set; } = 1;
			public int?					  mirostat           { get; set; } = 0;
			public int?					  mirostat_tau       { get; set; } = 5;
			public float?				  mirostat_eta       { get; set; } = 0.46f;
			public string				  genkey             { get; set; }
			public string				  grammar            { get; set; }
			public string				  images             { get; set; } //base64
			public bool?				  trim_stop          { get; set; }
			public bool?				  bypass_eos         { get; set; }
			public string[]				  banned_tokens      { get; set; }
			public Dictionary<int, float> logit_bias         { get; set; } // token id => bias.
			public string                 prompt             { get; set; } // do not change

			public object Clone()
			{
				var clone = new KoboldAPIConf
				{
					max_context_length = max_context_length,
					max_length = max_length,
					rep_pen = rep_pen,
					rep_pen_range = rep_pen_range,
					sampler_seed = sampler_seed,
					stop_sequence = stop_sequence,
					temperature = temperature,
					tfs = tfs,
					top_a = top_a,
					top_k = top_k,
					top_p = top_p,
					min_p = min_p,
					typical = typical,
					dynatemp_range = dynatemp_range,
					smoothing_factor = smoothing_factor,
					dynatemp_exponent = dynatemp_exponent,
					mirostat = mirostat,
					mirostat_tau = mirostat_tau,
					mirostat_eta = mirostat_eta,
					genkey = genkey,
					grammar = grammar,
					images = images,
					trim_stop = trim_stop,
					bypass_eos = bypass_eos,
					banned_tokens = banned_tokens,
					logit_bias = logit_bias,
					prompt = prompt
				};
				return clone;
			}
		}

		public KoboldAPIConf conf = new();
		public Uri _uri;
		public string uri
		{
			get => _uri.AbsoluteUri;
			set
			{
				//validate
				_uri = CreateLooseUri(value);
			}
		}

		public KoboldAPI(string uri, KoboldAPIConf conf)
		{
			this.conf = conf;
			_uri = CreateLooseUri(uri);
		}
		public KoboldAPI(string uri)
		{
			_uri = CreateLooseUri(uri);
		}
		private static Uri CreateLooseUri(string looseUriString)
		{
			looseUriString = looseUriString.TrimEnd('/');
			if (string.IsNullOrWhiteSpace(looseUriString))
			{
				throw new ArgumentException("URI string cannot be null, empty, or whitespace.", nameof(looseUriString));
			}

			// 检查是否已包含http://或https://，如果没有则添加http://
			if (!looseUriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
				!looseUriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				looseUriString = "http://" + looseUriString;
			}

			Uri uri;
			if (Uri.TryCreate(looseUriString, UriKind.Absolute, out uri))
			{
				return uri;
			}
			else
			{
				throw new ArgumentException($"The provided string '{looseUriString}' could not be parsed into a valid URI.");
			}
		}

		public async IAsyncEnumerable<string> GenerateTextStream(string prompt, CancellationToken cancellationToken)
		{
			conf.prompt = prompt;
			var client = new HttpClient();

			client.BaseAddress = new Uri(uri + "/extra/generate/stream");

			var request = new HttpRequestMessage(HttpMethod.Post, uri + "/extra/generate/stream")
			{
				Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
				Encoding.UTF8, "application/json")
			};

			// 将 cancellationToken 传递给 SendAsync 方法
			using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

			// 将 cancellationToken 传递给 ReadAsStreamAsync 方法
			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

			_ = response.Content.Headers.TryGetValues("Content-Type", out var contentTypes);

			if (contentTypes.Any(x => x.StartsWith("text/event-stream")))
			{
				using var reader = new StreamReader(stream);

				while (!reader.EndOfStream)
				{
					// 检查取消令牌是否已取消
					cancellationToken.ThrowIfCancellationRequested();

					// 将 cancellationToken 传递给 ReadLineAsync 方法
					var line = await reader.ReadLineAsync(cancellationToken);

					if (line.StartsWith("data:"))
					{
						var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(line.Substring(5));
						yield return data["token"];
					}
				}
			}
		}
		public async Task<string> GenerateText(string prompt, int retry = 0)
		{
			conf.prompt = prompt;
			var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(999) };
			client.BaseAddress = new Uri(uri + "/v1/generate");

			var request = new HttpRequestMessage(HttpMethod.Post, uri + "/v1/generate")
			{
				Content = new StringContent(JsonConvert.SerializeObject(conf, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
											Encoding.UTF8, "application/json")
			};


			try
			{
				// 发送请求并获取响应
				var response = client.Send(request);
				response.EnsureSuccessStatusCode();

				// 读取响应内容
				string responseBody = response.Content.ReadAsStringAsync().Result;

				// 解析JSON响应，提取"text"字段  
				JObject json = JObject.Parse(responseBody);
				JArray results = (JArray)json["results"];
				if (results.Count > 0)
				{
					string text = results[0]["text"].ToString();
					return text; // 直接返回提取的文本内容
				}
				else
				{
					throw new ApplicationException("No 'results' found in the response.");
				}
			}
			catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
			{
				// 将503服务繁忙异常直接抛出
				throw;
			}
			catch (Exception ex)
			{
				if (retry > 0)
				{
					return await GenerateText(prompt, retry - 1);
				}

				// 将其他异常也直接抛出
				throw new ApplicationException($"An error occurred while processing the request:{ex.Message}", ex);
			}
		}

		public IAsyncEnumerable<string> GenerateChatReplyStream(PromptBuilder promptBuilder, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<string> GenerateChatReply(PromptBuilder promptBuilder)
		{
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<ResponseEx> GenerateChatExStream(PromptBuilder pb, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<ResponseEx> GenerateChatEx(PromptBuilder pb)
		{
			throw new NotImplementedException();
		}
	}
}