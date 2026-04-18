namespace Micro.Shared.Persistence;

public class RequestContext : IRequestContext
{
    public string Country { get; set; } = "Egypt";
    public OperationMode OperationMode { get; set; } = OperationMode.Write;
}
