using System.Collections.Generic;
using MEC;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class Spawner {
		public static readonly Spawner Instance = new Spawner();

		public Spawner() => SpawnGroup.OnRoomIsFree += SpawnGroup_OnRoomIsFree;

		private static List<SpawnGroup> Rooms => SpawnPointCreator.SpawnGroups;
		public readonly List<SpawnGroup> FreeRooms = new List<SpawnGroup>();

		//bool ContainItem(List<SpawnableItem> list, SpawnableItem item) {

		//	foreach (var lockerItem in list) {

		//		if (lockerItem != item &&
		//			lockerItem.chanceOfSpawn == item.chanceOfSpawn &&
		//			lockerItem.inventoryId == item.inventoryId) {
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		public void Reset() {

			// Testing Lockers

			//using (var writer = new StreamWriter(File.Create(Path.Combine(Paths.Configs, "SpawnableItemsRaw") + ".txt"))) {
			//	var itemList = new List<SpawnableItem>();

			//	var lockerItems = LockerManager.singleton.items;
			//	var lockerItemCount = lockerItems.Length;

			//	// Group via same ID and CHANCE
			//	for (int i = 0; i < lockerItemCount; i++) {
			//		var item = lockerItems[i];

			//		//var item = new SpawnableItem {
			//		//	chanceOfSpawn = oldItem.chanceOfSpawn,
			//		//	copies = oldItem.copies, // so we can add them together.
			//		//	inventoryId = oldItem.inventoryId,
			//		//	itemTag = oldItem.itemTag,
			//		//	name = oldItem.name
			//		//};

			//		var index = itemList.FindIndex((spawnableItem) =>
			//			item.chanceOfSpawn == spawnableItem.chanceOfSpawn &&
			//			item.inventoryId == spawnableItem.inventoryId);

			//		if (index == -1) // Not Found
			//		{
			//			itemList.Add(item);
			//		} else {
			//			//itemList[index].copies += item.copies;
			//		}
			//	}

			//	var spawnCount = itemList.Count;
			//	for (int i = 0; i < spawnCount; i++) {
			//		var item = itemList[i];
			//		var numItems = Mathf.RoundToInt(100 / item.chanceOfSpawn) - 1;



			//		writer.WriteLine($"CONTAINER: [{item.itemTag}] -- ITEM ID: [{(int)item.inventoryId}]  -- COPIES: [{item.copies + 1}] -- CHANCE TO SPAWN: [{item.chanceOfSpawn}]");

			//	}

				//for (int i = 0; i < lockerItemCount; i++) {
				//	var lockerItem = lockerItems[i];

				//	if (!ContainItem(newSpawnableItemList, lockerItem))
				//		newSpawnableItemList.Add(lockerItem);
				//}

				//writer.WriteLine("[Item Lists]");

				//var newSpawnCount = newSpawnableItemList.Count;

				//for (int i = 0; i < newSpawnCount; i++) {
				//	var item = newSpawnableItemList[i];

				//	writer.WriteLine($"{item.inventoryId}:{(int)item.inventoryId}:{item.copies}:{item.chanceOfSpawn}");
				//}

				//writer.WriteLine();
				//writer.WriteLine("[Containers]");
				//writer.WriteLine();

				//var keyCounter = new Dictionary<string, StringBuilder>();

				//for (int i = 0; i < lockerItemCount; i++) {
				//	var item = lockerItems[i];

				//	var key = item.itemTag;
				//	var itemId = item.inventoryId.ToString();

				//	if (!string.IsNullOrWhiteSpace(itemId) && keyCounter.TryGetValue(key, out var value)) {
				//		value.Append($",{itemId}");
				//	} else {
				//		keyCounter.Add(key, new StringBuilder($"{key}:{itemId}"));
				//	}
				//}

				//foreach (var stringPair in keyCounter) {
				//	writer.WriteLine(stringPair.Value.ToString());
				//}
				////for (int i = 0; i < length; i++)
				////{
				////	var item = spawnableItems[i];

				////	writer.WriteLine($"name: {item.name} | tag: {item.itemTag} | Inventory ID: {item.inventoryId} | copies: {item.copies} | Spawn Chance: {item.chanceOfSpawn}");
				////}
			//}

			// Testing Lockers


			_cachedInventory = PlayerManager.localPlayer.GetComponent<Inventory>();
			FreeRooms.Clear();

			var rooms = Rooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				var room = Rooms[i];
				if (!room.AtMaxItemSpawns) {
					FreeRooms.Add(rooms[i]);
				}
			}

			FreeRooms.ShuffleList();

			SpawnStartItems();
		}

		private void SpawnStartItems() {
			var roomCount = FreeRooms.Count;

			for (int i = roomCount - 1; i >= 0; i--) {
				var room = FreeRooms[i];

				if (room.AtMaxItemSpawns || !room.TrySpawnStartItem()) {
					FreeRooms.RemoveAt(i);
				}
			}

			if (FreeRooms.Count != 0)
				SpawnStartItems();
		}

		#region Static spawning methods

		private static Inventory _cachedInventory;
		public static void SpawnItem(ItemSpawnPoint point, ItemType itemType) {
			if (itemType == ItemType.None) return;

			var pickup = _cachedInventory.SetPickup(itemType, -4.65664672E+11f, point.Position, point.Rotation, 0, 0, 0);

			if (CustomItemSpawner.Configs.EnableItemTracking) {
				var listener = pickup.gameObject.AddComponent<PickupDisableTrigger>();
				listener.Initialize(point);
			}
		}

		public static void SpawnItems(List<SpawnInfo> spawns) => Timing.RunCoroutine(StaggerSpawnItems(spawns), Segment.FixedUpdate);

		private static IEnumerator<float> StaggerSpawnItems(IReadOnlyList<SpawnInfo> spawns) {
			var spawnCount = spawns.Count;
			for (int i = 0; i < spawnCount; i++) {
				var spawn = spawns[i];
				SpawnItem(spawn.ItemSpawnPoint, spawn.ItemType);
				yield return Timing.WaitForOneFrame;
			}
		}

		#endregion

		#region Endless Spawning

		private void SpawnGroup_OnRoomIsFree(SpawnGroup spawnGroup) => FreeRooms.Insert(Random.Range(0, FreeRooms.Count), spawnGroup);

		public void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev)
		{
			if (CustomItemSpawner.Configs.EnableItemTracking) ev.Pickup.GetComponent<PickupDisableTrigger>()?.PickedUp();
		}

		#endregion
	}
}
