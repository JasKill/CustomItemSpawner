using ArithFeather.ArithSpawningKit.SpawnPointTools;
using System.Collections.Generic;

namespace ArithFeather.RandomItemSpawner
{
	public class ItemRoom
	{
		public CustomRoom Room { get; }

		public List<ItemSpawnPoint> ItemSpawnPoints = new List<ItemSpawnPoint>();

		private int currentItemsSpawned;
		public int CurrentItemsSpawned
		{
			get => currentItemsSpawned;
			set
			{
				currentItemsSpawned = value;
				AtMaxItemSpawns = (value >= MaxItemsAllowed);
			}
		}

		public int MaxItemsAllowed;
		public bool IsFree = true;

		public ItemRoom(CustomRoom room, int maxItemsAllowed)
		{
			Room = room;
			MaxItemsAllowed = maxItemsAllowed;
		}

		public bool AtMaxItemSpawns { get; private set; }
	}
}
