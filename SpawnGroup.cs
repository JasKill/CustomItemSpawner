using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;

namespace ArithFeather.CustomItemSpawner {
	public class SpawnGroup
	{
		public delegate void RoomIsFree(SpawnGroup spawnGroup);
		public static event RoomIsFree OnRoomIsFree;

		public string Id { get; private set; }
		public List<IItemObtainable> Items { get; private set; }
		public List<ItemSpawnPoint> Points { get; private set; }
		public int MaxItemsAllowed { get; private set; }

		public bool AtMaxItemSpawns => _currentItemsSpawned >= MaxItemsAllowed;
		public bool SpawnedAllItems => _indexer >= Items.Count;

		private int _currentItemsSpawned;
		private int _indexer;

		public void Initialize(string id, List<IItemObtainable> items, List<ItemSpawnPoint> points) {
			Id = id;
			Items = items;
			Points = points;
			MaxItemsAllowed = points.Count;

			// Hook up to spawn point event
			var pointCount = points.Count;
			for (int i = 0; i < pointCount; i++)
			{
				points[i].OnNotifyPointFree += TriggerItemSetFree;
			}
		}

		public void TriggerItemSetFree() {
			if (AtMaxItemSpawns) {
				OnRoomIsFree?.Invoke(this);
			}

			_currentItemsSpawned--;
		}

		/// <returns>Were we able to spawn a start item?</returns>
		public bool TrySpawnStartItem() {
			while (!AtMaxItemSpawns && !SpawnedAllItems)
			{
				var nextItem = Items[_indexer];
				_indexer++;

				if (nextItem.HasItems) {
					SpawnItem(true, GetRandomFreePoint(), nextItem.GetItem());
					return true;
				}
			}

			return false;
		}       
		
		/// <returns>Were we able to spawn a start item?</returns>
		public bool TrySpawnItem(IItemObtainable item) {
			while (!AtMaxItemSpawns && !SpawnedAllItems) {
				_indexer++;

				if (item.HasItems) {
					SpawnItem(false, GetRandomFreePoint(), item.GetItem());
					return true;
				}
			}

			return false;
		}

		public void SpawnItem(bool savedSpawn, ItemSpawnPoint point, ItemData itemData) {
			_currentItemsSpawned++;
			point.IsFree = false;
			if (savedSpawn) SavedItemRoom.SavedRooms[point.CustomRoom.Id].SavedSpawns.Add(new SpawnInfo(point, itemData));
			else Spawner.SpawnItem(point, itemData);
		}

		public ItemSpawnPoint GetRandomFreePoint() {
			Points.UnityShuffle();
			var pointsLength = Points.Count;

			for (int n = 0; n < pointsLength; n++) {
				var spawnPoint = Points[n];

				if (spawnPoint.IsFree) {
					return spawnPoint;
				}
			}

			return null;
		}
	}
}
