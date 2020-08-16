using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner {
	public class ItemSpawnPoint
	{
		public delegate void NotifyPointFree();
		public event NotifyPointFree OnNotifyPointFree;

		public CustomRoom CustomRoom => _fixedPoint.CustomRoom;
		public Vector3 Position => _fixedPoint.Position;
		public Quaternion Rotation => _fixedPoint.Rotation;

		private readonly FixedPoint _fixedPoint;

		public ItemSpawnPoint(FixedPoint fixedPoint)
		{
			_fixedPoint = fixedPoint;
		}

		public bool IsFree = true;

		public void SetFree()
		{
			IsFree = true;
			OnNotifyPointFree?.Invoke();
		}
	}
}
