using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using Log = Exiled.API.Features.Log;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public static class EndlessSpawning {
		/// <summary>
		/// Enables the creation of components on items to track if they have been picked up.
		/// </summary>
		public static bool EnableItemTracking { get; private set; }

		private static List<SpawnGroup> FreeRooms => 
			Spawner.Instance.FreeRooms;

		private static Dictionary<string, List<IItemObtainable>> EndlessSpawningItemsDictionary =>
			SpawnPointCreator.EndlessSpawningItemsDictionary;

		private static Dictionary<string, SpawnGroup> SpawnGroupDictionary =>
			SpawnPointCreator.SpawnGroupDictionary;

		public static void Enable() {
			EnableItemTracking = true;
			SpawnGroup.OnRoomIsFree += SpawnGroup_OnRoomIsFree;
			Exiled.Events.Handlers.Player.PickingUpItem += Player_PickingUpItem;
		}

		public static void Disable() {
			EnableItemTracking = false;
			SpawnGroup.OnRoomIsFree -= SpawnGroup_OnRoomIsFree;
			Exiled.Events.Handlers.Player.PickingUpItem -= Player_PickingUpItem;
		}

		///// <summary>
		///// Resets a specific queued list by re-randomizing and resetting its index to 0.
		///// </summary>
		///// <param name="list"></param>
		//public static void ResetQueuedList(string list)
		//{
		//	if (ItemSpawnIO.QueuedListDictionary.TryGetValue(list, out var queuedList))
		//	{
		//		queuedList.Reset();
		//	}
		//}

		/// <summary>
		/// Spawns all endless group items inside this group.
		/// </summary>
		/// <param name="groupName"></param>
		public static void SpawnItemsInEndlessGroup(string groupName)
		{
			// Spawning items for a specific room.
			if (EndlessSpawningItemsDictionary.TryGetValue(groupName, out var itemList) &&
			    SpawnGroupDictionary.TryGetValue(groupName, out var spawnGroup) &&
			    !spawnGroup.AtMaxItemSpawns)
			{
				var listSize = itemList.Count;
				for (var i = 0; i < listSize; i++)
				{
					if (spawnGroup.AtMaxItemSpawns) return;

					var nextItem = itemList[i];

					if (nextItem.HasItems) {
						spawnGroup.SpawnItem(false, spawnGroup.GetRandomFreePoint(), nextItem.GetItem());
					}
				}
			}
		}

		private static void SpawnGroup_OnRoomIsFree(SpawnGroup spawnGroup) => FreeRooms.Insert(Random.Range(0, FreeRooms.Count), spawnGroup);

		private static void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev) {
			if (EnableItemTracking) {
				ev.Pickup.GetComponent<PickupDisableTrigger>()?.PickedUp();
			}
		}
	}
}
