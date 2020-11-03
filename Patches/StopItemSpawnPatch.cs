using HarmonyLib;

namespace ArithFeather.CustomItemSpawner.Patches
{
	[HarmonyPatch(typeof(HostItemSpawner), "Spawn")]
	internal static class StopItemSpawnPatch
	{
		private static bool Prefix()
		{
			if (!CustomItemSpawner.Configs.IsEnabled) return true;
			return false;
		}
	}
}
