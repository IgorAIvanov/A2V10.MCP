using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace A2V10.MCP.Prompts
{
    [McpServerPromptType]
    public class MyPrompts
    {
        [McpServerPrompt, Description("Step-by step creating A2V10 model")]
        public static ChatMessage Greeting()
            => new(ChatRole.User, "Hello! How can you help me today?");
    }
}
