using Niuro.Loans.Application.Decisions;
using Niuro.Loans.Application.Events;
using Niuro.Loans.Application.Persistence;
using Niuro.Loans.Domain.Applications;
using Niuro.Loans.Domain.Customers;

namespace Niuro.Loans.Application.LoanApplications;

/// <summary>
/// The one use case in this system: decide a submission and, if it survives the rules,
/// record it.
/// </summary>
public sealed class SubmitLoanApplicationHandler(
    LoanDecisionEngine decisionEngine,
    ICustomerRepository customers,
    ILoanApplicationRepository applications,
    IEventPublisher events,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SubmitLoanApplicationResult> HandleAsync(
        SubmitLoanApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var submission = ToSubmission(command);

        var decision = await decisionEngine.DecideAsync(submission, cancellationToken);

        // A denied application leaves no trace: nothing is written and nothing is published.
        if (!decision.IsApproved)
        {
            return SubmitLoanApplicationResult.Denied(decision.Denials);
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            token => RecordAsync(submission, token),
            cancellationToken);
    }

    private async Task<SubmitLoanApplicationResult> RecordAsync(
        LoanApplicationSubmission submission,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var existingCustomer = await customers.FindBySsnAsync(submission.Ssn, cancellationToken);
        var isNewCustomer = existingCustomer is null;

        var customer = isNewCustomer
            ? Register(submission, utcNow)
            : Update(existingCustomer!, submission, utcNow);

        var application = await AmendOrOpenApplicationAsync(
            customer.Id,
            submission.RequestedAmount,
            isNewCustomer,
            utcNow,
            cancellationToken);

        // Staged, not sent. It becomes real only if the transaction around this commits.
        events.Publish(new LoanApplicationRecorded(customer.Id, application.Id, isNewCustomer));

        return SubmitLoanApplicationResult.Approved(
            customer.Id,
            application.Id,
            isReturningCustomer: !isNewCustomer);
    }

    private Customer Register(LoanApplicationSubmission submission, DateTime utcNow)
    {
        var customer = Customer.Register(
            submission.Ssn,
            submission.FirstName,
            submission.LastName,
            submission.Address,
            submission.CompanyName,
            utcNow);

        customers.Add(customer);
        return customer;
    }

    private static Customer Update(Customer customer, LoanApplicationSubmission submission, DateTime utcNow)
    {
        customer.UpdateDetails(
            submission.FirstName,
            submission.LastName,
            submission.Address,
            submission.CompanyName,
            utcNow);

        return customer;
    }

    private async Task<LoanApplication> AmendOrOpenApplicationAsync(
        Guid customerId,
        decimal requestedAmount,
        bool isNewCustomer,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // A known customer normally has an application to amend. The lookup is skipped for a
        // brand new customer, and still guarded for the case where a customer somehow has none.
        var existing = isNewCustomer
            ? null
            : await applications.FindByCustomerIdAsync(customerId, cancellationToken);

        if (existing is null)
        {
            var opened = LoanApplication.Submit(customerId, requestedAmount, utcNow);
            applications.Add(opened);
            return opened;
        }

        existing.ChangeRequestedAmount(requestedAmount, utcNow);
        return existing;
    }

    private static LoanApplicationSubmission ToSubmission(SubmitLoanApplicationCommand command) =>
        new(
            Ssn.Create(command.Ssn),
            command.FirstName,
            command.LastName,
            Address.Create(command.Street, command.City, command.State, command.PostalCode),
            command.CompanyName,
            command.RequestedAmount);
}
