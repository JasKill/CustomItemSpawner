using Exiled.API.Features;
using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(Door), "SetState")]
	internal static class DoorOpenEventPatch {
		private static readonly Harmony Harmony = new Harmony("doorOpen");
		public static void Enable() => Harmony.PatchAll();
		public static void Disable() => Harmony.UnpatchAll();

		public delegate void DoorOpened(Door door);
		public static event DoorOpened OnDoorOpened;

		private static int _lastDoorTriggered;
		private static bool Prefix(bool open, Door __instance) {
			if (open && Round.IsStarted) {
				var id = __instance.GetInstanceID();
				if (id != _lastDoorTriggered) {
					_lastDoorTriggered = id;
					OnDoorOpened?.Invoke(__instance);
				}
			}

			return true;
		}
	}
}
