using System.Collections.Generic;
using ArithFeather.AriToolKit;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class SavedItemRoom {
		public readonly List<SpawnInfo> SavedSpawns = new List<SpawnInfo>();

		public bool HasBeenEntered { get; set; }

		public void SpawnSavedItems() => Spawner.SpawnItems(SavedSpawns);

		public static readonly List<SavedItemRoom> SavedRooms = new List<SavedItemRoom>();

		public static void CreateGlobalRooms() {
			SavedRooms.Clear();

			var rooms = Rooms.CustomRooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				SavedRooms.Add(new SavedItemRoom());
			}
		}
	}
}