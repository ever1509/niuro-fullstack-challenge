# Architecture

## Layout

```
backend/src/
  Niuro.Loans.Domain           entities and value objects. No dependencies at all.
  Niuro.Loans.Application      the use case, the rule engine, and the ports it needs
  Niuro.Loans.Infrastructure   EF Core, repositories, the outbox and its dispatcher, HTTP client
  Niuro.Loans.Api              one controller, DTOs, composition root
backend/tests/                 unit tests and integration tests
frontend/                      Next.js: form, approved page, denied page
mock-external-service/         the partner system stand-in
```

References point one way: `Api -> Infrastructure -> Application -> Domain`. Domain references
nothing, not even a NuGet package. Application references Domain only, which is why its tests
run in milliseconds against hand-written fakes.

Anything Application needs from outside is an interface it declares and Infrastructure
implements: `ICustomerRepository`, `ILoanApplicationRepository`, `IUnitOfWork`,
`IEventPublisher`, `IBlacklistedSsnRegistry`. Replacing SQLite or HTTP means editing
`Infrastructure/DependencyInjection.cs` and nothing above it.

## Domain

`Customer` and `LoanApplication` are separate aggregates; the application holds a `CustomerId`
rather than a navigation property. Neither has a public setter. State changes go through
`UpdateDetails(...)` and `ChangeRequestedAmount(...)`, so invariants cannot be bypassed.

`Ssn` normalises to nine digits on construction. That is what makes "same SSN means the same
customer" work no matter how it was typed. Its `ToString()` is masked (`***-**-6789`) so a full
number cannot reach a log by accident.

Timestamps are passed in as parameters rather than read from `DateTime.UtcNow` inside entities.
Hidden clock access makes domain logic untestable; the application layer supplies the value from
`TimeProvider`.

## The rule engine

A rule is one class with one method:

```csharp
public interface IDenyRule
{
    Task<Denial?> EvaluateAsync(LoanApplicationSubmission submission, CancellationToken ct);
}
```

`null` means no objection. `LoanDecisionEngine` receives `IEnumerable<IDenyRule>` from DI and
knows nothing about any individual rule.

**To add a rule:** write the class, then register it in `Program.cs`:

```csharp
builder.Services.AddScoped<IDenyRule, YourNewRule>();
```

No existing rule and no existing test changes. `LoanDecisionEngineTests` proves this by inventing
a throwaway rule inside the test file and showing it takes effect.

The engine runs every rule instead of stopping at the first denial, so an applicant sees all the
reasons at once. With two rules that costs one extra lookup on an already-doomed application. If
rules became expensive, the fix is to order them cheapest-first and short-circuit.

Rules that need data get it through a port: `BlacklistedSsnRule` depends on
`IBlacklistedSsnRegistry`, not on a `DbContext`. The blacklist is a seeded table. The unserved
states are a constant inside `UnservedStateRule`; making that configurable would add an options
class and a config section to satisfy a requirement nobody has.

## The transaction

Saving the customer, saving the application and publishing the event must be one unit of work.

An HTTP call cannot be rolled back. Calling the partner inside the transaction means a later
database failure leaves them holding a record we do not have. Calling it after the commit leaves
a window where a crash loses the event permanently.

So no HTTP call happens inside the transaction. `IEventPublisher.Publish` returns `void` and does
no I/O; it only stages the event. Infrastructure implements it as an insert into
`OutboxMessages` using the same `DbContext` as the repositories:

```
BEGIN
  find customer by SSN
  new      -> insert Customer, insert LoanApplication, insert OutboxMessage(IsNewCustomer: true)
  existing -> update Customer, update LoanApplication, insert OutboxMessage(IsNewCustomer: false)
COMMIT                                      all three rows, or none of them
```

What commits atomically is the decision to send. Delivery happens afterwards.

- **Database fails:** everything rolls back. No customer, no application, no outbox row, so no
  event can ever be delivered. The caller gets a 500.
- **Outbox insert fails:** identical, it is the same transaction.
- **Delivery fails later:** nothing rolls back. The data stays, the message stays pending, the
  dispatcher retries. The applicant was already told they were approved, and that was true.

`TransactionRollbackAfterWriteTests` proves the rollback rather than asserting it. An interceptor
throws after `SaveChanges` has physically written all three rows; the test asserts three rows
were written, then that the database holds zero.

### Concurrency

Two simultaneous first-time submissions with the same SSN could in principle both miss the
lookup and both insert. Under SQLite they cannot: explicit transactions open at `Serializable`,
implemented as `BEGIN IMMEDIATE`, so the write lock is taken before the lookup. Verified with 40
concurrent identical submissions: all returned 200, leaving one customer, one application, one
create event and 39 update events.

The unique index on `Customers.Ssn` stays regardless. Under a database with row-level MVCC
(PostgreSQL at `ReadCommitted`) the race is real, and the index turns it into a loud error rather
than a duplicate customer. Handling it there would mean catching the violation and retrying the
use case once, which would then take the returning-customer path.

## The background event

`OutboxDispatcher` is a `BackgroundService`. Every two seconds it takes pending messages that are
due, sends them, and marks each delivered or failed. It runs outside the HTTP request, so the
applicant never waits for the partner.

The event carries identifiers, not a copy of the data:

```csharp
record LoanApplicationRecorded(Guid CustomerId, Guid ApplicationId, bool IsNewCustomer);
```

The dispatcher re-reads the records and sends their current state, so a retry after a later
submission still leaves the partner holding what we hold, rather than replaying a stale snapshot.

| Case | Request |
|---|---|
| First application | `POST /customers` with `customerId` in the body |
| Repeat application | `PUT /customers/{customerId}` |

The mock stores by `customerId`, so `POST` is an idempotent upsert. That matters because outbox
delivery is at-least-once: a crash between the partner returning 200 and our marking the message
delivered means we send again. At-least-once plus an idempotent receiver gives the right result.

The payload carries `ssnLast4`, never the full number. The partner keys on our customer id and
has no need for it. A test serialises the payload and asserts the nine digits appear nowhere.

Retries live in the outbox rather than in an HTTP retry policy. A Polly retry lives in memory, so
restarting the process mid-retry loses the event; an outbox row survives restarts. Backoff
doubles from 2 seconds up to 5 minutes. After 5 attempts a message stops being retried but is not
deleted: it stays unprocessed with its attempt count and last error, where someone can find it.

## API

One endpoint: `POST /api/loan-applications`.

A denial returns **200**, not 4xx, with `{ "decision": "Denied", "reasons": [...] }`. The request
was well formed and the system did what it was asked; denial is a business outcome, not a client
error. 400 is reserved for malformed input, produced by data annotations at the edge and by
`DomainExceptionHandler` for anything past them.

The controller maps request to command, calls the handler, maps the result. No business logic, no
`DbContext`, no `try/catch`.

## Frontend

Three routes: the form, `/approved`, `/denied`. The decision travels between them in
`sessionStorage` rather than the URL, since a loan outcome should not sit in a link that can be
bookmarked or logged by a proxy. It is read with `useSyncExternalStore`, React's primitive for
reading an external store without tearing during hydration.

Client-side zod validation duplicates the server rules deliberately. It exists for the person
typing, not for safety; the API validates independently because a form can be bypassed with curl.
The state field is a dropdown, so the rule never has to interpret `"new york"` versus `"NY"`.

## Trade-offs

Left out on purpose:

- **A message broker.** RabbitMQ would not have removed the outbox. Publishing to a broker inside
  a transaction is the same dual-write problem as calling HTTP inside one, so the outbox would
  still be needed and the broker would sit behind it. With one event, one consumer and one call
  it adds a container and two hops without changing any guarantee.
- **Structured logging on the request path.** Nothing records that an application was decided or
  why. In a real lending product that is an audit gap and the first thing I would add.
- **Docker and CI.** Each piece runs with one command and the tests need no services.
- **Frontend tests.** The brief names the rule engine, the returning-customer path and the
  endpoint. All three are covered where the behaviour lives.
- **An OpenAPI document.** The template's `Microsoft.AspNetCore.OpenApi` package was removed; it
  pulls `Microsoft.OpenApi` 2.0.0, which carries a known high-severity advisory (NU1903), and
  nothing used it.
- **MediatR, CQRS, generic repositories, AutoMapper**, and an interface for `LoanDecisionEngine`.
  One use case does not need a mediator, and the engine has one implementation whose tests use it
  directly with fake rules.

Convenience choices that would change in production:

- Migrations are applied at startup so a fresh clone needs only `dotnet run`.
- SQLite, for the same reason. It has real transactions, and write-ahead logging is enabled at
  startup because the dispatcher writes from a background thread while requests are writing.
- SSNs are stored in plaintext. A real system would encrypt them at rest.
- A second API instance would poll the same outbox and could deliver twice. The idempotent
  receiver keeps that correct, but production would want `SELECT ... FOR UPDATE SKIP LOCKED`.
