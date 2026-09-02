namespace AkaKraft.Application.Interfaces;

/// <summary>
/// Zugriff auf die API des Parkraum-Bewirtschafters (campusparken.de).
/// Login-Token werden pro Parkkonto zwischengespeichert.
/// </summary>
public interface ICampusParkenClient
{
    /// <summary>
    /// Aktuell im Portal hinterlegte Kennzeichen des Kontos.
    /// <paramref name="allowCache"/> = true nutzt einen kurzlebigen In-Memory-Cache (für Anzeige);
    /// für Änderungen (read-modify-write) mit false den aktuellen Stand erzwingen.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(Guid accountId, string username, string password, bool allowCache = true, CancellationToken ct = default);

    /// <summary>Ersetzt die komplette Kennzeichenliste des Kontos im Portal.</summary>
    Task ReplaceAsync(Guid accountId, string username, string password, IReadOnlyList<string> kennzeichen, CancellationToken ct = default);
}

/// <summary>Fehler bei der Kommunikation mit dem Bewirtschafter-Portal.</summary>
public class CampusParkenException(string message) : Exception(message);
