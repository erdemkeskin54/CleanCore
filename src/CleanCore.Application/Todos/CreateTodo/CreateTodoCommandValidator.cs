using FluentValidation;

namespace CleanCore.Application.Todos.CreateTodo;

// Title boş olmasın + DB sütun limit'ini aşmasın.
// Frontend tarafında Zod ile aynı kurallar (createTodoSchema.ts) — defense in depth.
public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Görev başlığı boş olamaz.")
            .MaximumLength(200).WithMessage("Görev başlığı en fazla 200 karakter olabilir.");
    }
}
