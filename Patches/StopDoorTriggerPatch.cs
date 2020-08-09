using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(DoorTriggerManager), "Trigger", typeof(string))]
	internal static class StopDoorTriggerPatch {
		private static bool Prefix() {
			return false;
		}
	}
}
