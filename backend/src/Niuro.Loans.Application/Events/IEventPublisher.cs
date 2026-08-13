namespace Niuro.Loans.Application.Events;

public interface IEventPublisher
{
    /// <summary>
    /// Records that the event must be delivered. Deliberately synchronous and without I/O:
    /// this only stages the event alongside the data that caused it, so the surrounding
    /// transaction decides whether it ever happened. Actual delivery occurs later,
    /// outside the request.
    /// </summary>
    void Publish(LoanApplicationRecorded @event);
}
