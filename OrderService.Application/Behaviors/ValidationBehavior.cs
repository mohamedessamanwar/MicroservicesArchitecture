using FluentValidation;
using MediatR;
using OrderService.Application.Common;

namespace OrderService.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var errorMessages = failures.Select(f => f.ErrorMessage);
                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(CommandResult<>))
                {
                    var resultType = typeof(TResponse);
                    var failMethod = resultType.GetMethod("Fail", new[] { typeof(IEnumerable<string>) });

                    if (failMethod != null)
                    {
                        return (TResponse)failMethod.Invoke(null, new object[] { errorMessages })!;
                    }
                }
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}