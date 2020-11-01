using System.Collections.Generic;
using MEC;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	internal class Spawner
	{
		public static readonly Spawner Instance = new Spawner();

		private static List<SpawnGroup> SpawnGroups => SpawnPointCreator.SpawnGroups;

		/// <summary>
		/// Called on WaitingForPlayers.
		/// Spawns start-game items.
		/// </summary>
		public void Reset()
		{
			_cachedInventory = ReferenceHub.HostHub.inventory;

			var groupCount = SpawnGroups.Count;
			for (int i = 0; i < groupCount; i++)
			{
				SpawnGroups[i].SpawnStartItem();
			}
		}

		private static Inventory _cachedInventory;
		public static PickupDisableTrigger SpawnItem(ItemSpawnPoint point, ItemData itemType)
		{
			if (itemType.Item == ItemType.None) return null;

			// -4.65664672E+11f Not sure what this is for durability.

			var pickup = _cachedInventory.SetPickup(itemType.Item, ReferenceHub.LocalHub.inventory.availableItems[(int)itemType.Item].durability, point.Position, point.Rotation, 0, 0, 0);

			if (EndlessSpawning.EnableItemTracking)
			{
				var listener = pickup.gameObject.AddComponent<PickupDisableTrigger>();
				listener.Initialize(point);
				return listener;
			}

			return null;
		}

		public static void SpawnItems(List<SpawnInfo> spawns) => Timing.RunCoroutine(StaggerSpawnItems(spawns), Segment.FixedUpdate);

		private static IEnumerator<float> StaggerSpawnItems(IReadOnlyList<SpawnInfo> spawns)
		{
			var spawnCount = spawns.Count;
			for (int i = 0; i < spawnCount; i++)
			{
				var spawn = spawns[i];
				var copies = spawn.ItemData.Copies;

				var copyList = new List<PickupDisableTrigger>(copies);

				for (int j = 0; j < copies; j++)
				{
					copyList.Add(SpawnItem(spawn.ItemSpawnPoint, spawn.ItemData));

					if (j + 1 < copies)
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

				yield return Timing.WaitForOneFrame;
			}
		}
	}
}
