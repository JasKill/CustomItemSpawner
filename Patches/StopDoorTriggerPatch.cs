using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(DoorTriggerManager), "Trigger", typeof(string))]
	internal static class StopDoorTriggerPatch {
		private static readonly Harmony Harmony = new Harmony("doorTrigger");
		public static void Enable() => Harmony.PatchAll();
		public static void Disable() => Harmony.UnpatchAll();

		private static bool Prefix() {
			return false;
		}
	}
}
