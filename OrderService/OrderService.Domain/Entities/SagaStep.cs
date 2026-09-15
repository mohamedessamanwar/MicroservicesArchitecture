namespace OrderService.Domain.Entities;

public enum SagaStepStatus
{
     Pending,
     Executing,
     Completed,
     Failed,
}

public enum CompensationStatus
{
     None,
     Pending,
     Executing,
     Completed,
     Failed
}

public class SagaStep
{
     public Guid Id { get; set; }
     public Guid SagaId { get; set; }
     public Saga Saga { get; set; } = null!;

     public string StepName { get; set; } = string.Empty;
     public SagaStepStatus Status { get; set; } = SagaStepStatus.Pending;
     public CompensationStatus CompensationStatus { get; set; } = CompensationStatus.None;

     public int Attempts { get; set; }
     public string? ErrorMessage { get; set; }

     // Store JSON data needed for retry or compensation
     public string? Payload { get; set; }
}
