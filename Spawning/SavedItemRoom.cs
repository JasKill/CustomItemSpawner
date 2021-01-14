using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	internal class SavedItemRoom
	{
		public readonly List<SpawnInfo> SavedSpawns = new List<SpawnInfo>();

		public bool HasBeenEntered { get; set; }

		public Room Room { get; }

		public void SpawnSavedItems() => Spawner.SpawnItems(SavedSpawns);

		public static readonly Dictionary<int, SavedItemRoom> SavedRooms = new Dictionary<int, SavedItemRoom>();

		public SavedItemRoom(Room room)
		{
			Room = room;
		}

		public static void CreateGlobalRooms()
		{
			SavedRooms.Clear();

			foreach (var room in Map.Rooms)
			{
				var gameObject = room.transform.gameObject;
				SavedRooms.Add(gameObject.GetInstanceID(), new SavedItemRoom(room));
			}

			foreach (var door in Map.Doors)
			{
				door.gameObject.AddComponent<CustomDoor>();
			}
		}
	}
}