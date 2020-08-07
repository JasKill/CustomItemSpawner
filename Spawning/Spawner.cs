using System;
using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemData;
using Log = Exiled.API.Features.Log;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class Spawner {
		public static readonly Spawner Instance = new Spawner();

		private static List<ItemRoom> Rooms => SpawnPointCreator.ItemRooms;
		private static readonly List<ItemRoom> FreeRooms = new List<ItemRoom>();

		public void Reset() {
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

			Log.Error($"Item Type has {Enum.GetNames(typeof(ItemType)).Length} values");
			Log.Error($"This should be none: {(ItemType)(-1)}");
			Log.Error($"This should be none: {(ItemType)(36)}");



			//todo randomize queuedlists
			//var listSize = QueuedListList.Count;
			//for (int i = 0; i < listSize; i++) {
			//	QueuedListList[i].Reset();
			//}

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

			Log.Error($"{FreeRooms.Count} left");
			if (FreeRooms.Count != 0)
				SpawnStartItems();
		}

		private static Inventory _cachedInventory;
		public static Pickup SpawnItem(ItemSpawnPoint point, ItemType itemType) {
			//Log.Error($"Spawning {itemType.ToString()}");

			if (itemType == ItemType.None) return null;

			var pickup = _cachedInventory.SetPickup(itemType, -4.65664672E+11f, point.Position, point.Rotation, 0, 0, 0);
			var listener = pickup.gameObject.AddComponent<PickupDisableTrigger>();
			listener.Initialize(point);
			return pickup;
		}
	}
}
