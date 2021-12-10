namespace ExampleGrainInterfaces;

/// <summary>
/// The example grain.
/// </summary>
/// <inheritdoc cref="IGrainWithStringKey" />
public interface IExampleGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets all data.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of <see cref="DtoData"/>.</returns>
    Task<List<DtoData>> GetAllData();
}
