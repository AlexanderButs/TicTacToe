using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.Controllers
{
    public class HomeController : Controller
    {
        [Route("/")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()   
        {
            return Redirect($"~{Startup.SwaggerRoute}");
        }
    }
}