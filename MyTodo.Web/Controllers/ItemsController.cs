using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTodo.Application.Commands.Items;
using MyTodo.Application.Queries.Items;
using MyTodo.Web.Models;

namespace MyTodo.Web.Controllers;

[Authorize]
[Route("mvc/items")]
public class ItemsController : Controller
{
    private readonly IItemQueryService _queryService;
    private readonly CreateItemCommandHandler _createHandler;

    public ItemsController(IItemQueryService queryService, CreateItemCommandHandler createHandler)
    {
        _queryService  = queryService;
        _createHandler = createHandler;
    }

    // GET /mvc/items
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _queryService.GetAllAsync();
        var viewModels = items.Select(item => new ItemViewModel(item)).ToList();
        ViewData["UserName"] = User.Identity?.Name ?? "（不明）";
        return View(viewModels);
    }

    // GET /mvc/items/create
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new ItemCreateViewModel());
    }

    // POST /mvc/items/create
    [HttpPost("create")]
    public async Task<IActionResult> Create(ItemCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _createHandler.HandleAsync(new CreateItemCommand(model.ItemCode, model.ItemName, model.Price));

        return RedirectToAction(nameof(Index));
    }
}