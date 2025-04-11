using ASP.MongoDb.API.Entities;
using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StructureController : ControllerBase
    {
        private IStructureOfSystemRepository _structureOfSystemRepository;


        public StructureController(IStructureOfSystemRepository structureOfSystemRepository)
        {
            _structureOfSystemRepository = structureOfSystemRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentDiversion()
        {
            try
            {


                var allStructures = await _structureOfSystemRepository.GetAllAsync();

                return Ok(allStructures);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"there is a error occured: {ex.Message}");
                return StatusCode(500, "internal server error occured");
            }
        }



    }
}
