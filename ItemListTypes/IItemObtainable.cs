namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public interface IItemObtainable
	{
		ItemData GetItem();

		bool HasItems { get; }
	}
}