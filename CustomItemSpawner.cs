using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.Patches;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;
using HarmonyLib;
using Version = System.Version;

namespace ArithFeather.CustomItemSpawner {
	public class CustomItemSpawner : Plugin<Config> {
		public static Config Configs { get; private set; }

		public override string Author => "Arith";
		public override Version Version => new Version("2.05");

		private readonly Harmony _harmony = new Harmony("customItemSpawner");

		public override void OnEnabled() {
			base.OnEnabled();

			ItemSpawnIO.Reload();
			Configs = Config;

			_harmony.PatchAll();
			Exiled.Events.Handlers.Server.ReloadedConfigs += Server_ReloadedConfigs;
			PointAPI.OnLoadSpawnPoints += SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed += CheckDoorItemSpawn;
			DoorOpenEventPatch.OnDoorOpened += CheckDoorItemSpawn;

			Exiled.Events.Handlers.Server.WaitingForPlayers += Spawner.Instance.Reset;
			SpawnGroup.OnRoomIsFree += Spawner.Instance.SpawnGroup_OnRoomIsFree;
			Exiled.Events.Handlers.Player.PickingUpItem += Spawner.Instance.Player_PickingUpItem;
		}

		public override void OnDisabled() {
			Exiled.Events.Handlers.Server.ReloadedConfigs -= Server_ReloadedConfigs;
			PointAPI.OnLoadSpawnPoints -= SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed -= CheckDoorItemSpawn;
			DoorOpenEventPatch.OnDoorOpened -= CheckDoorItemSpawn;

			Exiled.Events.Handlers.Server.WaitingForPlayers -= Spawner.Instance.Reset;
			SpawnGroup.OnRoomIsFree -= Spawner.Instance.SpawnGroup_OnRoomIsFree;
			Exiled.Events.Handlers.Player.PickingUpItem -= Spawner.Instance.Player_PickingUpItem;

			_harmony.UnpatchAll();
			base.OnDisabled();
		}

		private void Server_ReloadedConfigs() => ItemSpawnIO.Reload();

		public static void CheckDoorItemSpawn(Door door) {
			var customDoor = door.GetCustomDoor();

			CheckRoomItemsSpawned(customDoor.Room1.Id);

			if (customDoor.HasTwoRooms) {
				CheckRoomItemsSpawned(customDoor.Room2.Id);
			}
		}

		private static void CheckRoomItemsSpawned(int id) {
			var room = SavedItemRoom.SavedRooms[id];

			if (room != null && !room.HasBeenEntered) {
				room.HasBeenEntered = true;
				room.SpawnSavedItems();
			}
		}
	}
}
