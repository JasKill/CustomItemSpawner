using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(Door), "DestroyDoor")]
	internal static class DestroyedDoorPatch {
		private static readonly Harmony Harmony = new Harmony("DestroyedDoor");
		public static void Enable() => Harmony.PatchAll();
		public static void Disable() => Harmony.UnpatchAll();

		public delegate void DoorDestroyed(Door door);
		public static event DoorDestroyed OnDoorDestroyed;

		private static int lastDoor;

		private static void Postfix(Door __instance) {
			var doorId = __instance.GetInstanceID();
			if (doorId != lastDoor) {
				lastDoor = doorId;
				lastDoor = __instance.GetInstanceID();
				OnDoorDestroyed?.Invoke(__instance);
			}
		}
	}
}
