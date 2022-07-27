using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Users;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Api.AnonymousUsers
{
	/// <summary>
	/// Handles anonymous users. Generates accounts for them.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AnonymousUserService : AutoService
	{
		private AnonymousUserConfig _configuration;
		private UserService _users;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AnonymousUserService(UserService users)
        {
			_users = users;
			_configuration = GetConfig<AnonymousUserConfig>();
			
			if(_configuration != null)
			{
				if(_configuration.Disabled)
				{
					return;
				}
				
				if(_configuration.FirstNames != null)
				{
					FirstNames = _configuration.FirstNames;
				}
				
				if(_configuration.LastNames != null)
				{
					LastNames = _configuration.LastNames;
				}

				if (_configuration.IgnoreUserAgents != null)
                {
					IgnoreUserAgents = _configuration.IgnoreUserAgents.Select(s => s.ToLower()).ToArray();
                }

			}
			
			if(FirstNames == null)
			{
				FirstNames = DefaultFirstNames;
			}
			
			if(LastNames == null)
			{
				LastNames = DefaultLastNames;
			}

			Events.ContextAfterAnonymous.AddEventListener(async (Context ctx, Context result, HttpRequest request) =>
			{
				// ignore specified user agents
				if (IgnoreUserAgents != null && IgnoreUserAgents.Any() && request.Headers.ContainsKey("user-agent"))
                {
					var userAgent = ((string)request.Headers["user-agent"]).ToLower();

					if (!string.IsNullOrWhiteSpace(userAgent) && IgnoreUserAgents.Any(s => userAgent.Contains(s)))
					{
						return result;
					}
                }

				if (result != null && result.UserId == 0)
				{
					// Create an account and use a context for it:
					var user = await CreateAccount();
					result.User = user;
				}

				return result;
			});
		}

		private async ValueTask<User> CreateAccount()
		{
			var firstName = "";
			var lastName = "";

			if (FirstNames != null && FirstNames.Length != 0)
			{
				firstName = FirstNames[_randomiser.Next(0, FirstNames.Length)];
			}

			if (LastNames != null && LastNames.Length != 0)
			{
				lastName = LastNames[_randomiser.Next(0, LastNames.Length)];
			}

			// Must use admin role in order to specify a role for the user like this.
			return await _users.Create(new Context(Roles.Developer),
			new User() {
				FirstName = firstName,
				LastName = lastName,
				Role = Role
			});
		}

		/// <summary>
		/// Role to assign.
		/// </summary>
		private uint Role = 3;

		/// <summary>
		/// First names in use.
		/// </summary>
		private string[] FirstNames;

		/// <summary>
		/// Last names in use.
		/// </summary>
		private string[] LastNames;
	
		/// <summary>
		/// User agents to ignore.
		/// </summary>
		private string[] IgnoreUserAgents;

		private Random _randomiser = new Random();
		
		/// <summary>
		/// Default first names
		/// </summary>
		public readonly string[] DefaultFirstNames = new string[]{
"Anonymous",
"Nameless",
"Unidentified",
"Unnamed",
"Incognito",
"Secret"
};
		
		/// <summary>
		/// Default last names
		/// </summary>
		public readonly string[] DefaultLastNames = new string[]{
"Aardvark",
"Aardwolf",
"Albatross",
"Alpaca",
"Anaconda",
"Angelfish",
"Anglerfish",
"Ant",
"Anteater",
"Antelope",
"Antlion",
"Aphid",
"Arctic Fox",
"Arctic Wolf",
"Armadillo",
"Badger",
"Bald eagle",
"Bandicoot",
"Barnacle",
"Bear",
"Beaver",
"Bee",
"Beetle",
"Bird",
"Bison",
"Blackbird",
"Black panther",
"Blue bird",
"Blue jay",
"Blue whale",
"Boa",
"Bobcat",
"Bobolink",
"Bonobo",
"Bovid",
"Butterfly",
"Buzzard",
"Camel",
"Canid",
"Cape buffalo",
"Capybara",
"Cardinal",
"Caribou",
"Carp",
"Cat",
"Catshark",
"Caterpillar",
"Catfish",
"Centipede",
"Cephalopod",
"Chameleon",
"Cheetah",
"Chickadee",
"Chimpanzee",
"Chinchilla",
"Chipmunk",
"Clam",
"Cobra",
"Cod",
"Condor",
"Constrictor",
"Coral",
"Coyote",
"Crab",
"Crane",
"Crawdad",
"Crayfish",
"Cricket",
"Crocodile",
"Crow",
"Cuckoo",
"Cicada",
"Damselfly",
"Deer",
"Dingo",
"Dinosaur",
"Dog",
"Dolphin",
"Donkey",
"Dove",
"Dragonfly",
"Dragon",
"Duck",
"Eagle",
"Elephant",
"Elk",
"Emu",
"Ermine",
"Falcon",
"Ferret",
"Finch",
"Firefly",
"Fish",
"Flamingo",
"Fly",
"Fox",
"Fro",
"Galliform",
"Gazelle",
"Gecko",
"Gerbil",
"Giant panda",
"Giant squid",
"Gibbon",
"Gila monster",
"Giraffe",
"Goat",
"Goldfish",
"Goose",
"Gopher",
"Gorilla",
"Grasshopper",
"Grouse",
"Guan",
"Guanaco",
"Guineafowl",
"Guinea pig",
"Gull",
"Guppy",
"Haddock",
"Halibut",
"Hammerhead shark",
"Hamster",
"Hare",
"Harrier",
"Hawk",
"Hedgehog",
"Hermit crab",
"Heron",
"Herring",
"Hippopotamus",
"Horse",
"Hoverfly",
"Hummingbird",
"Humpback whale",
"Hyena",
"Iguana",
"Impala",
"Jackal",
"Jaguar",
"Jay",
"Jellyfish",
"Junglefowl",
"Kangaroo",
"Kingfisher",
"Kite",
"Kiwi",
"Koala",
"Koi",
"Krill",
"Ladybug",
"Lamprey",
"Landfowl",
"Lark",
"Lemming",
"Lemur",
"Leopard",
"Leopon",
"Limpet",
"Lion",
"Lizard",
"Llama",
"Lobster",
"Lynx",
"Macaw",
"Mackerel",
"Magpie",
"Manatee",
"Mandrill",
"Manta ray",
"Marmoset",
"Marmot",
"Marsupial",
"Marten",
"Mastodon",
"Meadowlark",
"Meerkat",
"Mink",
"Minnow",
"Mollusk",
"Mongoose",
"Monkey",
"Moose",
"Mosquito",
"Moth",
"Mountain goat",
"Mouse",
"Muskox",
"Narwhal",
"Newt",
"Nyan cat",
"Nightingale",
"Ocelot",
"Octopus",
"Opossum",
"Orangutan",
"Orca",
"Ostrich",
"Otter",
"Owl",
"Ox",
"Panda",
"Panther",
"Parakeet",
"Parrot",
"Parrotfish",
"Partridge",
"Peacock",
"Peafowl",
"Pelican",
"Penguin",
"Perch",
"Pheasant",
"Pigeon",
"Pilot whale",
"Pinniped",
"Platypus",
"Polar bear",
"Pony",
"Porcupine",
"Porpoise",
"Possum",
"Prawn",
"Primate",
"Ptarmigan",
"Puffin",
"Puma",
"Python",
"Quail",
"Quelea",
"Quokka",
"Rabbit",
"Raccoon",
"Raven",
"Red panda",
"Reindeer",
"Reptile",
"Rhinoceros",
"Right whale",
"Roadrunner",
"Rook",
"Rooster",
"Roundworm",
"Sailfish",
"Salamander",
"Salmon",
"Sawfish",
"Scallop",
"Scorpion",
"Seahorse",
"Sea lion",
"Sheep",
"Silverfish",
"Snow leopard",
"Sparrow",
"Spider",
"Spoonbill",
"Squid",
"Squirrel",
"Starfish",
"Stoat",
"Stork",
"Sturgeon",
"Swallow",
"Swan",
"Swift",
"Swordfish",
"Swordtail",
"Tahr",
"Takin",
"Tapir",
"Tarantula",
"Tern",
"Tiger",
"Tiglon",
"Toad",
"Tortoise",
"Toucan",
"Tree frog",
"Trout",
"Tuna",
"Turkey",
"Turtle",
"Wallaby",
"Walrus",
"Whale",
"Whitefish",
"Whooping crane",
"Wildcat",
"Wildebeest",
"Wolf",
"Wolverine",
"Wombat",
"Woodpecker",
"Wren",
"Yak",
"Zebra"
};
		
	}
    
}
