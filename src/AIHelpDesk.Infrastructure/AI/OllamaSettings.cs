using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Infrastructure.AI
{
    public class OllamaSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
        public string ChatModel { get; set; } = "llama3.2";
    }
}
