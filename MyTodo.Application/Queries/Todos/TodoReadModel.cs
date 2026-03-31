namespace MyTodo.Application.Queries.Todos;

public record TodoReadModel(int Id, string Title, bool Done, DateTime CreatedAt);
