using Microsoft.AspNetCore.Mvc;
using MyTodo.Application.Commands.Todos;
using MyTodo.Application.Queries.Todos;

namespace MyTodo.Web.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosApiController : ControllerBase
{
    private readonly ITodoQueryService _queryService;
    private readonly CreateTodoCommandHandler _createHandler;

    public TodosApiController(ITodoQueryService queryService, CreateTodoCommandHandler createHandler)
    {
        _queryService  = queryService;
        _createHandler = createHandler;
    }

    // GET /api/todos
    [HttpGet]
    public async Task<ActionResult<TodoListResponse>> GetAll()
    {
        var items = await _queryService.GetAllAsync();
        return Ok(new TodoListResponse
        {
            Todos = items.Select(item =>
                new TodoResponseModel(item.Id, item.Title, item.Done, item.CreatedAt)).ToList()
        });
    }

    // POST /api/todos
    [HttpPost]
    public async Task<ActionResult> Create(TodoPostRequest request)
    {
        await _createHandler.HandleAsync(new CreateTodoCommand(request.Title));
        return Created();
    }

    public record TodoPostRequest(string Title);

    public record TodoListResponse
    {
        public required List<TodoResponseModel> Todos { get; init; }
    }

    public record TodoResponseModel(int Id, string Title, bool Done, DateTime CreatedAt);
}