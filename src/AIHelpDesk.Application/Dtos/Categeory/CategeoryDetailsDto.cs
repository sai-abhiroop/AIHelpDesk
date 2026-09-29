using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.Categeory
{
    public class CategeoryDetailsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
