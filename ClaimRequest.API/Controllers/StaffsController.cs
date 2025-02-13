using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStaffs()
        {
            return Ok();
        }
        [HttpGet]
        [Route("{id}")]
        public IActionResult GetStaffById(string id)
        {
            return Ok();
        }
        [HttpPost]
        public IActionResult CreateStaff()
        {
            return Ok();
        }
        [HttpPut]
        public IActionResult UpdateStaff()
        {
            return Ok();
        }
    }
}
