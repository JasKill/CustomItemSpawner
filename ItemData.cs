namespace ArithFeather.CustomItemSpawner
{
	public readonly struct ItemData
	{
		public readonly ItemType Item;
		public readonly int Copies;

		public ItemData(ItemType item, int copies)
		{
			Item = item;
			Copies = copies;
		}
	}
}
