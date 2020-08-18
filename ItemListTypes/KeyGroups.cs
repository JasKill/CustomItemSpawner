using System;
using System.Collections.Generic;
using Exiled.API.Features;

namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public interface IKeyGroups
	{
		List<string> GetGroups();
	}

	public class KeyGroups : IKeyGroups
	{
		public readonly List<IKeyGroups> Groups = new List<IKeyGroups>();

		public List<string> GetGroups() {
			var groupSize = Groups.Count;

			var listOfKeys = new List<string>();
			for (int i = 0; i < groupSize; i++) {
				listOfKeys.AddRange(Groups[i].GetGroups());
			}

			return listOfKeys;
		}
	}

	public class StringKey : IKeyGroups
	{
		private readonly List<string> _key;

		public StringKey(string key)
		{
			_key = new List<string>{key};
		}

		public List<string> GetGroups() => _key;
	}
}
