using System;
using System.Runtime.CompilerServices;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemData;
using ArithFeather.CustomItemSpawner.Patches;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;
using Version = System.Version;

namespace ArithFeather.CustomItemSpawner {
	public class CustomItemSpawner : Plugin<BasicConfig> {

		public CustomItemSpawner() {
			SpawnPointCreator.Reload();
		}

		public override string Author => "Arith";
		public override Version Version => new Version("2.00");

		public override void OnEnabled() {
			base.OnEnabled();
			PointAPI.OnLoadSpawnPoints += SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed += CheckDoorItemSpawn;
			DestroyedDoorPatch.Enable();
			StopDoorTriggerPatch.Enable();

			//Exiled.Events.Handlers.Player.PickingUpItem += EndlessSpawning.Instance.Player_PickingUpItem;

			Exiled.Events.Handlers.Server.WaitingForPlayers += Spawner.Instance.Reset;

			Exiled.Events.Handlers.Player.InteractingDoor += Player_InteractingDoor;
		}

		public override void OnDisabled() {
			PointAPI.OnLoadSpawnPoints -= SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed -= CheckDoorItemSpawn;
			DestroyedDoorPatch.Disable();
			StopDoorTriggerPatch.Disable();

			//Exiled.Events.Handlers.Player.PickingUpItem -= EndlessSpawning.Instance.Player_PickingUpItem;
			Exiled.Events.Handlers.Server.WaitingForPlayers -= Spawner.Instance.Reset;
			Exiled.Events.Handlers.Player.InteractingDoor -= Player_InteractingDoor;

			base.OnDisabled();
		}

		public override void OnReloaded() {
			base.OnReloaded();
			SpawnPointCreator.Reload();
		}

		private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev) {
			var door = ev.Door;
			if (door.isOpen || door.destroyed || (!door.isOpen && !ev.IsAllowed)) return;

			CheckDoorItemSpawn(door);
		}

		private void CheckDoorItemSpawn(Door door) {
			var customDoor = door.GetCustomDoor();

			CheckRoomItemsSpawned(customDoor.Room1.Id);

			if (customDoor.HasTwoRooms) {
				CheckRoomItemsSpawned(customDoor.Room2.Id);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckRoomItemsSpawned(int id) {
			var room = SpawnPointCreator.ItemRooms[id];

			if (room != null && !room.HasBeenEntered) {
				room.HasBeenEntered = true;
				room.SpawnSavedItems();
			}
		}
	}
}
