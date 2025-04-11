using ASP.MongoDb.API.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASP.MongoDb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StructureNdController : ControllerBase
    {
        private IDataOfStructureRepository _dataOfStructureRepository;

        public StructureNdController(IDataOfStructureRepository dataOfStructureRepository)
        {
            _dataOfStructureRepository = dataOfStructureRepository;
        }

        [HttpGet("getStructureData")]
        public async Task<IActionResult> GetStructureData()
        {
            var result = await _dataOfStructureRepository.GetAllAsync();
            return Ok(result);
        }
    }
}
