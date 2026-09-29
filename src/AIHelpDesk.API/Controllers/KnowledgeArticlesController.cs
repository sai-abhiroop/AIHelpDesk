using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SupportAgent,Admin")]
    public class KnowledgeArticlesController : ControllerBase
    {
        private readonly IKnowledgeArticleService _service;
        private readonly IKnowledgeSearchService _searchService;
        private readonly IKnowledgeRagService _ragService;
            
        public KnowledgeArticlesController(IKnowledgeArticleService service, IKnowledgeSearchService searchService, IKnowledgeRagService ragService)
        {
            _service = service;
            _searchService = searchService;
            _ragService = ragService;
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public async Task<ActionResult<KnowledgeArticleDto>> CreateKnowledgeArticle(CreateKnowledgeArticleDto dto)
        {
            var article = await _service.CreateAsync(dto.Title, dto.Content);

            return CreatedAtAction(nameof(GetById), new{id=article.Id }, article);
        }

        [HttpPost("ask")]
        public async Task<ActionResult<RagResponseDto>> Ask(RagQueryDto dto)
        {
            var result = await _ragService.AskAsync(dto.Question);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KnowledgeArticleDto>>> GetActiveKnowledgeArticles()
        {
            var articles = await _service.GetActiveAsync();

            return Ok(articles);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<KnowledgeArticleDto>> GetById(Guid id)
        {
            var article = await _service.GetByIdAsync(id);

            if(article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search([FromQuery] string searchTerm)
        {
            var result = await _searchService.SearchAsync(searchTerm);

            return Ok(result);
        }

        [HttpGet("semantic-search")]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> SemanticSearch([FromQuery] string searchTerm)
        {
            var result = await _searchService.SemanticSearchAsync(searchTerm);

            return Ok(result);
        }

        [HttpGet("hybrid-search")]
        public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> HybridSearch([FromQuery] string searchTerm)
        {
            var result = await _searchService.HybridSearchAsync(searchTerm);
            
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeArticleDto>> UpdateKnowledgeArticle(Guid id, UpdateKnowledgeArticleDto dto)
        {
            var article = await _service.UpdateAsync(id, dto.Title, dto.Content);

            if(article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }

        [HttpPatch("{id:guid}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var result = await _service.DeactivateAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPatch("{id:guid}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await _service.ActivateAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
