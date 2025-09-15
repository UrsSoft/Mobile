using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SantiyeTalepWebUI.Models;
using SantiyeTalepWebUI.Services;

namespace SantiyeTalepWebUI.Filters
{
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly UserRole _requiredRole;

        public AuthorizeRoleAttribute(UserRole requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var authService = context.HttpContext.RequestServices.GetService<IAuthService>();
            if (authService == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var currentUser = authService.GetCurrentUser();
            var token = authService.GetStoredToken();

            if (currentUser == null || string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (currentUser.Role != _requiredRole)
            {
                context.Result = new ForbidResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    public class RequireAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var authService = context.HttpContext.RequestServices.GetService<IAuthService>();
            if (authService == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var currentUser = authService.GetCurrentUser();
            var token = authService.GetStoredToken();

            if (currentUser == null || string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}