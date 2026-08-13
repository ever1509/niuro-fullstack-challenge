namespace Niuro.Loans.Application.Events;

/// <summary>
/// A loan application was approved and written to our database. Something else — the
/// external service — now needs to hear about it.
/// <para>
/// The event carries identifiers rather than a copy of the data. Whoever delivers it reads
/// the current state at send time, so a delivery that is retried after a later submission
/// still leaves the external service holding what our database holds.
/// </para>
/// </summary>
public sealed record LoanApplicationRecorded(Guid CustomerId, Guid ApplicationId, bool IsNewCustomer);
