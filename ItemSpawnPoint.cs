using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner {
	public class ItemSpawnPoint
	{
		public CustomRoom CustomRoom => _fixedPoint.CustomRoom;
		public Vector3 Position => _fixedPoint.Position;
		public Quaternion Rotation => _fixedPoint.Rotation;

		private readonly SpawnGroup _spawnGroup;
		private readonly FixedPoint _fixedPoint;

		public ItemSpawnPoint(SpawnGroup spawnGroup, FixedPoint fixedPoint)
		{
			_spawnGroup = spawnGroup;
			_fixedPoint = fixedPoint;
		}

		public bool IsFree = true;

		public void SetFree()
		{
			IsFree = true;
			_spawnGroup.TriggerItemSetFree();
		}
	}
}
