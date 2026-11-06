using System.Text;
using MedStore.Data;
using MedStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[Authorize]
public sealed class MedicinesController(DemoStore store) : Controller
{
    [Authorize(Roles="Admin,Pharmacist,Billing Staff")]
    public async Task<IActionResult> Index(string? q,int? categoryId,string? manufacturer,DateTime? expiryBefore,string? sort="name",int page=1)
    {
        var all=await Filtered(q,categoryId,manufacturer,expiryBefore,sort);
        const int pageSize=20;page=Math.Max(1,page);var vm=new MedicineListViewModel{Categories=await store.Categories(),Search=q,CategoryId=categoryId,Manufacturer=manufacturer,ExpiryBefore=expiryBefore,Sort=sort??"name",TotalCount=all.Count,TotalPages=Math.Max(1,(int)Math.Ceiling(all.Count/(double)pageSize)),Page=page,Items=all.Skip((page-1)*pageSize).Take(pageSize).ToList()};
        ViewData["Title"]="Medicine management";return View(vm);
    }
    [Authorize(Roles="Admin,Pharmacist")] public async Task<IActionResult> Create(){await FillCategories();return View("Edit",new Medicine{ExpiryDate=DateTime.Today.AddYears(1)});}
    [Authorize(Roles="Admin,Pharmacist")] public async Task<IActionResult> Edit(int id){var m=await store.Medicine(id);if(m is null)return NotFound();await FillCategories();return View(m);}
    [Authorize(Roles="Admin,Pharmacist"),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Medicine model)
    {
        if(model.ExpiryDate.Date<DateTime.Today)ModelState.AddModelError(nameof(model.ExpiryDate),"Expiry date must be today or later.");
        if(model.SellingPrice<=0)ModelState.AddModelError(nameof(model.SellingPrice),"Selling price must be greater than zero.");
        if(!ModelState.IsValid){await FillCategories();return View("Edit",model);}
        try{await store.SaveMedicine(model);TempData["Notice"]="Medicine saved.";return RedirectToAction(nameof(Index));}
        catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);await FillCategories();return View("Edit",model);}
    }
    [Authorize(Roles="Admin,Pharmacist"),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id){try{await store.DeleteMedicine(id);}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index));}
    [Authorize(Roles="Admin,Pharmacist,Billing Staff")]
    public async Task<IActionResult> Export(string? q,int? categoryId,string? manufacturer,DateTime? expiryBefore,string? sort="name")
    {
        var rows=await Filtered(q,categoryId,manufacturer,expiryBefore,sort);var sb=new StringBuilder("Id,Medicine,Category,Manufacturer,Batch,SupplierId,PurchasePrice,SellingPrice,Quantity,ExpiryDate,Description\r\n");
        foreach(var m in rows)sb.AppendLine(string.Join(',',new[]{m.Id.ToString(),m.Name,m.CategoryName??"",m.Manufacturer??"",m.BatchNumber,m.SupplierId?.ToString()??"",m.PurchasePrice.ToString("0.00"),m.SellingPrice.ToString("0.00"),m.Quantity.ToString(),m.ExpiryDate.ToString("yyyy-MM-dd"),m.Description??""}.Select(Csv)));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),"text/csv","medistock-medicines.csv");
    }
    private async Task<List<Medicine>> Filtered(string? q,int? categoryId,string? manufacturer,DateTime? expiryBefore,string? sort)
    {
        var rows=await store.Medicines(q);if(categoryId.HasValue)rows=rows.Where(m=>m.CategoryId==categoryId).ToList();if(!string.IsNullOrWhiteSpace(manufacturer))rows=rows.Where(m=>(m.Manufacturer??"").Contains(manufacturer,StringComparison.OrdinalIgnoreCase)).ToList();if(expiryBefore.HasValue)rows=rows.Where(m=>m.ExpiryDate.Date<=expiryBefore.Value.Date).ToList();
        return (sort??"name").ToLowerInvariant() switch{"expiry"=>rows.OrderBy(m=>m.ExpiryDate).ToList(),"quantity"=>rows.OrderBy(m=>m.Quantity).ToList(),"price"=>rows.OrderBy(m=>m.SellingPrice).ToList(),_=>rows.OrderBy(m=>m.Name).ToList()};
    }
    private async Task FillCategories()=>ViewBag.Categories=await store.Categories();
    private static string Csv(string value)=>"\""+(value.StartsWith('=')||value.StartsWith('+')||value.StartsWith('-')||value.StartsWith('@')?"'":"")+value.Replace("\"","\"\"")+"\"";
}
