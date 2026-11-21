namespace MediaPilot.Core;

/// <summary>
/// Describes the track that is being played right now.
/// </summary>
/// <param name="Title">Title of the track.</param>
/// <param name="Artist">Artist of the track, empty when the media session does not publish it.</param>
/// <remarks>
/// It is a plain domain type on purpose: the media session API of Windows
/// (SMTC) stays inside the platform layer and never reaches the domain.
/// </remarks>
public sealed record MediaTrack(string Title, string Artist)
{
    /// <summary>
    /// Gets a value indicating whether the track carries no usable information.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Artist);
}
