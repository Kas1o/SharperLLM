using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SharperLLM.API
{
	/// <summary>
	/// OpenAI 文本补全客户端（/completions 端点）。
	/// 注意：新版 OpenAI 模型多已废弃此端点，仅用于兼容旧接口或自部署服务。
	/// </summary>
	public class OpenAITextCompletionClient : ITextCompletionClient
	{
		private readonly string _url;
		private readonly string _apiKey;
		private readonly string _model;
		private readonly float _temperature;
		private readonly int _maxTokens;

		public OpenAITextCompletionClient(string url, string apiKey, string model,
			float temperature = 0.7f, int maxTokens = 8192)
		{
			_url = url;
			_apiKey = apiKey;
			_model = model;
			_temperature = temperature;
			_maxTokens = maxTokens;
		}

		public async Task<string> GenerateAsync(string prompt)
		{
			var targetURL = $"{_url}/completions";
			var requestBody = new
			{
				model = _model,
				prompt,
				max_tokens = _maxTokens,
				temperature = _temperature
			};

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

			var jsonContent = JsonConvert.SerializeObject(requestBody);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(targetURL, content);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var jsonResponse = JObject.Parse(responseString)
					?? throw new InvalidDataException("API returns invalid JSON Object");
				return jsonResponse["choices"]?[0]?["text"]?.ToString()
					?? throw new InvalidDataException("API response contains no choices[0].text field.");
			}

			throw new HttpRequestException(
				$"OpenAI /completions error ({response.StatusCode}): {responseString}");
		}

		public async IAsyncEnumerable<string> GenerateStreamAsync(
			string prompt,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var targetURL = $"{_url}/completions";
			var requestBody = new
			{
				model = _model,
				prompt,
				max_tokens = _maxTokens,
				temperature = _temperature,
				stream = true
			};

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

			var jsonContent = JsonConvert.SerializeObject(requestBody);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			using var request = new HttpRequestMessage(HttpMethod.Post, targetURL) { Content = content };
			using var response = await client.SendAsync(
				request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
				throw new HttpRequestException(
					$"OpenAI /completions error ({response.StatusCode}): {errorContent}");
			}

			using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			using var reader = new StreamReader(responseStream, Encoding.UTF8);

			while (await reader.ReadLineAsync(cancellationToken) is string line)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				var match = Regex.Match(line, @"^data: (.*)$");
				if (!match.Success)
					continue;

				var json = match.Groups[1].Value;
				if (json == "[DONE]")
					break;

				JObject data;
				try { data = JObject.Parse(json); }
				catch (JsonException) { continue; }

				var text = data["choices"]?[0]?["text"]?.ToString();
				if (text != null)
					yield return text;
			}
		}
	}
}
