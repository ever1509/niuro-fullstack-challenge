namespace Niuro.Loans.Domain.Customers;

/// <summary>
/// A US postal address. <see cref="State"/> is normalised to a two-letter uppercase code
/// because the eligibility rules are decided on it.
/// </summary>
public sealed record Address
{
    private Address(string street, string city, string state, string postalCode)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
    }

    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }

    public static Address Create(string? street, string? city, string? state, string? postalCode)
    {
        var normalisedState = (state ?? string.Empty).Trim().ToUpperInvariant();

        if (normalisedState.Length != 2 || !normalisedState.All(char.IsAsciiLetter))
        {
            throw new DomainException("State must be a two-letter US state code.");
        }

        return new Address(
            Required(street, nameof(street)),
            Required(city, nameof(city)),
            normalisedState,
            Required(postalCode, nameof(postalCode)));
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainException($"Address {field} is required.")
            : value.Trim();

    public override string ToString() => $"{Street}, {City}, {State} {PostalCode}";
}
