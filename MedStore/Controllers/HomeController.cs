using MedStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[Authorize]
public sealed class HomeController(DemoStore store) : Controller
{
    public async Task<IActionResult> Index() => View(await store.Dashboard());
    public IActionResult Error() => View();
}
