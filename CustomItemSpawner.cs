using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Patches;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;
using HarmonyLib;
using Version = System.Version;

namespace ArithFeather.CustomItemSpawner {
	public class CustomItemSpawner : Plugin<Config> {

		public override string Author => "Arith";

		public override Version Version => new Version("2.06");

		private readonly Harmony _harmony = new Harmony("customItemSpawner");

		public delegate void PickedUpItem(ItemSpawnPoint itemSpawnPoint);
		public static event PickedUpItem OnPickedUpItem;

		public static IReadOnlyList<SavedItemType> ItemTypeList => ItemSpawnIO.ItemTypeList;
		public static IReadOnlyDictionary<string, ItemList> ItemListDictionary => ItemSpawnIO.ItemListDictionary;

		public static IReadOnlyDictionary<string, SpawnGroupData> SpawnGroupItemDictionary => ItemSpawnIO.SpawnGroupItemDictionary;
		public static IReadOnlyDictionary<string, SpawnGroupData> EndlessSpawnGroupItemDictionary => ItemSpawnIO.EndlessSpawnGroupItemDictionary;
		public static IReadOnlyDictionary<string, SpawnGroupData> ContainerGroupItemDictionary => ItemSpawnIO.ContainerGroupItemDictionary;

		public override void OnEnabled() {
			base.OnEnabled();

			ItemSpawnIO.Reload();

			_harmony.PatchAll();
			Exiled.Events.Handlers.Server.ReloadedConfigs += Server_ReloadedConfigs;
			PointAPI.OnLoadSpawnPoints += SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed += CheckDoorItemSpawn;
			DoorOpenEventPatch.OnDoorOpened += CheckDoorItemSpawn;

			Exiled.Events.Handlers.Server.WaitingForPlayers += Spawner.Instance.Reset;
			PickupDisableTrigger.OnPickedUpItem += PickupDisableTrigger_OnPickedUpItem;
		}

		private void PickupDisableTrigger_OnPickedUpItem(ItemSpawnPoint itemSpawnPoint) =>
			OnPickedUpItem?.Invoke(itemSpawnPoint);

		public override void OnDisabled() {
			Exiled.Events.Handlers.Server.ReloadedConfigs -= Server_ReloadedConfigs;
			PointAPI.OnLoadSpawnPoints -= SpawnPointCreator.OnLoadSpawnPoints;

			DestroyedDoorPatch.OnDoorDestroyed -= CheckDoorItemSpawn;
			DoorOpenEventPatch.OnDoorOpened -= CheckDoorItemSpawn;

			Exiled.Events.Handlers.Server.WaitingForPlayers -= Spawner.Instance.Reset;
			PickupDisableTrigger.OnPickedUpItem -= PickupDisableTrigger_OnPickedUpItem;

			_harmony.UnpatchAll();
			base.OnDisabled();
		}

		private void Server_ReloadedConfigs() => ItemSpawnIO.Reload();

		public static void CheckDoorItemSpawn(Door door)
		{
			if (SavedItemRoom.SavedRooms.Count == 0) return;

			var customDoor = door.GetCustomDoor();

			CheckRoomItemsSpawned(customDoor.Room1.Id);

			if (customDoor.HasTwoRooms) {
				CheckRoomItemsSpawned(customDoor.Room2.Id);
			}
		}

		public static void CheckRoomItemsSpawned(int id) {
			var room = SavedItemRoom.SavedRooms[id];

			if (room != null && !room.HasBeenEntered) {
				room.HasBeenEntered = true;
				room.SpawnSavedItems();
			}
		}
	}
}
