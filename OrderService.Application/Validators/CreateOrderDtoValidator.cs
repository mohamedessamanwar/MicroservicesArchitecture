using FluentValidation;
using OrderService.Application.DTOs;

namespace OrderService.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId)
        .NotEmpty()
        .WithMessage("CustomerId is required");

        RuleFor(x => x.TotalAmount)
        .GreaterThan(0)
        .WithMessage("TotalAmount must be greater than zero");
    }
}