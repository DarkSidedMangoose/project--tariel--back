using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Structure : ControllerBase
    {
        private IStructureOfSystemRepository _structureOfSystemRepository;


        public Structure(IStructureOfSystemRepository structureOfSystemRepository)
        {
            _structureOfSystemRepository = structureOfSystemRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _structureOfSystemRepository.GetAllAsync();
            Console.WriteLine(result);
            return Ok(result);
        }
    }
}
