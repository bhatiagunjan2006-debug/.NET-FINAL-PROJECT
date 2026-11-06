using MedStore.Data;
using MedStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[Authorize,Route("transactions")]
public sealed class TransactionsController(DemoStore store):Controller
{
    [HttpGet("{kind}")] public async Task<IActionResult> Index(string kind,string? q)
    {
        if(kind is not ("sale" or "purchase"))return NotFound();
        if(kind=="purchase"&&!User.IsInRole("Admin")&&!User.IsInRole("Pharmacist"))return Forbid();
        var vm=await store.TransactionPage(kind,q);ViewBag.Search=q;ViewData["Title"]=kind=="sale"?"Sales & billing":"Purchase management";return View(vm);
    }
    [HttpGet("{kind}/{id:int}/invoice")] public async Task<IActionResult> Invoice(string kind,int id)
    {if(kind is not ("sale" or "purchase"))return NotFound();if(kind=="purchase"&&!User.IsInRole("Admin")&&!User.IsInRole("Pharmacist"))return Forbid();var invoice=await store.Invoice(kind,id);return invoice is null?NotFound():View(invoice);}
    [HttpPost("{kind}/create"),ValidateAntiForgeryToken] public async Task<IActionResult> Create(string kind,TransactionInput input)
    {
        if(kind is not ("sale" or "purchase"))return NotFound();if(kind=="purchase"&&!User.IsInRole("Admin")&&!User.IsInRole("Pharmacist"))return Forbid();
        if(!ModelState.IsValid){TempData["Error"]="Check item quantities and discount values.";return RedirectToAction(nameof(Index),new{kind});}
        input.Kind=kind;
        try{await store.CreateTransaction(input);TempData["Notice"]=kind=="sale"?"Sale saved and stock reduced.":"Purchase saved and stock increased.";}
        catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}
        return RedirectToAction(nameof(Index),new{kind});
    }
    [HttpPost("sale/{id:int}/cancel"),ValidateAntiForgeryToken,Authorize(Roles="Admin,Pharmacist")] public async Task<IActionResult> Cancel(int id)
    {try{await store.CancelSale(id);TempData["Notice"]="Sale cancelled and stock restored.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index),new{kind="sale"});}
}
