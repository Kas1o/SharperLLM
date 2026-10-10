using System;

namespace SharperLLM.Util
{
	/// <summary>
	/// 一条多模态消息中的图像内容块，只承载请求需要的 Base64 数据。
	/// <para>
	/// 本类型不做任何文件读写：磁盘上的图像由上层应用在构建请求前读取并转成 Base64
	/// （例如 ShimmerChat 的 UserUploadImage 存储方式），因此这里缺失数据即数据错误，
	/// 构建请求时会直接抛异常而不是静默忽略。
	/// </para>
	/// </summary>
	public class ChatImage : ICloneable
	{
		public const string DefaultMimeType = "image/png";

		/// <summary>MIME 类型，例如 image/png；为空时按 <see cref="DefaultMimeType"/> 处理。</summary>
		public required string MimeType { get; set; }

		/// <summary>Base64 图像数据（不含 data URL 前缀）。</summary>
		public required string Base64 { get; set; }

		public static ChatImage FromBase64(string base64, string mimeType)
			=> new() { Base64 = base64, MimeType = mimeType };

		/// <summary>OpenAI 兼容 API 的 image_url 形式：data:{mime};base64,{data}</summary>
		public string GetDataUrl()
		{
			if (string.IsNullOrEmpty(Base64))
				throw new InvalidOperationException(
					"ChatImage carries no Base64 data; the image must be resolved before building a request.");

			var mimeType = string.IsNullOrWhiteSpace(MimeType) ? DefaultMimeType : MimeType;
			return $"data:{mimeType};base64,{Base64}";
		}

		public object Clone() => new ChatImage
		{
			MimeType = MimeType,
			Base64 = Base64
		};
	}
}
