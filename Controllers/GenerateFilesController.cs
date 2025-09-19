using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using ASP.MongoDb.API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerateFilesController : ControllerBase
    {
        private readonly IGenerateFilesRepository _generateFilesRepository;

        public GenerateFilesController(IGenerateFilesRepository generateFilesRepository)
        {
         _generateFilesRepository = generateFilesRepository;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _generateFilesRepository.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("getTemplatesName")]

        public async Task<IActionResult> GetTemplates()
        {
            var allData = await _generateFilesRepository.GetAllAsync();

            if (allData == null)
            {
                return BadRequest("connection Error to the database");
            }
            else
            {

                var templateList = allData.Select(t => t.templateName).ToList();
                var templateIds = allData.Select(t => t.id).ToList();

                return Ok(new
                {
                    templateList = templateList,
                    templateIds = templateIds
                });
            }
        }
        [HttpGet("getTemplateState")]
        public async Task<IActionResult> GetTemplateState([FromQuery] string templateId)
        {
            if(string.IsNullOrEmpty(templateId))
            {
                return BadRequest("cant access template id correctly");
            }

            var templateData = await _generateFilesRepository.GetByIdAsync(templateId);
            return Ok(templateData);
        }

        [HttpPost("addNewTemplate")]
        public async Task<IActionResult> AddNewTemplate([FromBody] GenerateFiles response )
        {
            if(response == null)
            {
                return BadRequest("response take from front is null");
            }else
            {
            await _generateFilesRepository.CreateAsync(response);
            return Ok("yes");

            }
        }
        [HttpPut("updateTemplate")]
        public async Task<IActionResult> UpdateTemplate([FromBody] GenerateFiles response)
        {
            if (response == null)
            {
                return BadRequest("response take from front is null");
            }
            else
            {
                await _generateFilesRepository.UpdateAsync(response.id, response);
                return Ok("yes");

            }
        }

    }
}
