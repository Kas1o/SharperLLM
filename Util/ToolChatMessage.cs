using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharperLLM.Util
{
    class ToolChatMessage : ChatMessage
    {
        public ToolChatMessage(string _content, string _id, string _imageBase64)
            :base( _content, _imageBase64 )
		{
            this.id = _id;
        }

        public string id;
    }
}
