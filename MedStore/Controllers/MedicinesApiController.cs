using MedStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStore.Controllers;
[ApiController,Route("api/medicines"),Authorize(Roles="Admin,Pharmacist,Billing Staff")]
public sealed class MedicinesApiController(DemoStore store):ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get([FromQuery]string? search)=>Ok(await store.Medicines(search));
}
