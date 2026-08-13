namespace Niuro.Loans.Domain;

/// <summary>
/// Thrown when an operation would leave an entity in an invalid state.
/// The API layer maps this to a 400 response.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
