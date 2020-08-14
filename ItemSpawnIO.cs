using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using Exiled.API.Features;
using UnityEngine;
using SpawnPoint = ArithFeather.AriToolKit.PointEditor.Point;

namespace ArithFeather.CustomItemSpawner {
	internal static class ItemSpawnIO {
		private const string PointDataFileName = "ItemSpawnPoints";
		private const string ItemDataFileName = "ItemSpawnInfo";

		private static readonly string ItemDataFilePath = Path.Combine(Paths.Configs, ItemDataFileName) + ".txt";
		private static readonly string PointDataFilePath = Path.Combine(PointIO.FolderPath, PointDataFileName) + ".txt";

		public static PointList SpawnPointList;

		// Used for recursive deserialization
		private static readonly List<SavedItemType> ItemTypeList = new List<SavedItemType>();
		private static readonly Dictionary<string, QueuedList> QueuedListDictionary = new Dictionary<string, QueuedList>();
		private static readonly Dictionary<string, ItemList> ItemListDictionary = new Dictionary<string, ItemList>();
		// Required to make the round data
		public static readonly Dictionary<string, SpawnGroupData> SpawnGroupItemDictionary = new Dictionary<string, SpawnGroupData>();
		public static readonly Dictionary<string, SpawnGroupData> ContainerGroupItemDictionary = new Dictionary<string, SpawnGroupData>();

		// Used for randomizing queue lists
		public static readonly List<QueuedList> QueuedListList = new List<QueuedList>();

		private static bool ItemFileExists { get; set; }
		private static bool SpawnFileExists { get; set; }

		public static void Reload() {
			LoadItemData();

			SpawnFileExists = FileManager.FileExists(PointDataFilePath);
			SpawnPointList = PointAPI.GetPointList(PointDataFileName);
		}

		public static void CheckFiles() {
			if (!ItemFileExists) CreateItemDataFile();
			if (!SpawnFileExists) CreateDefaultSpawnPointsFile();
		}

		#region Saving Data

		private static void CreateDefaultSpawnPointsFile() {
			Log.Warn("Creating new Spawn Point file using default spawn points.");

			var ris = RandomItemSpawner.singleton;

			// Save Position data
			var positionData = ris.posIds;
			var positionDataLength = positionData.Length;

			var spawnPoints = SpawnPointList.RawPoints;

			for (int i = 0; i < positionDataLength; i++) {
				var dat = positionData[i];
				var itemTransform = dat.position;

				var room = itemTransform.GetComponentInParent<CustomRoom>();

				if (room == null) {
					Log.Error($"Could not find Custom Room for {dat.posID}");
					continue;
				}

				var roomTransform = room.gameObject.transform;

				var localItemPosition = roomTransform.InverseTransformPoint(itemTransform.position);
				var localItemRotation = roomTransform.InverseTransformDirection(itemTransform.eulerAngles);

				spawnPoints.Add(new SpawnPoint(dat.posID.ToLowerInvariant(), room.Room.RoomType, room.Room.Zone, localItemPosition,
					localItemRotation));
			}

			SpawnFileExists = true;
			PointIO.Save(SpawnPointList, PointDataFilePath);
			SpawnPointList.FixData();
		}


		private const string ItemSpawnTypesDescription =
			"# How to use ItemSpawnInfo.txt\n" +
			"\n" +
			"# Spawn Groups\n" +
			"# Spawn Groups are IDs used to link your 'Spawn Points' to the 'Items lists'.\n" +
			"# Example: This will tell the key: 'lcz_armory_frags' to spawn 3 Frag grenades. \n" +
			"# lcz_armory_frags:25,25,25\n" +
			"# Note: The spawn points you make must use the same key 'lcz_armory_frags'.\n" +
			"\n" +
			"# When the server spawns items, it will:\n" +
			"# Go through all the 'Spawn Groups' and tell each group to spawn an item until ALL groups have spawned their items (Or no more spawn points).\n" +
			"\n" +
			"# Each 'Spawn group' will spawn its items in order:\n" +
			"\n" +
			"# First: Spawn all items assigned to spawn. (* and 0 to 36)\n" +
			"# Example: This will force the HID room to spawn the MicroHID.\n" +
			"# [Spawn Groups]\n" +
			"# Hid:16\n" +
			"\n" +
			"# Second: Spawn an item from a 'Queued List' (Until the Queued list is empty).\n" +
			"# Example, this will make sure at least 2 checkpoint key cards will spawn somewhere in LCZ.\n" +
			"# [Queued Lists]\n" +
			"# SpawnLCZ:3,3\n" +
			"# [Spawn Groups]\n" +
			"# LCZ_Toilets:SpawnLCZ\n" +
			"# LCZ_372:SpawnLCZ\n" +
			"# LCZ_Cafe:SpawnLCZ\n" +
			"# LCZ_173:SpawnLCZ\n" +
			"\n" +
			"# Third: Any 'Item Lists' you attached to the 'Spawn Group' will spawn a random item from that list.\n" +
			"# Example: You can use this for rarities.\n" +
			"# [Item Lists]\n" +
			"# HighRarity:21,25\n" +
			"# LowRarity:12,14,15\n" +
			"# [Spawn Groups]\n" +
			"# LCZ_Armory:LowRarity,LowRarity,LowRarity,HighRarity,HighRarity\n" +
			"# (This will spawn 3 random items from the LowRarity list and 2 items from the HighRarity list in Light Containment Armory.\n" +
			"\n" +
			"# -Again, the difference between a Queued List and Item List is: A Queued list will spawn all the items inside it, across all the groups it is attached to. Where an Item List will only spawn 1 random item inside the list.\n" +
			"# -You can add an Item List to a Queued List, but you can't add a Queued List to an Item List, or an Item List to an Item List.\n" +
			"# -For spawn points inside duplicate rooms (like Plant Room), the items will be split across those rooms.\n" +
			"\n" +
			"# *NEW* Modifiers\n" +
			"# % Chance To Spawn -You can give your Lists and Items a chance to spawn (1-100%).\n" +
			"# # Copies          -You can create more than one item per spawn point (1-20).\n" +
			"\n" +
			"# Example: When the game tells this group to spawn a pistol, there's a 50% chance it will spawn two pistols!\n" +
			"# RandomPistol:13%50#2\n\n";

		private static void CreateItemDataFile() {
			Log.Warn("Creating new ItemSpawnInfo file using default items.");

			using (var writer = new StreamWriter(File.Create(ItemDataFilePath))) {

				// Description
				writer.Write(ItemSpawnTypesDescription);

				// Display Items
				writer.WriteLine("# [Items]\n");
				writer.WriteLine("# *=Random Item");
				var names = Enum.GetNames(typeof(ItemType));
				var nameLength = names.Length;

				for (int i = 0; i < nameLength; i++) {
					writer.WriteLine($"# {i}={names[i]}");
				}

				writer.WriteLine();

				// Display Lists

				writer.WriteLine("[Item Lists]");
				writer.WriteLine("# Example lists below");
				writer.WriteLine();
				writer.WriteLine("Garbage:0,1,15,19,22,26,28,29,35");
				writer.WriteLine("Common:2,3,4,12,14,23,25,33,34");
				writer.WriteLine("Uncommon:5,6,17,18,24,30,31,32");
				writer.WriteLine("Rare:7,8,9,13,20,21");
				writer.WriteLine("VeryRare:10,11,16");
				writer.WriteLine();

				// Display Queued Lists

				writer.WriteLine("[Queued Lists]");
				writer.WriteLine("# Example queues below");
				writer.WriteLine();
				writer.WriteLine("SpawnOneOfEachItem:3,2,3,13,Garbage,Garbage,Uncommon,Common");
				writer.WriteLine();

				// Display Spawn Groups

				writer.WriteLine("[Spawn Groups]");
				writer.WriteLine("# Group Name : Item Data (1,6,Rare,Uncommon,etc)");
				writer.WriteLine();

				// Create default Groups
				var defaultItems = RandomItemSpawner.singleton.pickups;
				var defaultItemsSize = defaultItems.Length;

				var itemTypes = Enum.GetNames(typeof(ItemType));

				var keyCounter = new Dictionary<string, StringBuilder>();

				for (int i = 0; i < defaultItemsSize; i++) {
					var item = defaultItems[i];

					var key = item.posID;

					string itemId = string.Empty;

					// Find item ID
					for (int j = 0; j < SavedItemType.ItemTypeLength; j++) {
						if (itemTypes[j].Equals(item.itemID.ToString(), StringComparison.InvariantCultureIgnoreCase))
							itemId = j.ToString();
					}

					if (!string.IsNullOrWhiteSpace(itemId) && keyCounter.TryGetValue(key, out var value)) {
						value.Append($",{itemId}");
					} else {
						keyCounter.Add(key, new StringBuilder($"{key}:{itemId}"));
					}
				}

				foreach (var stringPair in keyCounter) {
					writer.WriteLine(stringPair.Value.ToString());
				}

				// Containers

				writer.WriteLine();
				writer.WriteLine("[Containers]");
				writer.WriteLine("# Containers have a max number of spawn points, listed below with a description.");
				writer.WriteLine();

				var containerDescription = new Dictionary<string, string>()
				{
					{"medkit", "# Max Spawns: [5] -First aid box."},
					{"glockere11", "# Max Spawns: [1] -Large container that spawns GunE11SR."},
					{"glocker556", "# Max Spawns: [5] -Ammo spawn point for the GunE11SR."},
					{"glocker-b-small", "# Max Spawns: [9] -Large 3x3 locker. (Small boxes)."},
					{"glocker-b-big", "# Max Spawns: [6] -Large 3x3 locker. (big boxes)."},
					{"pedestal", "# Max Spawns: [8] -The glass boxes."},
					{"misclocker", "# Max Spawns: [24] -The basic locker."}
				};

				var keySortedDictionary = new Dictionary<string, List<SpawnableItem>>();

				var lockerItems = LockerManager.singleton.items;
				var lockerItemCount = lockerItems.Length;

				for (int i = 0; i < lockerItemCount; i++) {
					var lockerItem = lockerItems[i];
					var key = lockerItem.itemTag;

					var item = new SpawnableItem {
						chanceOfSpawn = lockerItem.chanceOfSpawn,
						copies = lockerItem.copies,
						inventoryId = lockerItem.inventoryId,
						name = lockerItem.name
					};

					if (keySortedDictionary.TryGetValue(key, out var itemList)) {
						itemList.Add(item);
					} else keySortedDictionary.Add(key, new List<SpawnableItem> { item });
				}

				foreach (var containers in keySortedDictionary) {
					var key = containers.Key;
					var itemList = containers.Value;
					var itemListCount = itemList.Count;

					if (itemListCount == 0) continue;

					StringBuilder s = new StringBuilder();

					for (int i = 0; i < itemListCount; i++) {
						var item = itemList[i];

						s.Append($",{(int)item.inventoryId}");
						if (item.chanceOfSpawn < 100) s.Append($"%{item.chanceOfSpawn}");
						if (item.copies > 0) s.Append($"#{item.copies + 1}");
					}

					writer.WriteLine($"{key}:{s.ToString().TrimStart(',')}");
					writer.WriteLine(containerDescription[key]);
					writer.WriteLine();
				}
			}

			LoadItemData();
		}

		#endregion

		#region Loading Data

		private static void LoadItemData() {
			ItemFileExists = FileManager.FileExists(ItemDataFilePath);
			if (!ItemFileExists) return;

			LoadedItemData.Clear();
			ItemTypeList.Clear();
			QueuedListDictionary.Clear();
			ItemListDictionary.Clear();
			SpawnGroupItemDictionary.Clear();
			QueuedListList.Clear();
			ContainerGroupItemDictionary.Clear();

			using (var reader = File.OpenText(ItemDataFilePath)) {

				// Create SavedItemType instances for dictionary
				ItemTypeList.Add(new SavedItemType()); // Wildcard
				for (int i = 0; i < SavedItemType.ItemTypeLength; i++) {
					ItemTypeList.Add(i == 36 ? new SavedItemType(ItemType.None) : new SavedItemType((ItemType)i));
				}

				_lastFoundSection = Section.None;

				while (!reader.EndOfStream) {
					var line = reader.ReadLine();

					if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

					line = line.ToLowerInvariant();
					LoadedItemData.Add(line); // Save for later parsing

					if (CheckForSection(line)) continue;

					var sData = line.Split(':');
					var sDataLength = sData.Length;

					if (sDataLength != 2) continue;

					var key = sData[0].Trim();

					if (_lastFoundSection == Section.ItemLists && !ItemListDictionary.ContainsKey(key))
						ItemListDictionary.Add(key, new ItemList());

					else if (_lastFoundSection == Section.QueuedLists && !QueuedListDictionary.ContainsKey(key)) {
						var qList = new QueuedList();
						QueuedListList.Add(qList);
						QueuedListDictionary.Add(key, qList);
					}
				}
			}

			SecondPass();
		}

		private static void SecondPass() {
			_lastFoundSection = Section.None;
			var textFileLength = LoadedItemData.Count;

			for (int i = 0; i < textFileLength; i++) {
				var line = LoadedItemData[i];

				if (CheckForSection(line)) continue;

				var sData = line.Split(':');
				var sDataLength = sData.Length;

				if (sDataLength < 2) continue;

				var key = sData[0].Trim();

				if (sDataLength > 2) {
					SectionKeyError(key, "Too many ':' splitters.");
					continue;
				}

				var data = sData[1].Split(',');
				var dataLength = data.Length;

				if (dataLength == 0) continue;

				switch (_lastFoundSection) {

					case Section.ItemLists:

						if (ItemListDictionary.TryGetValue(key, out var itemList)) {

							var theList = new List<IItemObtainable>(dataLength);

							for (int k = 0; k < dataLength; k++) {
								var rawItem = data[k].Trim();

								if (!ParseKeyGetInstance(rawItem, out KeyData keyData, out IItemObtainable instance)) {
									if (!string.IsNullOrWhiteSpace(rawItem)) SectionKeyError(key, $"Regex could not parse [{rawItem}]");
									continue;
								}

								if (instance.GetType() != typeof(SavedItemType)) {
									SectionKeyError(key,
										$"Failed to add {keyData.Key}. You can only add Items to Item Lists");
								} else {
									theList.Add(new SpawnChanceWrapper(instance, keyData.Chance, keyData.Copies));
								}
							}

							if (theList.Count != 0) {
								itemList.Items = theList;
								continue;
							}
						}

						ListNotExistError(key);
						ItemListDictionary.Remove(key);

						break;

					case Section.QueuedLists:

						if (QueuedListDictionary.TryGetValue(key, out var queuedList)) {

							var theList = new List<IItemObtainable>(dataLength);

							for (int k = 0; k < dataLength; k++) {
								var rawItem = data[k].Trim();

								if (!ParseKeyGetInstance(rawItem, out KeyData keyData, out IItemObtainable instance)) {
									if (!string.IsNullOrWhiteSpace(rawItem)) SectionKeyError(key, $"Regex could not parse [{rawItem}]");
									continue;
								}

								theList.Add(new SpawnChanceWrapper(instance, keyData.Chance, keyData.Copies));
							}

							if (theList.Count != 0) {
								queuedList.Items = theList;
								continue;
							}
						}

						ListNotExistError(key);
						QueuedListList.Remove(queuedList);
						QueuedListDictionary.Remove(key);

						break;

					case Section.SpawnGroups:

						var groupExists = SpawnGroupItemDictionary.TryGetValue(key, out var spawnGroup);

						if (!groupExists) spawnGroup = new SpawnGroupData();

						bool dataAttached = false;

						for (int j = 0; j < dataLength; j++) {
							var rawItem = data[j].Trim();

							if (!ParseKeyGetInstance(rawItem, out KeyData keyData, out IItemObtainable instance)) {
								if (!string.IsNullOrWhiteSpace(rawItem)) SectionKeyError(key, $"Regex could not parse [{rawItem}]");
								continue;
							}

							dataAttached = true;

							var wrappedList = new SpawnChanceWrapper(instance, keyData.Chance, keyData.Copies);

							if (instance.GetType() == typeof(ItemList)) {
								spawnGroup.ItemLists.Add(wrappedList);
							} else if (instance.GetType() == typeof(QueuedList)) {
								spawnGroup.QueuedLists.Add(wrappedList);
							} else spawnGroup.Items.Add(wrappedList);
						}

						if (!groupExists && dataAttached)
							SpawnGroupItemDictionary.Add(key, spawnGroup);
						else if (groupExists)
							SectionKeyError(key, $"Key already exists, merging items...");
						else ListNotExistError(key);

						break;

					case Section.Containers:

						groupExists = ContainerGroupItemDictionary.TryGetValue(key, out spawnGroup);

						if (!groupExists) spawnGroup = new SpawnGroupData();

						dataAttached = false;

						for (int j = 0; j < dataLength; j++) {
							var rawItem = data[j].Trim();

							if (!ParseKeyGetInstance(rawItem, out KeyData keyData, out IItemObtainable instance)) {
								if (!string.IsNullOrWhiteSpace(rawItem)) SectionKeyError(key, $"Regex could not parse [{rawItem}]");
								continue;
							}

							dataAttached = true;

							var containerItem = new ContainerItem(key, instance, keyData.Chance, keyData.Copies);

							if (instance.GetType() == typeof(ItemList)) {
								spawnGroup.ItemLists.Add(containerItem);
							} else if (instance.GetType() == typeof(QueuedList)) {
								spawnGroup.QueuedLists.Add(containerItem);
							} else spawnGroup.Items.Add(containerItem);
						}

						if (!groupExists && dataAttached)
							ContainerGroupItemDictionary.Add(key, spawnGroup);
						else if (groupExists)
							SectionKeyError(key, $"Key already exists, merging items...");
						else ListNotExistError(key);

						break;
				}
			}

			LoadedItemData.Clear();
		}

		private static bool TryParseKey(string key, out KeyData data) {
			var match = KeyRegex.Match(key);

			if (match.Success) {
				var matchedGroups = match.Groups;

				string name = matchedGroups["name"].Value;
				int copies = 1;
				int percent = 100;

				var copyGroup = matchedGroups["copies"];
				var percentGroup = matchedGroups["chance"];

				if (copyGroup.Success) {
					copies = Mathf.Clamp(int.Parse(copyGroup.Value), 1, 20);
				}

				if (percentGroup.Success) {
					percent = Mathf.Clamp(int.Parse(percentGroup.Value), 1, 100);
				}

				data = new KeyData(name, percent, copies);
				return true;
			}

			data = new KeyData();
			return false;
		}

		private static bool ParseKeyGetInstance(string rawItem, out KeyData keyData, out IItemObtainable instance) {
			if (!TryParseKey(rawItem, out keyData)) {
				instance = null;
				return false;
			}

			var item = keyData.Key;

			instance = GetInstance(item);

			return instance != null;
		}


		private static IItemObtainable GetInstance(string key) {
			if (key.Equals("*")) return ItemTypeList[ItemTypeList.Count - 1];

			if (int.TryParse(key, out var result) && result < SavedItemType.ItemTypeLength && result >= 0) {
				return ItemTypeList[result + 1];
			}

			if (ItemListDictionary.TryGetValue(key, out var il)) {
				return il;
			}

			if (QueuedListDictionary.TryGetValue(key, out var ql)) {
				return ql;
			}

			return null;
		}

		private static readonly Regex SectionHeaderRegex = new Regex(@"\[(?<Name>[a-zA-Z\s]*)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly Regex KeyRegex = new Regex(@"^(?<name>[\s\d\w]+)(?:%(?<percent>[\d]+)|#(?<copies>[\d]+))*", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
		private static readonly List<string> LoadedItemData = new List<string>();
		private static readonly Dictionary<string, Section> Sections = new Dictionary<string, Section> {
			{"none", Section.None},
			{"spawn groups", Section.SpawnGroups},
			{"item lists", Section.ItemLists},
			{"queued lists", Section.QueuedLists},
			{"containers", Section.Containers}
		};

		private static Section _lastFoundSection;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckForSection(string line) {
			var match = SectionHeaderRegex.Match(line);
			if (match.Success && Sections.TryGetValue(match.Groups["Name"].Value, out var section)) {
				_lastFoundSection = section;
				return true;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SectionKeyError(string key, string error) => Log.Error($"Section [{_lastFoundSection}] Key [{key}] -- {error}");
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ListNotExistError(string key) => SectionKeyError(key, "List does not exist!");

		internal enum Section {
			None,
			ItemLists,
			QueuedLists,
			SpawnGroups,
			Containers
		}

		internal readonly struct KeyData {
			public readonly int Copies;
			public readonly int Chance;
			public readonly string Key;

			public KeyData(string key, int chance, int copies) {
				Copies = copies;
				Chance = chance;
				Key = key.Trim();
			}
		}

		#endregion
	}
}
