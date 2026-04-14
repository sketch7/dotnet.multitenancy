namespace Sketch7.Multitenancy.Sample.Api.Heroes;

// singleton service shared across tenants
public interface IDataClientManager
{
	IDataClientManager Register(object instance);
}

public class DataClientManager : IDataClientManager, IDisposable
{
	private readonly HashSet<object> _instances = [];

	public IDataClientManager Register(object instance)
	{
		_instances.Add(instance);
		return this;
	}

	public void Dispose() { }
}
