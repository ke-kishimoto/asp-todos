using Microsoft.AspNetCore.Mvc;

namespace MyTodo.Web.Controllers;

[Route("blazor/orders")]
public class BlazorOrdersController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Orders (Blazor Server)";
        return View();
    }
}
