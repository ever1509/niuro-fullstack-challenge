using Microsoft.AspNetCore.Mvc;
using Niuro.Loans.Api.Contracts;
using Niuro.Loans.Application.LoanApplications;

namespace Niuro.Loans.Api.Controllers;

[ApiController]
[Route("api/loan-applications")]
public sealed class LoanApplicationsController(SubmitLoanApplicationHandler handler) : ControllerBase
{
    /// <summary>
    /// Submits an application and returns the decision.
    /// <para>
    /// A denial answers 200, not 4xx: the request was well formed and the system did exactly
    /// what it was asked to. "Denied" is an outcome, not an error, and the caller reads it
    /// from the body.
    /// </para>
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SubmitLoanApplicationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitLoanApplicationResponse>> Submit(
        SubmitLoanApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanApplicationCommand(
            request.FirstName,
            request.LastName,
            request.Street,
            request.City,
            request.State,
            request.PostalCode,
            request.CompanyName,
            request.RequestedAmount,
            request.Ssn);

        var result = await handler.HandleAsync(command, cancellationToken);

        return Ok(SubmitLoanApplicationResponse.From(result));
    }
}
