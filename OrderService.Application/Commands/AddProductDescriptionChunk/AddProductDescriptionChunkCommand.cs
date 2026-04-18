using MediatR;

namespace OrderService.Application.Commands.AddProductDescriptionChunk
{
    public class AddProductDescriptionChunkCommand : IRequest<bool>
    {
        public required AddProductDescriptionChunkDto Product { get; set; }
    }

    public class AddProductDescriptionChunkDto
    {
        public Guid ProductId { get; set; }
        public required string Text { get; set; }
    }

    public class AddProductDescriptionChunkCommandHandler : IRequestHandler<AddProductDescriptionChunkCommand, bool>
    {
        public Task<bool> Handle(AddProductDescriptionChunkCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Handler] Received AddProductDescriptionChunkCommand. ID: {request.Product.ProductId}, Text: {request.Product.Text}");
            return Task.FromResult(true);
        }
    }
}