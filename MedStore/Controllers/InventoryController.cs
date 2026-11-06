using MedStore.Data;
using MedStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[Authorize,Route("inventory")]
public sealed class InventoryController(DemoStore store):Controller
{
    [HttpGet] public async Task<IActionResult> Index(){ViewData["Title"]="Inventory management";ViewBag.Movements=await store.Movements();ViewBag.Medicines=await store.Medicines();return View();}
    [HttpPost("adjust"),ValidateAntiForgeryToken,Authorize(Roles="Admin,Pharmacist")] public async Task<IActionResult> Adjust(StockAdjustmentInput input)
    {if(!ModelState.IsValid){TempData["Error"]="Enter a medicine, a nonzero stock change, and a reason.";return RedirectToAction(nameof(Index));}try{if(input.Change==0)throw new InvalidOperationException("Stock change cannot be zero.");await store.AdjustStock(input.MedicineId,input.Change,input.Note);TempData["Notice"]="Stock adjustment recorded.";}catch(Exception ex)when(ex is InvalidOperationException or KeyNotFoundException){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index));}
}
