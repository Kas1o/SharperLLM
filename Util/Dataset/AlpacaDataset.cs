using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util.Dataset
{
    public class AlpacaDataset
    {
		public string instruction { get; set; }
		public string output { get; set; }
		public string input { get; set; }

		public void WriteToPromptBuilder(PromptBuilder originPromptBuilder)
		{
			originPromptBuilder.Messages =
				[
				(input + "\n" +instruction, PromptBuilder.From.user),
				(output, PromptBuilder.From.assistant)
				];
		}
	}
}
