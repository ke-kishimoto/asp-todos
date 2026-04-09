using Microsoft.AspNetCore.Mvc;

namespace MyTodo.Web.Controllers;

[Route("blazor/todos")]
public class BlazorTodosController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Todos (Blazor Server)";
        return View();
    }
}
