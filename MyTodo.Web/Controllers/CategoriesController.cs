using Microsoft.AspNetCore.Mvc;

namespace MyTodo.Web.Controllers;

[Route("mvc/categories")]
public class CategoriesController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
