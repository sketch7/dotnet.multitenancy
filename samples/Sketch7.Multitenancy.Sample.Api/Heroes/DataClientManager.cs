using System;
using System.Collections.Generic;

namespace Sketch7.Multitenancy.Sample.Api.Heroes
{
	// singleton service shared across tenants 
public interface IDataClientManager
{
	IDataClientManager Register(object type);
}

public class DataClientManager : IDataClientManager, IDisposable
{
	private readonly HashSet<object> _instances = new HashSet<object>();

	public IDataClientManager Register(object type)
	{
		_instances.Add(type);
		return this;
	}

	public void Dispose()
	{
	}
}
}
