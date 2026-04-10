namespace MyTodo.Domain.Category;

public record CategoryId(int Value);
public record CategoryName(string Value);
public record CategoryCreatedAt(DateTime Value);

public record Category(
    CategoryId Id,
    CategoryName Name,
    CategoryCreatedAt CreatedAt);
