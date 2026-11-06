using MedStore.Data;
using MedStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;

[Authorize, Route("modules")]
public sealed class ModulesController(DemoStore store) : Controller
{
    [HttpGet("{module}")]
    public async Task<IActionResult> Index(string module, string? q)
    {
        if (!Allowed(module, out var definition)) return NotFound();
        if (!HasModuleRole(definition.Role)) return Forbid();
        var page = await store.ModulePage(module, q); ViewData["Title"] = page.Title; return View(page);
    }

    [HttpGet("{module}/new")]
    public IActionResult Create(string module)
    {
        if (!Allowed(module, out var definition)) return NotFound(); if (!HasModuleRole(definition.Role)) return Forbid();
        return Form(module, definition.Title, new BusinessRecord { Date=DateTime.Today, StartDate=DateTime.Today, EndDate=DateTime.Today });
    }

    [HttpGet("{module}/{id:int}/edit")]
    public async Task<IActionResult> Edit(string module, int id)
    {
        if (!Allowed(module, out var definition)) return NotFound(); if (!HasModuleRole(definition.Role)) return Forbid();
        var row=await store.Record(module,id);return row is null?NotFound():Form(module,definition.Title,row);
    }

    [HttpGet("leaves/calendar")]
    public async Task<IActionResult> LeaveCalendar(DateTime? month)
    {if(!HasModuleRole(DemoStore.ModuleDefinitions["leaves"].Role))return Forbid();var vm=await store.LeaveCalendar(month??DateTime.Today);ViewData["Title"]="Employee leave calendar";return View(vm);}

    [HttpPost("{module}/save"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string module, BusinessRecord record)
    {
        if (!Allowed(module,out var definition)) return NotFound(); if (!HasModuleRole(definition.Role)) return Forbid();
        if (!ModelState.IsValid) return Form(module,definition.Title,record);
        try { await store.SaveRecord(module,record); TempData["Notice"]="Changes saved."; return RedirectToAction(nameof(Index),new{module}); }
        catch(Exception ex) when(ex is InvalidOperationException or KeyNotFoundException) { ModelState.AddModelError("",ex.Message); return Form(module,definition.Title,record); }
    }

    [HttpPost("{module}/{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string module,int id)
    {
        if (!Allowed(module,out var definition)) return NotFound(); if (!HasModuleRole(definition.Role)) return Forbid();
        try { await store.DeleteRecord(module,id); TempData["Notice"]="Record deleted."; }
        catch(InvalidOperationException ex) { TempData["Error"]=ex.Message; }
        return RedirectToAction(nameof(Index),new{module});
    }

    [HttpPost("leaves/{id:int}/review"), ValidateAntiForgeryToken, Authorize(Roles="Admin")]
    public async Task<IActionResult> ReviewLeave(int id,string decision)
    {
        if(decision is not ("Approved" or "Rejected")) return BadRequest();
        try{await store.SetLeaveStatus(id,decision);TempData["Notice"]=$"Leave {decision.ToLowerInvariant()}.";}catch(KeyNotFoundException){return NotFound();}
        return RedirectToAction(nameof(Index),new{module="leaves"});
    }

    [HttpPost("payroll/generate"),ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> GeneratePayroll(DateTime period)
    { try{await store.GeneratePayroll(period);TempData["Notice"]="Payroll generated for the selected month.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index),new{module="payroll"}); }

    [HttpGet("payroll/{id:int}/slip"),Authorize(Roles="Admin")]
    public async Task<IActionResult> PayrollSlip(int id){var slip=await store.PayrollSlip(id);return slip is null?NotFound():View("PayrollSlip",slip);}

    private IActionResult Form(string module,string title,BusinessRecord record)
    {
        if (!Allowed(module,out var definition)) return NotFound();
        ViewData["Title"] = record.Id==0?"Add · "+title:"Edit · "+title;
        return View("Form",new ModuleFormViewModel{Key=module.ToLowerInvariant(),Title=title,Fields=definition.Fields.ToList(),Record=record});
    }
    private bool Allowed(string module,out (string Title,string Description,string Role,ModuleField[] Fields) definition)=>DemoStore.ModuleDefinitions.TryGetValue(module,out definition);
    private bool HasModuleRole(string roles)=>User.IsInRole("Admin")||roles.Split(',').Any(User.IsInRole);
}
