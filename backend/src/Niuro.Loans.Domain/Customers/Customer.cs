namespace Niuro.Loans.Domain.Customers;

/// <summary>
/// The person applying. Identified in the business by <see cref="Ssn"/>, which is why
/// a second submission with the same SSN updates this record instead of creating another.
/// </summary>
public sealed class Customer
{
    private Customer(
        Guid id,
        Ssn ssn,
        string firstName,
        string lastName,
        Address address,
        string companyName,
        DateTime registeredAtUtc)
    {
        Id = id;
        Ssn = ssn;
        FirstName = firstName;
        LastName = lastName;
        Address = address;
        CompanyName = companyName;
        RegisteredAtUtc = registeredAtUtc;
        UpdatedAtUtc = registeredAtUtc;
    }

    // Required by EF Core to materialise the entity; not for application code.
    private Customer()
    {
        Ssn = null!;
        FirstName = null!;
        LastName = null!;
        Address = null!;
        CompanyName = null!;
    }

    public Guid Id { get; private init; }
    public Ssn Ssn { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Address Address { get; private set; }
    public string CompanyName { get; private set; }
    public DateTime RegisteredAtUtc { get; private init; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Customer Register(
        Ssn ssn,
        string firstName,
        string lastName,
        Address address,
        string companyName,
        DateTime utcNow) =>
        new(
            Guid.NewGuid(),
            ssn,
            Required(firstName, "First name"),
            Required(lastName, "Last name"),
            address,
            Required(companyName, "Company name"),
            utcNow);

    /// <summary>
    /// Applies the latest submission to an existing customer. The SSN is not updated:
    /// it is how we found this record in the first place.
    /// </summary>
    public void UpdateDetails(
        string firstName,
        string lastName,
        Address address,
        string companyName,
        DateTime utcNow)
    {
        FirstName = Required(firstName, "First name");
        LastName = Required(lastName, "Last name");
        Address = address;
        CompanyName = Required(companyName, "Company name");
        UpdatedAtUtc = utcNow;
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainException($"{field} is required.")
            : value.Trim();
}
