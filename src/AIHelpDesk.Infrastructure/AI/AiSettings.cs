using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Infrastructure.AI
{
    public class AiSettings
    {
        public string Provider { get; set; } = "Fake";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }
}
