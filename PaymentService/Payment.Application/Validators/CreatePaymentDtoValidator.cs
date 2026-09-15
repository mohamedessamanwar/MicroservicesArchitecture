using FluentValidation;
using Payment.Application.DTOs;

namespace Payment.Application.Validators;

public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.OrderId)
        .NotEmpty()
                .WithMessage("OrderId is required");

        RuleFor(x => x.Amount)
          .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");
    }
}