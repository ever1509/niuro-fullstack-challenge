# Niuro Loans

**Video walkthrough:** https://www.loom.com/share/894ffef780a74d81b0bc5c336da5c5e8

A loan application flow. Someone fills in a form, a rule engine decides whether to approve it,
approved applications are stored, and a background event forwards them to an external service.

Applying twice with the same SSN updates the existing customer and application rather than
creating a second one, both in our database and in the external service.

| Part | Technology | URL |
|---|---|---|
| Backend | .NET 10, EF Core, SQLite | http://localhost:5207 |
| Frontend | Next.js 16, TypeScript, Tailwind | http://localhost:3000 |
| External service (mock) | Node, no dependencies | http://localhost:4000 |

Design decisions and trade-offs are in [ARCHITECTURE.md](ARCHITECTURE.md).

## Prerequisites

- .NET SDK 10.0 (`dotnet --version`)
- Node.js 20 or newer (`node --version`)

Nothing else. The database is a SQLite file created on first run, and the mock service has no
npm dependencies.

## Running it locally

Three terminals, in this order.

**1. External service mock**

```bash
cd mock-external-service && node server.js
```

**2. Backend.** Creates and migrates `loans.db` on startup.

```bash
cd backend/src/Niuro.Loans.Api && dotnet run
```

**3. Frontend**

```bash
cd frontend && npm install && npm run dev
```

Then open http://localhost:3000.

The backend runs fine without the mock service: deliveries simply stay pending in the outbox
and retry. Start the mock at any point and the queued events go out.

## Running the tests

```bash
cd backend && dotnet test
```

41 tests: 23 unit (rules, decision engine, use case) and 18 integration (endpoint, returning
customer, transaction rollback, outbox delivery and retries). They need no running services;
the integration tests boot the API against a throwaway SQLite file.

## Test data

Three SSNs are seeded as blacklisted by the initial migration:

| SSN | Reason |
|---|---|
| `666-55-4444` | Known fraud ring |
| `111-11-1111` | Identity reported stolen |
| `999-99-9999` | Sanctions list match |

To produce each outcome:

| To get | Enter |
|---|---|
| **Approved** | Any SSN not listed above, any state except NY. For example `123-45-6789`, CA |
| **Returning customer** | Submit an approved application, then submit again with the **same SSN** and a different amount |
| **Denied by state** | Any unlisted SSN with state **NY** |
| **Denied by blacklist** | `666-55-4444` with any state except NY |
| **Denied for both reasons** | `666-55-4444` with state **NY** |

SSNs are matched on their digits, so `123-45-6789` and `123456789` are the same person. Typing
either one on a second submission is recognised as a returning customer.

### Watching the external service

The mock logs every write. `GET http://localhost:4000/customers` shows everything it currently
holds. A first application arrives as `created`, a repeat application as `updated` against the
same record.

To see delivery retries, tell the mock to reject the next two writes and then submit:

```bash
curl -X POST "http://localhost:4000/__control/fail-next?count=2"
```

The applicant still gets an immediate answer; the event retries with exponential backoff and
lands a few seconds later.

### Inspecting the database

```bash
sqlite3 backend/src/Niuro.Loans.Api/loans.db "SELECT Ssn, FirstName, CompanyName FROM Customers; SELECT RequestedAmount FROM LoanApplications; SELECT Type, Attempts, ProcessedAtUtc FROM OutboxMessages;"
```

## Configuration

Backend settings live in `backend/src/Niuro.Loans.Api/appsettings.json`:

| Setting | Default | Purpose |
|---|---|---|
| `ConnectionStrings:LoansDatabase` | `Data Source=loans.db` | SQLite file |
| `Cors:AllowedOrigins` | `http://localhost:3000` | Origins allowed to call the API |
| `Outbox:BaseUrl` | `http://localhost:4000` | External service address |
| `Outbox:PollingInterval` | `00:00:02` | How often pending events are checked |
| `Outbox:MaxAttempts` | `5` | Attempts before a message is left for inspection |

The frontend reads `NEXT_PUBLIC_API_BASE_URL` (see `frontend/.env.example`) and defaults to
`http://localhost:5207`.

## What is not here

Authentication, which the brief excludes. No Docker, CI, or structured logging. See the
trade-offs section of [ARCHITECTURE.md](ARCHITECTURE.md) for what was left out and why.
