using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches {
	[HarmonyPatch(typeof(LockerManager), "Generate")]
	internal static class OnContainerGeneratePatch {

		private static void Prefix(int seed)
		{
			SpawnPointCreator.OnLoadContainers(seed);
		}
	}
}
