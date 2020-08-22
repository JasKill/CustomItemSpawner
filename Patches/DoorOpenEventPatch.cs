using Exiled.API.Features;
using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(Door), "SetState")]
	internal static class DoorOpenEventPatch {
		public delegate void DoorOpened(Door door);
		public static event DoorOpened OnDoorOpened;

		private static int _lastDoorTriggered;

		private static void Prefix(bool open, Door __instance) {
			if (!CustomItemSpawner.Configs.IsEnabled) return;

			if (!__instance.NetworkisOpen && open && Round.IsStarted) {

				var id = __instance.GetInstanceID();

				if (id != _lastDoorTriggered) {
					_lastDoorTriggered = id;
					OnDoorOpened?.Invoke(__instance);
				}
			}
		}
	}
}
