namespace Niuro.Loans.Domain.Customers;

/// <summary>
/// A Social Security Number, stored as its nine digits with any formatting removed.
/// Normalising on construction is what makes "same SSN means the same customer" reliable:
/// "123-45-6789" and "123456789" are the same person.
/// </summary>
public sealed record Ssn
{
    private Ssn(string value) => Value = value;

    /// <summary>The nine digits, without separators.</summary>
    public string Value { get; }

    public string Last4 => Value[^4..];

    public static Ssn Create(string? input)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 9)
        {
            throw new DomainException("SSN must contain exactly 9 digits.");
        }

        return new Ssn(digits);
    }

    /// <summary>
    /// Masked on purpose: an SSN that reaches a log or an exception message is a leak,
    /// so the type refuses to render itself in full.
    /// </summary>
    public override string ToString() => $"***-**-{Last4}";
}
