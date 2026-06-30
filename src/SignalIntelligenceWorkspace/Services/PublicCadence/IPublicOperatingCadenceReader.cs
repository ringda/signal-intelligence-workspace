namespace SignalIntelligenceWorkspace.Services.PublicCadence;

public interface IPublicOperatingCadenceReader
{
    Task<PublicOperatingCadenceSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}
