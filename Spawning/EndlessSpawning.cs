using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemData;
using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class EndlessSpawning {
		public static readonly EndlessSpawning Instance = new EndlessSpawning();

		public EndlessSpawning() => ItemRoom.OnRoomIsFree += RoomItemComponent_OnRoomIsFree;

		private static List<ItemRoom> Rooms => SpawnPointCreator.ItemRooms;
		private static readonly List<ItemRoom> FreeRooms = new List<ItemRoom>();

		public void Reset() {
			FreeRooms.Clear();

			var rooms = Rooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				FreeRooms.Add(rooms[i]);
			}
		}

		public void RoomItemComponent_OnRoomIsFree(ItemRoom itemRoom) => FreeRooms.Insert(Random.Range(0, FreeRooms.Count), itemRoom);

		public void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev) {
			if (ev.Pickup.GetComponent<PickupDisableTrigger>() == null) {
				// If it doesn't exist, it's a default game spawn.
				//Log.Error("Pickup doesn't exist");
			} else {
				ev.Pickup.GetComponent<PickupDisableTrigger>().PickedUp();

			}
		}
	}
} 