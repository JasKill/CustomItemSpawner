using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	internal class SavedItemRoom : MonoBehaviour
	{
		public readonly List<SpawnInfo> SavedSpawns = new List<SpawnInfo>();

		public bool HasBeenEntered { get; set; }

		public Room Room { get; private set; }

		public void SpawnSavedItems() => Spawner.SpawnItems(SavedSpawns);

		public static readonly Dictionary<int, SavedItemRoom> SavedRooms = new Dictionary<int, SavedItemRoom>();

		public static void CreateGlobalRooms()
		{
			SavedRooms.Clear();

			foreach (var room in Map.Rooms)
			{
				var gameObject = room.Transform.gameObject;
				var comp = gameObject.AddComponent<SavedItemRoom>();
				comp.Room = room;
				SavedRooms.Add(gameObject.GetInstanceID(), comp);
			}

			foreach (var door in Map.Doors)
			{
				door.gameObject.AddComponent<CustomDoor>();
			}
		}
	}
}