using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IUserService
    {
        Task<bool> IsActiveSupportAgentAsync(Guid userId);
    }
}
