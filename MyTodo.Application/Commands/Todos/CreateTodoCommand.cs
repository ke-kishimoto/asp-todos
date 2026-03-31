using MyTodo.Application.Repositories;
using MyTodo.Domain.Todo;

namespace MyTodo.Application.Commands.Todos;

public record CreateTodoCommand(string Title);

public class CreateTodoCommandHandler
{
    private readonly ITodoRepository _repo;

    public CreateTodoCommandHandler(ITodoRepository repo)
    {
        _repo = repo;
    }

    public async Task<TodoItem> HandleAsync(CreateTodoCommand command)
    {
        return await _repo.AddAsync(command.Title.Trim());
    }
}
