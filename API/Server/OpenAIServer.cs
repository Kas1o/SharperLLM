using SharperLLM.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SharperLLM.API.Server
{
		/// <summary>
	/// EndPoints：
	/// - /v1/chat/completions POST
	///   <- {"model": "gpt-3.5-turbo", "messages": [{"role": "user", "content": "Hello!"}], "stream": "true" }
	///   if stream:
	///      -> data: {"choices": [{"index": "0", "delta": {[if first output["role": "assistant"]]"content": "Hello!"}}], "finish_reason": null}
	///   else
	///      -> {"choices": [{"index": "0", "message": {"role": "assistant", "content": "Hello!"}, "finish_reason": null}]}
	///   /v1/models GET
	///   -> {"object": "list", "data": [{"id": "gpt-3.5-turbo", "object": "model", "owned_by": "openai"}]}
	/// </summary>
	public class OpenAIServer
	{
		private readonly Func<(PromptBuilder prompt, string model), Task<string>> _handler;
		private readonly Func<(PromptBuilder prompt, string model), IAsyncEnumerable<string>> _handlerStream;
		private readonly List<string> _models;
		private readonly bool _allowAnyModel;
		private readonly string _prefix;

		public OpenAIServer(
			Func<(PromptBuilder prompt, string model), Task<string>> handler,
			Func<(PromptBuilder prompt, string model), IAsyncEnumerable<string>> handlerStream,
			string host = "localhost",
			int port = 5000,
			List<string>? models = null,
			bool allowRequestAnyModel = true)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
			_handlerStream = handlerStream ?? throw new ArgumentNullException(nameof(handlerStream));
			_models = models ?? new List<string> { "gpt-3.5-turbo", "gpt-4" };
			_allowAnyModel = allowRequestAnyModel;
			_prefix = $"http://{host}:{port}/";
		}

		public void Start()
		{
			var listener = new HttpListener();
			listener.Prefixes.Add(_prefix);
			listener.Start();
			Console.WriteLine($"OpenAI-compatible server listening on {_prefix}");

			_ = Task.Run(async () =>
			{
				while (true)
				{
					var ctx = await listener.GetContextAsync();
					_ = Task.Run(() => HandleRequestAsync(ctx));
				}
			});
		}

		private async Task HandleRequestAsync(HttpListenerContext ctx)
		{
			try
			{
				var req = ctx.Request;
				var resp = ctx.Response;

				// CORS
				resp.Headers.Add("Access-Control-Allow-Origin", "*");
				if (req.HttpMethod == "OPTIONS")
				{
					resp.StatusCode = 200;
					resp.Close();
					return;
				}

				if (req.HttpMethod == "GET" && req.Url!.AbsolutePath.Equals("/v1/models", StringComparison.OrdinalIgnoreCase))
				{
					await WriteJsonAsync(resp, new
					{
						@object = "list",
						data = _models.Select(id => new
						{
							id,
							@object = "model",
							owned_by = "openai"
						})
					});
					return;
				}

				if (req.HttpMethod == "POST" && req.Url!.AbsolutePath.Equals("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
				{
					await HandleChatCompletionsAsync(req, resp);
					return;
				}

				resp.StatusCode = 404;
				resp.Close();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				try { ctx.Response.Close(); } catch { /* ignore */ }
			}
		}

		private async Task HandleChatCompletionsAsync(HttpListenerRequest req, HttpListenerResponse resp)
		{
			using var ms = new MemoryStream();
			await req.InputStream.CopyToAsync(ms);
			ms.Position = 0;
			var json = JsonDocument.Parse(ms);
			var root = json.RootElement;

			var model = root.GetProperty("model").GetString()!;
			var stream = root.TryGetProperty("stream", out var s) && s.GetBoolean();
			var msgsRaw = root.GetProperty("messages");

			if (!_allowAnyModel && !_models.Contains(model))
			{
				resp.StatusCode = 400;
				await WriteJsonAsync(resp, new { error = new { message = $"Model '{model}' not available." } });
				return;
			}

			var pb = new PromptBuilder();

			// role -> From 的映射表
			var map = new Dictionary<string, PromptBuilder.From>(StringComparer.OrdinalIgnoreCase)
			{
				["system"] = PromptBuilder.From.system,
				["user"] = PromptBuilder.From.user,
				["human"] = PromptBuilder.From.user,
				["gpt"] = PromptBuilder.From.assistant,
				["tool_call"] = PromptBuilder.From.tool_call,
				["tool_result"] = PromptBuilder.From.tool_result,
				["assistant"] = PromptBuilder.From.assistant
			};

			foreach (var m in msgsRaw.EnumerateArray())
			{
				var role = m.GetProperty("role").GetString()!;
				var content = m.GetProperty("content").GetString()!;

				if (!map.TryGetValue(role, out var from))
					throw new ArgumentException($"Unknown role: {role}");

				// 直接给 Messages 数组赋值
				pb.Messages = pb.Messages.Append((new ChatMessage(content, null), from)).ToArray();
			}

			if (stream)
			{
				resp.ContentType = "text/plain; charset=utf-8";
				resp.Headers.Add("Cache-Control", "no-cache");
				resp.Headers.Add("Connection", "keep-alive");
				await resp.OutputStream.FlushAsync();

				await foreach (var chunk in _handlerStream((pb, model)))
				{
					var sse = "data: " + JsonSerializer.Serialize(new
					{
						choices = new[]
						{
							new
							{
								index = 0,
								delta = new { content = chunk },
								finish_reason = (string?)null
							}
						}
					}) + "\n\n";
					var bytes = Encoding.UTF8.GetBytes(sse);
					await resp.OutputStream.WriteAsync(bytes);
					await resp.OutputStream.FlushAsync();
				}

				var done = "data: [DONE]\n\n";
				await resp.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(done));
				resp.Close();
			}
			else
			{
				var text = await _handler((pb, model));
				await WriteJsonAsync(resp, new
				{
					choices = new[]
					{
						new
						{
							index = 0,
							message = new { role = "assistant", content = text },
							finish_reason = (string?)null
						}
					}
				});
			}
		}

		private static async Task WriteJsonAsync(HttpListenerResponse resp, object obj)
		{
			resp.ContentType = "application/json";
			var bytes = JsonSerializer.SerializeToUtf8Bytes(obj);
			resp.ContentLength64 = bytes.Length;
			await resp.OutputStream.WriteAsync(bytes);
			resp.Close();
		}
	}
}
