using AIHelpDesk.Application.Dtos.Categeory;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface ICategeoryService
    {
        Task<CategeoryDetailsDto> CreateCategeoryAsync(CreateCategeoryDto createCategoryDto);

        Task<IEnumerable<CategeoryDetailsDto>> GetActiveCategeoriesAsync();

        Task<CategeoryDetailsDto?> GetCategeoryByIdAsync(Guid id);
    }
}
