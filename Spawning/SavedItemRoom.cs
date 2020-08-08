using System.Collections.Generic;
using ArithFeather.AriToolKit;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class SavedItemRoom {
		public readonly List<SavedSpawnInfo> SavedSpawns = new List<SavedSpawnInfo>();

		public bool HasBeenEntered { get; set; }

		public void SpawnSavedItems() {
			var length = SavedSpawns.Count;
			for (int i = 0; i < length; i++) {
				var spawn = SavedSpawns[i];
				Spawner.SpawnItem(spawn.ItemSpawnPoint, spawn.ItemType);
			}
		}

		public static readonly List<SavedItemRoom> SavedRooms = new List<SavedItemRoom>();

		public static void CreateGlobalRooms() {
			SavedRooms.Clear();

			var rooms = Rooms.CustomRooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				SavedRooms.Add(new SavedItemRoom());
			}
		}

		public readonly struct SavedSpawnInfo {

			public readonly ItemSpawnPoint ItemSpawnPoint;
			public readonly ItemType ItemType;

			public SavedSpawnInfo(ItemSpawnPoint point, ItemType itemType) {
				ItemSpawnPoint = point;
				ItemType = itemType;
			}
		}
	}
}