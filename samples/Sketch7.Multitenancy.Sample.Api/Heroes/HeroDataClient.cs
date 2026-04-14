namespace Sketch7.Multitenancy.Sample.Api.Heroes;

public interface IHeroDataClient
{
	Task<Hero?> GetByKey(string key);
	Task<List<Hero>> GetAll();
}

public class MockLoLHeroDataClient : IHeroDataClient
{
	private readonly ILogger<MockLoLHeroDataClient> _logger;

	public Guid InstanceId { get; } = Guid.NewGuid();

	public MockLoLHeroDataClient(
		ILogger<MockLoLHeroDataClient> logger,
		IDataClientManager clientManager
	)
	{
		_logger = logger;
		clientManager.Register(this);
	}

	public Task<List<Hero>> GetAll()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAll), InstanceId);
		return Task.FromResult(MockDataService.GetHeroes().ToList());
	}

	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), SanitizeForLog(key), InstanceId);
		return Task.FromResult(MockDataService.GetById(key));
	}

	private static string SanitizeForLog(string value)
		=> value.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
}

public class MockHotsHeroDataClient : IHeroDataClient, IDisposable
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

	public Guid InstanceId { get; } = Guid.NewGuid();

	public MockHotsHeroDataClient(
		ILogger<MockHotsHeroDataClient> logger,
		IDataClientManager clientManager
	)
	{
		_logger = logger;
		clientManager.Register(this);
	}

	public Task<List<Hero>> GetAll()
	{
		_logger.LogDebug("[{Method}] Fetch from mock service ({InstanceId})", nameof(GetAll), InstanceId);
		return Task.FromResult(_data);
	}

	public Task<Hero?> GetByKey(string key)
	{
		_logger.LogDebug("[{Method}] Fetching key: {Key} from mock service ({InstanceId})", nameof(GetByKey), SanitizeForLog(key), InstanceId);
		return Task.FromResult(_data.Find(x => x.Key == key));
	}

	private static string SanitizeForLog(string value)
		=> value.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);

	public void Dispose() { }
}
