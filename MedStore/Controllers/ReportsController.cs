using MedStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[Authorize(Roles="Admin,Pharmacist"),Route("reports")]
public sealed class ReportsController(DemoStore store):Controller
{
    [HttpGet] public async Task<IActionResult> Index(){ViewData["Title"]="Reports & analytics";ViewBag.Dashboard=await store.Dashboard();ViewBag.Sales=await store.AllSales();return View();}
}
