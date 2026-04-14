namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Singleton service that tracks all registered <see cref="IHeroDataClient"/> instances across tenants.</summary>
public interface IDataClientManager
{
	/// <summary>Registers a data client instance.</summary>
	IDataClientManager Register(object instance);
}

/// <summary>In-memory implementation of <see cref="IDataClientManager"/>.</summary>
public sealed class DataClientManager : IDataClientManager
{
	private readonly HashSet<object> _instances = [];

	/// <inheritdoc />
	public IDataClientManager Register(object instance)
	{
		_instances.Add(instance);
		return this;
	}
}