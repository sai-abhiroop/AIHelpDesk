using AIHelpDesk.Application.Dtos.Categeory;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Services
{
    public class CategeoryService : ICategeoryService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public CategeoryService(IApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        public async Task<CategeoryDetailsDto> CreateCategeoryAsync(CreateCategeoryDto createCategoryDto)
        {
            var nameExists= await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == createCategoryDto.Name.ToLower());

            if (nameExists)
            {
                throw new InvalidOperationException("A category with this name already exists.");
            }

            var categeory = new Category(createCategoryDto.Name, createCategoryDto.Description);

            _dbContext.Categories.Add(categeory);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<CategeoryDetailsDto>(categeory);

        }

        public async Task<IEnumerable<CategeoryDetailsDto>> GetActiveCategeoriesAsync()
        {
            var categeories = await _dbContext.Categories
                                             .AsNoTracking()
                                             .Where(c => c.IsActive)
                                             .OrderBy(c => c.Name)
                                             .ToListAsync();

            return _mapper.Map<IEnumerable<CategeoryDetailsDto>>(categeories);
        }

        public async Task<CategeoryDetailsDto?> GetCategeoryByIdAsync(Guid id)
        {
            var category = await _dbContext.Categories
                                           .AsNoTracking()
                                           .FirstOrDefaultAsync(c =>c.Id == id && c.IsActive);

            if (category == null)
            {
                return null;
            }

            return _mapper.Map<CategeoryDetailsDto>(category);
        }
    }
}
