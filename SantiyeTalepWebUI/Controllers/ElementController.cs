using Microsoft.AspNetCore.Mvc;

namespace SantiyeTalepWebUI.Controllers
{
    public class ElementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("bize-ulasin")]
        public IActionResult BizeUlasin()
        {
            return View();
        }

        [Route("devam-eden-projelerimiz")]
        public IActionResult Projects()
        {
            return View();
        }

        [Route("biten-projelerimiz")]
        public IActionResult FinishProjects()
        {
            return View();
        }

        [Route("haberler")]
        public IActionResult News()
        {
            return View();
        }

        [Route("hakkimizda")]
        public IActionResult About()
        {
            return View();
        }

        [Route("kalite-politikamiz")]
        public IActionResult OurPolicy()
        {
            return View();
        }

        [Route("kariyer")]
        public IActionResult Career()
        {
            return View();
        }

        [Route("kurumsal-kimlik")]
        public IActionResult Identity()
        {
            return View();
        }

        [Route("referanslarimiz")]
        public IActionResult Referances()
        {
            return View();
        }

        [Route("site-haritasi")]
        public IActionResult SiteMap()
        {
            return View();
        }

        [Route("hizmetlerimiz")]
        public IActionResult Services()
        {
            return View();
        }
    }
}
