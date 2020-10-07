using System.Collections.Generic;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using ArithFeather.Points.Tools;
using Exiled.API.Features;

namespace ArithFeather.CustomItemSpawner
{
	public class SpawnGroup
	{
		public delegate void GroupIsFree(SpawnGroup spawnGroup);
		public static event GroupIsFree OnGroupIsFree;

		public string Id { get; }
		private List<IItemObtainable> Items { get; }
		private List<ItemSpawnPoint> Points { get; }
		public int MaxItemsAllowed { get; }

		public bool AtMaxItemSpawns => _currentItemsSpawned >= MaxItemsAllowed;
		public bool SpawnedAllItems => _indexer >= Items.Count;

		private int _currentItemsSpawned;
		private int _indexer;

		public SpawnGroup(string id, List<IItemObtainable> items, List<ItemSpawnPoint> points)
		{
			Id = id;
			Items = items ?? (new List<IItemObtainable>());
			Points = points;
			MaxItemsAllowed = points.Count;

			// Hook up to spawn point event
			var pointCount = points.Count;
			for (int i = 0; i < pointCount; i++)
			{
				points[i].OnNotifyPointFreedom += SpawnGroup_OnNotifyPointFreedom;
			}
		}

		private void SpawnGroup_OnNotifyPointFreedom(bool isFree)
		{
			if (isFree)
			{
				_currentItemsSpawned--;

				if (AtMaxItemSpawns)
					OnGroupIsFree?.Invoke(this);
			}
			else
			{
				_currentItemsSpawned++;
			}
		}

		public void SpawnStartItem()
		{
			while (!AtMaxItemSpawns && !SpawnedAllItems)
			{
				var nextItem = Items[_indexer];
				_indexer++;

				SpawnItem(true, GetRandomFreePoint(), nextItem.GetItem());
			}
		}

		/// <returns>Were we able to spawn a start item?</returns>
		public bool TrySpawnItem(IItemObtainable item)
		{
			if (AtMaxItemSpawns) return false;

			SpawnItem(false, GetRandomFreePoint(), item.GetItem());
			return true;
		}

		private void SpawnItem(bool savedSpawn, ItemSpawnPoint point, ItemData itemData)
		{
			point.IsFree = false;
			if (savedSpawn)
			{
				if (SavedItemRoom.SavedRooms.TryGetValue(point.Room.Transform.gameObject.GetInstanceID(), out var savedRoom))
					savedRoom.SavedSpawns.Add(new SpawnInfo(point, itemData));
				else
					Log.Error("Could not located [SavedItemRoom.SavedRoom].");
			}
			else
			{
				Spawner.SpawnItem(point, itemData);
			}
		}

		public ItemSpawnPoint GetRandomFreePoint()
		{
			Points.UnityShuffle();
			var pointsLength = Points.Count;

			for (int n = 0; n < pointsLength; n++)
			{
				var spawnPoint = Points[n];

				if (spawnPoint.IsFree)
				{
					return spawnPoint;
				}
			}

			return null;
		}
	}
}
