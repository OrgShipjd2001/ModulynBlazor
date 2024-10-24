using Microsoft.AspNetCore.Mvc;

namespace TestModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestModuleController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "TestModuleController";
        }
        
    }
}
