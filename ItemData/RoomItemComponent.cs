using System.Collections.Generic;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.Components;
using ArithFeather.CustomItemSpawner.Spawning;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemData
{
	public class RoomItemComponent
	{

		public RoomItemComponent(CustomRoom customRoom, List<ItemSpawnPoint> points, List<IItemObtainable> items)
		{
			CustomRoom = customRoom;
			Points = points;
			Items = items;
		}

		public delegate void RoomIsFree(RoomItemComponent room);
		public static event RoomIsFree OnRoomIsFree;

		public readonly CustomRoom CustomRoom;
		public readonly List<ItemSpawnPoint> Points;
		public readonly List<IItemObtainable> Items;

		public int MaxItemsAllowed { get; set; }

		public void TriggerItemSetFree()
		{
			if (AtMaxItemSpawns) {
				OnRoomIsFree?.Invoke(this);
			}

			_currentItemsSpawned--;
		}

		private int _currentItemsSpawned;
		public bool AtMaxItemSpawns => _currentItemsSpawned >= MaxItemsAllowed;

		public bool HasBeenEntered { get; set; }

		private readonly Stack<SavedSpawnInfo> _savedSpawns = new Stack<SavedSpawnInfo>();

		public void AddSavedSpawn(ItemSpawnPoint point, ItemType itemType)
		{
			_currentItemsSpawned++;
			point.IsFree = false;
			_savedSpawns.Push(new SavedSpawnInfo(point, itemType));
		}

		public void SpawnSavedItems()
		{
			var length = _savedSpawns.Count;
			for (int i = 0; i < length; i++)
			{
				var spawn = _savedSpawns.Pop();
				Spawner.SpawnItem(spawn.ItemSpawnPoint, spawn.ItemType);
			}
		}

		public readonly struct SavedSpawnInfo
		{

			public readonly ItemSpawnPoint ItemSpawnPoint;
			public readonly ItemType ItemType;

			public SavedSpawnInfo(ItemSpawnPoint point, ItemType itemType)
			{
				ItemSpawnPoint = point;
				ItemType = itemType;
			}
		}

		#region Start Game Spawning

		private int _indexer;

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Were we able to spawn a start item?</returns>
		public bool TrySpawnStartItem() {

			if (AtMaxItemSpawns) return false;

			while (_indexer < Items.Count && !Items[_indexer].HasItems)
			{
				_indexer++;
			}

			if (_indexer >= Items.Count) return false;

			AddSavedSpawn(GetRandomFreePoint(), Items[_indexer].GetItem());

			_indexer++;
			return true;
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

		#endregion
	}
}