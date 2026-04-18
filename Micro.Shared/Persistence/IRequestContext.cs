namespace Micro.Shared.Persistence;

public interface IRequestContext
{
    string Country { get; set; }
    OperationMode OperationMode { get; set; }
}
