using System.Collections.Generic;

namespace ArithFeather.CustomItemSpawner.ItemListTypes
{
	public class SpawnGroupData
	{
		public List<string> Owners = new List<string>();
		public List<IItemObtainable> ItemLists = new List<IItemObtainable>();
		public List<IItemObtainable> Items = new List<IItemObtainable>();
	}
}
