using System.Collections.Generic;
using MEC;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class Spawner {
		public static readonly Spawner Instance = new Spawner();

		private static List<SpawnGroup> Rooms => SpawnPointCreator.SpawnGroups;
		public readonly List<SpawnGroup> FreeRooms = new List<SpawnGroup>();

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

			FreeRooms.ShuffleList();

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

			if (FreeRooms.Count != 0)
				SpawnStartItems();
		}

		#region Static spawning methods

		private static Inventory _cachedInventory;
		public static PickupDisableTrigger SpawnItem(ItemSpawnPoint point, ItemData itemType) {
			if (itemType.Item == ItemType.None) return null;

			var pickup = _cachedInventory.SetPickup(itemType.Item, -4.65664672E+11f, point.Position, point.Rotation, 0, 0, 0);

			if (CustomItemSpawner.Configs.EnableItemTracking) {
				var listener = pickup.gameObject.AddComponent<PickupDisableTrigger>();
				listener.Initialize(point);
				return listener;
			}

			return null;
		}

		public static void SpawnItems(List<SpawnInfo> spawns) => Timing.RunCoroutine(StaggerSpawnItems(spawns), Segment.FixedUpdate);

		private static IEnumerator<float> StaggerSpawnItems(IReadOnlyList<SpawnInfo> spawns) {
			var spawnCount = spawns.Count;
			for (int i = 0; i < spawnCount; i++) {
				var spawn = spawns[i];
				var copies = spawn.ItemData.Copies;

				var copyList = new List<PickupDisableTrigger>(copies);

				for (int j = 0; j < copies; j++) {
					copyList.Add(SpawnItem(spawn.ItemSpawnPoint, spawn.ItemData));
					yield return Timing.WaitForOneFrame;
				}

				for (int j = 0; j < copies; j++)
				{
					var trig1 = copyList[j];

					for (int k = 0; k < copies; k++)
					{
						var trig2 = copyList[k];

						if (j != k)
						{
							trig1.Copies.Add(trig2);
						}
					}
				}
			}
		}

		#endregion

		#region Endless Spawning

		public void SpawnGroup_OnRoomIsFree(SpawnGroup spawnGroup) => FreeRooms.Insert(Random.Range(0, FreeRooms.Count), spawnGroup);

		public void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev) {
			if (CustomItemSpawner.Configs.EnableItemTracking) ev.Pickup.GetComponent<PickupDisableTrigger>()?.PickedUp();
		}

		#endregion
	}
}
