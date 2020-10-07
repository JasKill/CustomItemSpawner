using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning
{
	internal class CustomDoor : MonoBehaviour
	{
		public Door Door { get; }
		public SavedItemRoom Room1 { get; }
		public SavedItemRoom Room2 { get; }
		public bool HasTwoRooms { get; }

		private static readonly RaycastHit[] cachedRaycastHIts = new RaycastHit[1];

		private CustomDoor()
		{
			Door = GetComponent<Door>();
			var upperPos = transform.position + Vector3.up / 2;

			if (Physics.RaycastNonAlloc(upperPos + transform.forward, Vector3.down, cachedRaycastHIts, 5,
				1 << 0) == 1)
			{
				Room1 = cachedRaycastHIts[0].transform.GetComponentInParent<SavedItemRoom>();
			}

			if (Physics.RaycastNonAlloc(upperPos - transform.forward, Vector3.down, cachedRaycastHIts, 5,
				1 << 0) == 1)
			{
				Room2 = cachedRaycastHIts[0].transform.GetComponentInParent<SavedItemRoom>();
			}

			HasTwoRooms = Room1?.gameObject != Room2?.gameObject;
		}
	}
}
