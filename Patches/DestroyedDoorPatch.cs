using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(Door), "DestroyDoor")]
	internal static class DestroyedDoorPatch {
		public delegate void DoorDestroyed(Door door);
		public static event DoorDestroyed OnDoorDestroyed;

		private static int _lastDoor;

		private static void Prefix(bool b, Door __instance)
		{
			if (!CustomItemSpawner.Configs.IsEnabled) return;

			if (b && __instance.destroyedPrefab != null && __instance.doorType != global::Door.DoorTypes.HeavyGate && !__instance.Networkdestroyed) {

				var doorId = __instance.GetInstanceID();
				if (doorId != _lastDoor) {
					_lastDoor = doorId;
					_lastDoor = __instance.GetInstanceID();
					OnDoorDestroyed?.Invoke(__instance);
				}
			}
		}
	}
}
