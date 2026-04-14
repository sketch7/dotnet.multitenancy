namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>
/// Persistent state for <see cref="HeroGrain"/>, stored in the <c>heroes</c> grain storage provider.
/// </summary>
[GenerateSerializer]
public sealed class HeroGrainState
{
	/// <summary>Gets or sets the cached hero list for this tenant.</summary>
	[Id(0)]
	public List<Hero> Heroes { get; set; } = [];
}