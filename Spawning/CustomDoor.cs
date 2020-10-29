using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	internal class CustomDoor : MonoBehaviour
	{
		public SavedItemRoom Room1 { get; }
		public SavedItemRoom Room2 { get; }
		public bool HasTwoRooms { get; }

		private static readonly RaycastHit[] cachedRaycastHIts = new RaycastHit[1];

		private CustomDoor()
		{
			try
			{
				var upperPos = transform.position + Vector3.up / 2;

				if (Physics.RaycastNonAlloc(upperPos + transform.forward, Vector3.down, cachedRaycastHIts, 5,
					1 << 0) == 1)
				{
					var room1 = Map.FindParentRoom(cachedRaycastHIts[0].transform.gameObject);
					if (room1 != null)
						Room1 = SavedItemRoom.SavedRooms[room1.gameObject.GetInstanceID()];
				}

				if (Physics.RaycastNonAlloc(upperPos - transform.forward, Vector3.down, cachedRaycastHIts, 5,
					1 << 0) == 1)
				{
					var room2 = Map.FindParentRoom(cachedRaycastHIts[0].transform.gameObject);
					if (room2 != null)
						Room2 = SavedItemRoom.SavedRooms[room2.gameObject.GetInstanceID()];
				}

				HasTwoRooms = (Room1 != null && Room1.Room != null &&
				               Room2 != null && Room2.Room != null &&
				               Room1.Room.gameObject != Room2.Room.gameObject);
			}
			catch
			{
				Log.Error($"Error while trying to find parent rooms for {name}");
			}
		}
	}
}
