using Microsoft.AspNetCore.Mvc;
using SantiyeTalepWebUI.Services;
using SantiyeTalepWebUI.Models.ViewModels;

namespace SantiyeTalepWebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthService _authService;

        public HomeController(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kullanıcı tipine göre dashboard'a yönlendir
            return currentUser.Role switch
            {
                Models.UserRole.Admin => RedirectToAction("Dashboard", "Admin"),
                Models.UserRole.Employee => RedirectToAction("Dashboard", "Employee"),
                Models.UserRole.Supplier => RedirectToAction("Dashboard", "Supplier"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}