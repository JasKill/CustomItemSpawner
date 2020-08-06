using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemData {
	public class ItemSpawnPoint
	{
		public readonly Vector3 Position;
		public readonly Quaternion Rotation;

		private readonly RoomItemComponent _room;

		public ItemSpawnPoint(RoomItemComponent room, Vector3 position, Vector3 rotation)
		{
			_room = room;
			Position = position;
			Rotation = Quaternion.Euler(rotation);
		}

		public bool IsFree = true;

		public void SetFree()
		{
			IsFree = true;
			_room.TriggerItemSetFree();
		}
	}
}
