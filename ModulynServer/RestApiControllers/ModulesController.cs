using Microsoft.AspNetCore.Mvc;
using WebServerBl;
using WebServerInterface;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebServer.RestApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private WebServerModuleManager m_moduleManager;

        public ModulesController(WebServerModuleManager ModuleManager)
        {
            m_moduleManager = ModuleManager;
        }

        // GET: api/<ModulesController>
        [HttpGet]
        public List<WebServerModuleInfo> Get()
        {
            List<WebServerModuleInfo> modList = new List<WebServerModuleInfo>();
            foreach (IWebServerModule module in m_moduleManager.GetModuleList())
            {
                modList.Add(new WebServerModuleInfo() { Name = module.ModuleId, Description = module.Description });
            }
            return modList;
        }

        //// GET api/<ModulesController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<ModulesController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<ModulesController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<ModulesController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
