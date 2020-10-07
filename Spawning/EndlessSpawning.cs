using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.Points;
using ArithFeather.Points.Tools;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	public static class EndlessSpawning
	{
		/// <summary>
		/// Enables the creation of components on items to track if they have been picked up.
		/// </summary>
		public static bool EnableItemTracking { get; private set; }

		private static Dictionary<string, List<IItemObtainable>> EndlessSpawningItemsDictionary =>
			SpawnPointCreator.EndlessSpawningItemsDictionary;

		private static Dictionary<string, SpawnGroup> SpawnGroupDictionary =>
			SpawnPointCreator.SpawnGroupDictionary;

		public static void Enable()
		{
			EnableItemTracking = true;
			Exiled.Events.Handlers.Player.PickingUpItem += Player_PickingUpItem;
		}

		public static void Disable()
		{
			EnableItemTracking = false;
			Exiled.Events.Handlers.Player.PickingUpItem -= Player_PickingUpItem;
		}

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

				itemList.UnityShuffle();

				var listSize = itemList.Count;
				for (var i = 0; i < listSize; i++)
				{
					if (!spawnGroup.TrySpawnItem(itemList[i]))
					{
						return;
					}
				}
			}
		}

		private static void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev)
		{
			if (EnableItemTracking)
			{
				ev.Pickup.GetComponent<PickupDisableTrigger>()?.PickedUp();
			}
		}
	}
}
