using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;

namespace ArithFeather.CustomItemSpawner {
	public class SpawnGroup {
		public delegate void RoomIsFree(SpawnGroup spawnGroup);
		public static event RoomIsFree OnRoomIsFree;

		public string Id { get; private set; }
		public List<IItemObtainable> Items { get; private set; }
		public List<ItemSpawnPoint> Points { get; private set; }
		public int MaxItemsAllowed { get; private set; }

		public void Initialize(string id, List<IItemObtainable> items, List<ItemSpawnPoint> points)
		{
			Id = id;
			Items = items;
			Points = points;
			MaxItemsAllowed = points.Count;
		}

		public void TriggerItemSetFree() {
			if (AtMaxItemSpawns) {
				OnRoomIsFree?.Invoke(this);
			}

			_currentItemsSpawned--;
		}

		private int _indexer;
		/// <returns>Were we able to spawn a start item?</returns>
		public bool TrySpawnStartItem() {

			if (AtMaxItemSpawns) return false;

			while (_indexer < Items.Count && !Items[_indexer].HasItems) {
				_indexer++;
			}

			if (_indexer >= Items.Count) return false;

			SpawnItem(true, GetRandomFreePoint(), Items[_indexer].GetItem());

			_indexer++;
			return true;
		}

		public void SpawnItem(bool savedSpawn, ItemSpawnPoint point, ItemType itemType) {

			if (itemType == ItemType.MicroHID) {Log.Error("spawning");}
			_currentItemsSpawned++;
			point.IsFree = false;
			if (savedSpawn) SavedItemRoom.SavedRooms[point.CustomRoom.Id].SavedSpawns.Add(new SpawnInfo(point, itemType));
			else Spawner.SpawnItem(point, itemType);
		}

		private ItemSpawnPoint GetRandomFreePoint() {

			var pointsLength = Points.Count;
			Points.UnityShuffle();

			for (int n = 0; n < pointsLength; n++) {
				var spawnPoint = Points[n];

				if (spawnPoint.IsFree) {
					return spawnPoint;
				}
			}

			return null;
		}

		private int _currentItemsSpawned;
		public bool AtMaxItemSpawns => _currentItemsSpawned >= MaxItemsAllowed;
	}
}
