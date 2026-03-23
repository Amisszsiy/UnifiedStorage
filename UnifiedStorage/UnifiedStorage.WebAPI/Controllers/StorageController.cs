using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnifiedStorage.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        /// <summary>
        /// This endpoint for testing purpose
        /// </summary>
        /// <returns>Always OK result</returns>
        [Route("Test")]
        public IActionResult Get()
        {
            return Ok("Success");
        }
    }
}
