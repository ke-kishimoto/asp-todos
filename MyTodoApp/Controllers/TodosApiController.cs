using Microsoft.AspNetCore.Mvc;
using MyTodoApp.Models;
using MyTodoApp.Services;
using System.Net;
using System.Reflection;

namespace MyTodoApp.Controllers;

// -----------------------------------------------------------------------
// 【Web API版】Todos コントローラー
//
// ★ 変更点：InMemoryTodoRepository → ITodoService に切り替え
//   かつ 非同期（async/await）に変更
// -----------------------------------------------------------------------
[ApiController]
[Route("api/todos")]
public class TodosApiController : ControllerBase
{
    private readonly ITodoService _service;

    public TodosApiController(ITodoService service)
    {
        _service = service;
    }

    // GET /api/todos
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoListResponse>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(new TodoListResponse { Todos = items.ToList() });
    }

    // POST /api/todos
    [HttpPost]
    public async Task<ActionResult<HttpResponse>> Create(TodoPostRequest request)
    {        
        await _service.CreateAsync(request.Title);
        return Created();
    }

    public record TodoPostRequest(string Title)
    {

    }

    public record TodoListResponse
    {
        public required List <TodoItem> Todos { get; init; }
    }
}
