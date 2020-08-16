using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;

namespace ArithFeather.CustomItemSpawner {
	internal static class SpawnPointCreator {

		// Populated every new game.
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

			// Group up the positions and item spawns via ID's
			foreach (var pair in spawnPointDictionary) {
				var key = pair.Key;
				var spawnPoints = pair.Value;

				if (SpawnGroupItemDictionary.TryGetValue(key, out var groupData)) {

					var itemList = new List<IItemObtainable>(groupData.Items.Count +
														 groupData.ItemLists.Count);

					// Shuffle the lists before adding them
					groupData.Items.UnityShuffle();
					groupData.ItemLists.UnityShuffle();

					itemList.AddRange(groupData.Items);
					itemList.AddRange(groupData.ItemLists);

					var spawnGroup = new SpawnGroup();

					// Convert SpawnPoint class to ItemSpawnPoint class.
					var newSpawnPointList = new List<ItemSpawnPoint>();
					var pointCount = spawnPoints.Count;
					for (int i = 0; i < pointCount; i++) {
						newSpawnPointList.Add(new ItemSpawnPoint(spawnPoints[i]));
					}

					spawnGroup.Initialize(key, itemList, newSpawnPointList);
					SpawnGroupDictionary.Add(key, spawnGroup);
					SpawnGroups.Add(spawnGroup);
				}

				// Add in our endless spawns information.
				if (EndlessSpawnGroupItemDictionary.TryGetValue(key, out groupData)) {

					var itemList = new List<IItemObtainable>(groupData.Items.Count +
														 groupData.ItemLists.Count);
					// Shuffle the lists before adding them
					groupData.Items.UnityShuffle();
					groupData.ItemLists.UnityShuffle();

					itemList.AddRange(groupData.Items);
					itemList.AddRange(groupData.ItemLists);

					EndlessSpawningItemsDictionary.Add(key, itemList);
				}
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

						if (item == null || !item.HasItems) continue;

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