using AIHelpDesk.Application.Dtos.Categeory;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategeoryController : ControllerBase
    {
        private readonly ICategeoryService _categeoryService;

        public CategeoryController(ICategeoryService categeoryService)
        {
            _categeoryService = categeoryService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CategeoryDetailsDto>>> GetActiveCategeories()
        {
            var categeories = await _categeoryService.GetActiveCategeoriesAsync();

            return Ok(categeories);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<CategeoryDetailsDto>> GetCategeory(Guid id)
        {
            var categeory = await _categeoryService.GetCategeoryByIdAsync(id);

            if (categeory == null)
            {
                return NotFound();
            }

            return Ok(categeory);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategeoryDetailsDto>> CreateCategeory(CreateCategeoryDto createCategoryDto)
        {
            var categeory = await _categeoryService.CreateCategeoryAsync(createCategoryDto);

            return CreatedAtAction(nameof(GetCategeory), new {id = categeory.Id},categeory);
        }
    }
}
