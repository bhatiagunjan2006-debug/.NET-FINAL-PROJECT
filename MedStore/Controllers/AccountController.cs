using System.Security.Claims;
using MedStore.Data;
using MedStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
public sealed class AccountController(DemoStore store) : Controller
{
    [AllowAnonymous] public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl=returnUrl; ViewData["Title"]="Login"; return View(); }
    [HttpPost,AllowAnonymous,ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model,string? returnUrl=null)
    {
        ViewData["Title"]="Login";
        if(!ModelState.IsValid) return View(model);
        var user=await store.Authenticate(model.Username,model.Password);
        if(user is null) { ModelState.AddModelError("","Invalid username or password."); ViewData["Title"]="Login"; return View(model); }
            var claims=new[]{new Claim(ClaimTypes.Name,model.Username),new Claim(ClaimTypes.GivenName,user.Value.Name),new Claim(ClaimTypes.Role,user.Value.Role)};
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=model.RememberMe,AllowRefresh=true});
            return Url.IsLocalUrl(returnUrl)?Redirect(returnUrl!):RedirectToAction("Index","Home");
    }
    [Authorize,HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);HttpContext.Session.Clear();return RedirectToAction(nameof(Login));}
    [AllowAnonymous] public IActionResult Denied()=>View();
    [AllowAnonymous] public IActionResult ForgotPassword()=>View();
}
