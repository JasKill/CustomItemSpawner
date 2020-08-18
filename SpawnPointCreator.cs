using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;

namespace ArithFeather.CustomItemSpawner {
	internal static class SpawnPointCreator {

		// Populated every new game.
		private static readonly Dictionary<string, List<ItemSpawnPoint>> FixedItemSpawnPointDictionary = new Dictionary<string, List<ItemSpawnPoint>>();
		public static readonly List<SpawnGroup> SpawnGroups = new List<SpawnGroup>();
		public static readonly Dictionary<string, SpawnGroup> SpawnGroupDictionary = new Dictionary<string, SpawnGroup>();
		public static readonly Dictionary<string, List<IItemObtainable>> EndlessSpawningItemsDictionary = new Dictionary<string, List<IItemObtainable>>();
		public static readonly List<SpawnableItem> SpawnableItems = new List<SpawnableItem>();

		private static PointList PointList => ItemSpawnIO.SpawnPointList;
		private static IReadOnlyDictionary<string, SpawnGroupData> SpawnGroupItemDictionary => ItemSpawnIO.SpawnGroupItemDictionary;
		private static IReadOnlyDictionary<string, SpawnGroupData> EndlessSpawnGroupItemDictionary => ItemSpawnIO.EndlessSpawnGroupItemDictionary;
		private static IReadOnlyDictionary<string, SpawnGroupData> ContainerGroupItemDictionary => ItemSpawnIO.ContainerGroupItemDictionary;

		public static void OnLoadSpawnPoints() {
			// Need to call these here because creating the files requires game data to be loaded.
			ItemSpawnIO.CheckFiles();

			var spawnPointDictionary = PointList.IdGroupedFixedPoints;
			var spawnPointDictionaryCount = spawnPointDictionary.Count;

			var itemGroupCount = SpawnGroupItemDictionary.Count;
			if (spawnPointDictionaryCount == 0 || itemGroupCount == 0) {
				Log.Error($"Could not make Spawn Groups. (Spawn Point Groups: {spawnPointDictionaryCount} | Item Groups: {itemGroupCount})");
				return;
			}

			SpawnGroups.Clear();
			SpawnGroupDictionary.Clear();
			EndlessSpawningItemsDictionary.Clear();
			FixedItemSpawnPointDictionary.Clear();

			// wrap spawn points in ItemSpawnPoint
			var oldFixedPointDictionary = PointList.IdGroupedFixedPoints;

			foreach (var keyOldfFixedPointsGroup in oldFixedPointDictionary) {
				var key = keyOldfFixedPointsGroup.Key;

				var oldSpawns = keyOldfFixedPointsGroup.Value;
				var spawnCount = oldSpawns.Count;

				var newSpawns = new List<ItemSpawnPoint>();

				for (int i = 0; i < spawnCount; i++) {
					newSpawns.Add(new ItemSpawnPoint(oldSpawns[i]));
				}

				FixedItemSpawnPointDictionary.Add(key, newSpawns);
			}

			// For each item group...
			foreach (var pair in SpawnGroupItemDictionary) {
				var groupData = pair.Value;
				var keys = groupData.Owners;
				var keyCount = keys.Count;

				var itemSpawnPoints = new List<ItemSpawnPoint>();

				// Adding spawn points from all the keys saved in ItemData.
				for (int i = 0; i < keyCount; i++)
				{
					if (FixedItemSpawnPointDictionary.TryGetValue(keys[i], out var spawnPoints))
					{
						itemSpawnPoints.AddRange(spawnPoints);
					}
				}

				if (itemSpawnPoints.Count == 0) continue;

				var itemList = new List<IItemObtainable>(groupData.Items.Count +
									 groupData.ItemLists.Count);

				// Shuffle the lists before adding them
				groupData.Items.UnityShuffle();
				groupData.ItemLists.UnityShuffle();

				itemList.AddRange(groupData.Items);
				itemList.AddRange(groupData.ItemLists);

				var spawnGroup = new SpawnGroup(pair.Key, itemList, itemSpawnPoints);

				SpawnGroupDictionary.Add(pair.Key, spawnGroup);
				SpawnGroups.Add(spawnGroup);
			}
			
			// Endless Spawning
			foreach (var pair in EndlessSpawnGroupItemDictionary) {
				var groupData = pair.Value;

				if (!SpawnGroupDictionary.ContainsKey(pair.Key)) {

					var keys = groupData.Owners;
					var keyCount = keys.Count;
					var itemSpawnPoints = new List<ItemSpawnPoint>();

					// Adding spawn points from all the keys saved in ItemData.
					for (int i = 0; i < keyCount; i++) {
						if (FixedItemSpawnPointDictionary.TryGetValue(keys[i], out var spawnPoints)) {
							itemSpawnPoints.AddRange(spawnPoints);
						}
					}

					if (itemSpawnPoints.Count == 0) continue;

					var spawnGroup = new SpawnGroup(pair.Key, default, itemSpawnPoints);
					SpawnGroupDictionary.Add(pair.Key, spawnGroup);
					SpawnGroups.Add(spawnGroup);
				}

				var itemList = new List<IItemObtainable>(groupData.Items.Count +
				                                         groupData.ItemLists.Count);

				// Shuffle the lists before adding them
				groupData.Items.UnityShuffle();
				groupData.ItemLists.UnityShuffle();

				itemList.AddRange(groupData.Items);
				itemList.AddRange(groupData.ItemLists);

				EndlessSpawningItemsDictionary.Add(pair.Key, itemList);
			}

			Log.Info($"Found {SpawnGroups.Count} group(s) with items to spawn.");

			SavedItemRoom.CreateGlobalRooms();
		}

		public static void OnLoadContainers(int seed) {
			UnityEngine.Random.InitState(seed);

			SpawnableItems.Clear();

			foreach (var pair in ContainerGroupItemDictionary) {
				var groupData = pair.Value;

				// Shuffle the lists before adding them
				groupData.Items.UnityShuffle();
				groupData.ItemLists.UnityShuffle();

				Parse(groupData.Items);
				Parse(groupData.ItemLists);

				void Parse(IReadOnlyList<IItemObtainable> items) {
					var itemCount = items.Count;
					for (int i = 0; i < itemCount; i++) {
						var item = items[i] as ContainerItem;

						if (item == null) continue;

						var itemData = item.GetItem();

						var newItem = new SpawnableItem {
							chanceOfSpawn = item.Chance,
							copies = itemData.Copies - 1,
							inventoryId = itemData.Item,
							itemTag = item.ContainerId,
							name = item.ContainerId
						};

						SpawnableItems.Add(newItem);
					}
				}
			}

			LockerManager.singleton.items = SpawnableItems.ToArray();
		}
	}
}