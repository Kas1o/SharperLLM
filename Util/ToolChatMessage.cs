using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
    public class ToolChatMessage : ChatMessage
    {
        public ToolChatMessage(string _content, string _id)
            :base( _content, null)
		{
            this.id = _id;
        }

        public string id;
    }
}
