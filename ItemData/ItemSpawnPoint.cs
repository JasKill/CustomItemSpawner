using ArithFeather.AriToolKit.PointEditor;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemData {
	public class ItemSpawnPoint
	{
		public Vector3 Position => _fixedPoint.Position;
		public Quaternion Rotation => _fixedPoint.Rotation;

		private readonly ItemRoom _itemRoom;
		private readonly FixedPoint _fixedPoint;

		public ItemSpawnPoint(ItemRoom itemRoom, FixedPoint fixedPoint)
		{
			_itemRoom = itemRoom;
			_fixedPoint = fixedPoint;
		}

		public bool IsFree = true;

		public void SetFree()
		{
			IsFree = true;
			_itemRoom.TriggerItemSetFree();
		}
	}
}
