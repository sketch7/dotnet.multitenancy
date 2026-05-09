namespace Sketch7.Multitenancy.Sample.Api.Heroes;

/// <summary>Provides hero data for a single tenant.</summary>
public interface IHeroDataClient
{
	/// <summary>Gets a single hero by key, or <c>null</c> if not found.</summary>
	Task<Hero?> GetByKey(string key);

	/// <summary>Gets all available heroes.</summary>
	Task<List<Hero>> GetAll();

	/// <summary>Gets all available hero types.</summary>
	Task<List<HeroType.HeroType>> GetAllHeroTypes();
}

/// <summary>Mock <see cref="IHeroDataClient"/> implementation backed by League of Legends data.</summary>
public sealed class MockLoLHeroDataClient : IHeroDataClient
{
	private readonly ILogger<MockLoLHeroDataClient> _logger;
	private readonly List<Hero> _heroes =
	[
		new() { Name = "Rengar", Key = "rengar", Difficulty = HeroDifficulty.Hard, Abilities = ["savagery", "battle-roar", "bola-strike", "thrill-of-the-hunt"] },
		new() { Name = "Kha'zix", Key = "kha-zix", Difficulty = HeroDifficulty.Hard, Abilities = ["taste-their-fear", "void-spike", "leap", "void-assault"] },
		new() { Name = "Singed", Key = "singed", Difficulty = HeroDifficulty.Medium, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] }
	];

	private readonly List<HeroType.HeroType> _heroTypes =
	[
		new() { Key = "assassin", Name = "Assassin", Description = "High-burst damage dealers" },
		new() { Key = "fighter", Name = "Fighter", Description = "Durable melee damage dealers" },
		new() { Key = "mage", Name = "Mage", Description = "Ability-based spell casters" },
		new() { Key = "marksman", Name = "Marksman", Description = "Ranged physical damage dealers" },
		new() { Key = "support", Name = "Support", Description = "Utility and healing specialists" },
		new() { Key = "tank", Name = "Tank", Description = "Durable frontline fighters" }
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
		return Task.FromResult(_heroes);
	}

	/// <inheritdoc />
	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), key.SanitizeForLog(), InstanceId);
		return Task.FromResult(_heroes.Find(x => x.Key == key));
	}

	/// <inheritdoc />
	public Task<List<HeroType.HeroType>> GetAllHeroTypes()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAllHeroTypes), InstanceId);
		return Task.FromResult(_heroTypes);
	}
}

/// <summary>Mock <see cref="IHeroDataClient"/> implementation backed by Heroes of the Storm data.</summary>
public sealed class MockHotsHeroDataClient : IHeroDataClient
{
	private readonly ILogger<MockHotsHeroDataClient> _logger;
	private readonly List<Hero> _heroes =
	[
		new() { Name = "Maiev", Key = "maiev", Difficulty = HeroDifficulty.Hard, Abilities = ["fan-of-knives", "vault-of-the-wardens", "umbral-bind", "containment-disc"] },
		new() { Name = "Alexstrasza", Key = "alexstrasza", Difficulty = HeroDifficulty.Medium, Abilities = ["breath-of-life", "abundance", "wing-buffet", "lifebinder"] },
		new() { Name = "Malthael", Key = "malthael", Difficulty = HeroDifficulty.Medium, Abilities = ["soul-rip", "wraith-strike", "reaper-of-souls", "death-shroud"] },
		new() { Name = "Johanna", Key = "johanna", Difficulty = HeroDifficulty.Easy, Abilities = ["blessed-shield", "falling-sword", "condemn", "laws-of-hope"] },
		new() { Name = "Kael'Thas", Key = "keal-thas", Difficulty = HeroDifficulty.Hard, Abilities = ["flamestrike", "gravity-lapse", "verdant-spheres", "pyroblast"] }
	];

	private readonly List<HeroType.HeroType> _heroTypes =
	[
		new() { Key = "melee-assassin", Name = "Melee Assassin", Description = "High-burst damage dealers who attack in melee range" },
		new() { Key = "ranged-assassin", Name = "Ranged Assassin", Description = "High-burst damage dealers who attack from range" },
		new() { Key = "support", Name = "Support", Description = "Utility and healing specialists" },
		new() { Key = "tank", Name = "Tank", Description = "Durable frontline fighters" },
		new() { Key = "bruiser", Name = "Bruiser", Description = "Durable melee damage dealers" }
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
		return Task.FromResult(_heroes);
	}

	/// <inheritdoc />
	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), key.SanitizeForLog(), InstanceId);
		return Task.FromResult(_heroes.Find(x => x.Key == key));
	}

	/// <inheritdoc />
	public Task<List<HeroType.HeroType>> GetAllHeroTypes()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAllHeroTypes), InstanceId);
		return Task.FromResult(_heroTypes);
	}
}