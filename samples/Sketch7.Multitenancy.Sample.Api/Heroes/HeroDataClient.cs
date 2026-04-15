namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Provides hero data for a single tenant.</summary>
public interface IHeroDataClient
{
	/// <summary>Gets a single hero by key, or <c>null</c> if not found.</summary>
	Task<Hero?> GetByKey(string key);

	/// <summary>Gets all available heroes.</summary>
	Task<List<Hero>> GetAll();
}

/// <summary>Mock <see cref="IHeroDataClient"/> implementation backed by League of Legends data.</summary>
public sealed class MockLoLHeroDataClient : IHeroDataClient
{
	private readonly ILogger<MockLoLHeroDataClient> _logger;
	private readonly List<Hero> _data =
	[
		new() { Name = "Rengar", Key = "rengar", Role = HeroRoleType.Assassin, Abilities = ["savagery", "battle-roar", "bola-strike", "thrill-of-the-hunt"] },
		new() { Name = "Kha'zix", Key = "kha-zix", Role = HeroRoleType.Assassin, Abilities = ["taste-their-fear", "void-spike", "leap", "void-assault"] },
		new() { Name = "Singed", Key = "singed", Role = HeroRoleType.Tank, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] }
	];

	/// <summary>Gets the unique instance identifier for diagnostics.</summary>
	public Guid InstanceId { get; } = Guid.NewGuid();

	/// <summary>Initializes a new instance of <see cref="MockLoLHeroDataClient"/>.</summary>
	public MockLoLHeroDataClient(ILogger<MockLoLHeroDataClient> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<List<Hero>> GetAll()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAll), InstanceId);
		return Task.FromResult(_data);
	}

	/// <inheritdoc />
	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), key.SanitizeForLog(), InstanceId);
		return Task.FromResult(_data.Find(x => x.Key == key));
	}
}

/// <summary>Mock <see cref="IHeroDataClient"/> implementation backed by Heroes of the Storm data.</summary>
public sealed class MockHotsHeroDataClient : IHeroDataClient
{
	private readonly ILogger<MockHotsHeroDataClient> _logger;
	private readonly List<Hero> _data =
	[
		new() { Name = "Maiev", Key = "maiev", Role = HeroRoleType.Assassin, Abilities = ["savagery", "battle-roar", "bola-strike", "thrill-of-the-hunt"] },
		new() { Name = "Alexstrasza", Key = "alexstrasza", Role = HeroRoleType.Support, Abilities = ["taste-their-fear", "void-spike", "leap", "void-assault"] },
		new() { Name = "Malthael", Key = "malthael", Role = HeroRoleType.Assassin, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] },
		new() { Name = "Johanna", Key = "johanna", Role = HeroRoleType.Tank, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] },
		new() { Name = "Kael'Thas", Key = "keal-thas", Role = HeroRoleType.Assassin, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] },
	];

	/// <summary>Gets the unique instance identifier for diagnostics.</summary>
	public Guid InstanceId { get; } = Guid.NewGuid();

	/// <summary>Initializes a new instance of <see cref="MockHotsHeroDataClient"/>.</summary>
	public MockHotsHeroDataClient(ILogger<MockHotsHeroDataClient> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public Task<List<Hero>> GetAll()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAll), InstanceId);
		return Task.FromResult(_data);
	}

	/// <inheritdoc />
	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), key.SanitizeForLog(), InstanceId);
		return Task.FromResult(_data.Find(x => x.Key == key));
	}
}