using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemData;
using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class EndlessSpawning {
		public static readonly EndlessSpawning Instance = new EndlessSpawning();

		public EndlessSpawning() => RoomItemComponent.OnRoomIsFree += RoomItemComponent_OnRoomIsFree;

		private static List<RoomItemComponent> Rooms => SpawnPointCreator.RoomItemsList;
		private static readonly List<RoomItemComponent> FreeRooms = new List<RoomItemComponent>();

		public void Reset() {
			FreeRooms.Clear();

			var rooms = Rooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				FreeRooms.Add(rooms[i]);
			}
		}

		public void RoomItemComponent_OnRoomIsFree(RoomItemComponent room) => FreeRooms.Insert(Random.Range(0, FreeRooms.Count), room);

		public void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev) {
			if (ev.Pickup.GetComponent<PickupDisableTrigger>() == null) {
				Log.Error("Pickup doesn't exist");
			} else {
				ev.Pickup.GetComponent<PickupDisableTrigger>().PickedUp();

			}
		}
	}
} 