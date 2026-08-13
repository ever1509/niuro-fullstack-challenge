namespace Niuro.Loans.Domain.Applications;

/// <summary>
/// An approved request for money. It points at its customer by id rather than holding a
/// reference: customer and application are separate aggregates, each saved on its own.
/// </summary>
public sealed class LoanApplication
{
    private const decimal MaximumRequestedAmount = 1_000_000m;

    private LoanApplication(Guid id, Guid customerId, decimal requestedAmount, DateTime submittedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        RequestedAmount = requestedAmount;
        SubmittedAtUtc = submittedAtUtc;
        UpdatedAtUtc = submittedAtUtc;
    }

    // Required by EF Core to materialise the entity; not for application code.
    private LoanApplication()
    {
    }

    public Guid Id { get; private init; }
    public Guid CustomerId { get; private init; }
    public decimal RequestedAmount { get; private set; }
    public DateTime SubmittedAtUtc { get; private init; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static LoanApplication Submit(Guid customerId, decimal requestedAmount, DateTime utcNow) =>
        new(Guid.NewGuid(), customerId, Valid(requestedAmount), utcNow);

    /// <summary>
    /// A returning customer amends the application they already have; we never open a second one.
    /// </summary>
    public void ChangeRequestedAmount(decimal requestedAmount, DateTime utcNow)
    {
        RequestedAmount = Valid(requestedAmount);
        UpdatedAtUtc = utcNow;
    }

    private static decimal Valid(decimal requestedAmount) => requestedAmount switch
    {
        <= 0 => throw new DomainException("Requested amount must be greater than zero."),
        > MaximumRequestedAmount => throw new DomainException(
            $"Requested amount must not exceed {MaximumRequestedAmount:N0}."),
        _ => requestedAmount
    };
}
