using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches
{
	[HarmonyPatch(typeof(LockerManager), "Generate")]
	internal static class OnContainerGeneratePatch
	{

		private static void Prefix()
		{
			if (!CustomItemSpawner.Configs.IsEnabled) return;
			SpawnPointCreator.OnLoadContainers();
		}
	}
}
