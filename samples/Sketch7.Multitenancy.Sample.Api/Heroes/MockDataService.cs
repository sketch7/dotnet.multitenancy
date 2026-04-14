namespace Sketch7.Multitenancy.Sample.Api.Heroes;

public static class MockDataService
{
	private static readonly List<Hero> MockData =
	[
		new() { Name = "Rengar", Key = "rengar", Role = HeroRoleType.Assassin, Abilities = ["savagery", "battle-roar", "bola-strike", "thrill-of-the-hunt"] },
		new() { Name = "Kha'zix", Key = "kha-zix", Role = HeroRoleType.Assassin, Abilities = ["taste-their-fear", "void-spike", "leap", "void-assault"] },
		new() { Name = "Singed", Key = "singed", Role = HeroRoleType.Tank, Abilities = ["poison-trail", "mega-adhesive", "fling", "insanity-potion"] }
	];

	public static List<Hero> GetHeroes() => MockData;

	public static Hero? GetById(string key) => MockData.FirstOrDefault(x => x.Key == key);
}