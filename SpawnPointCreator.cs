using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using ArithFeather.CustomItemSpawner.ItemListTypes;
using ArithFeather.CustomItemSpawner.Spawning;
using Exiled.API.Features;
using SpawnPoint = ArithFeather.AriToolKit.PointEditor.Point;

namespace ArithFeather.CustomItemSpawner {
	internal static class SpawnPointCreator {
		private const string PointDataFileName = "ItemSpawnPoints";
		private const string ItemDataFileName = "ItemSpawnInfo";

		private static readonly string ItemDataFilePath = Path.Combine(Paths.Configs, ItemDataFileName) + ".txt";
		private static readonly string PointDataFilePath = Path.Combine(PointIO.FolderPath, PointDataFileName) + ".txt";

		private static PointList _pointList;

		// Used for recursive deserialization
		private static readonly List<SavedItemType> ItemTypeList = new List<SavedItemType>();
		private static readonly Dictionary<string, QueuedList> QueuedListDictionary = new Dictionary<string, QueuedList>();
		private static readonly Dictionary<string, ItemList> ItemListDictionary = new Dictionary<string, ItemList>();
		private static readonly Dictionary<string, SpawnGroupData> spawnGroupItemDictionary = new Dictionary<string, SpawnGroupData>();

		// Used for randomizing queue lists
		public static readonly List<QueuedList> QueuedListList = new List<QueuedList>();

		// Populated every new game.
		public static readonly List<SpawnGroup> SpawnGroups = new List<SpawnGroup>();
		public static readonly Dictionary<string, SpawnGroup> SpawnGroupDictionary = new Dictionary<string, SpawnGroup>();

		public static void Reload() {
			LoadItemData();
			_pointList = PointAPI.GetPointList(PointDataFileName);
		}

		public static void OnLoadSpawnPoints(int seed) {
			if (!File.Exists(PointDataFilePath)) {
				CreateDefaultSpawnPointsFile();
			}

			if (!File.Exists(ItemDataFilePath)) {
				CreateItemDataFile();
			}

			var spawnPointDictionary = _pointList.IdGroupedFixedPoints;
			var spawnPointDictionaryCount = spawnPointDictionary.Count;

			var itemGroupCount = spawnGroupItemDictionary.Count;
			if (spawnPointDictionaryCount == 0 || itemGroupCount == 0) {
				Log.Error($"Could not make Spawn Groups. (Spawn Point Groups: {spawnPointDictionaryCount} | Item Groups: {itemGroupCount})");
				return;
			}

			UnityEngine.Random.InitState(seed);

			SpawnGroups.Clear();
			SpawnGroupDictionary.Clear();

			// Group up the positions and item spawns via ID's
			foreach (var pair in spawnPointDictionary) {
				var key = pair.Key;
				var spawnPoints = pair.Value;

				if (spawnGroupItemDictionary.TryGetValue(key, out var groupData)) {

					var itemList = new List<IItemObtainable>(groupData.Items.Count + groupData.QueuedLists.Count +
														 groupData.ItemLists.Count);

					// Shuffle the lists before adding them
					groupData.Items.UnityShuffle();
					groupData.QueuedLists.UnityShuffle();
					groupData.ItemLists.UnityShuffle();

					itemList.AddRange(groupData.Items);
					itemList.AddRange(groupData.QueuedLists);
					itemList.AddRange(groupData.ItemLists);

					var spawnGroup = new SpawnGroup();

					// Convert SpawnPoint class to ItemSpawnPoint class.
					var newSpawnPointList = new List<ItemSpawnPoint>();
					var pointCount = spawnPoints.Count;
					for (int i = 0; i < pointCount; i++) {
						newSpawnPointList.Add(new ItemSpawnPoint(spawnGroup, spawnPoints[i]));
					}

					spawnGroup.Initialize(key, itemList, newSpawnPointList);
					SpawnGroupDictionary.Add(key, spawnGroup);
					SpawnGroups.Add(spawnGroup);
				}
			}

			Log.Info($"Found {SpawnGroups.Count} group(s) with items to spawn.");

			// Shuffle Groups
			SpawnGroups.UnityShuffle();

			// Shuffle Queue Data
			var queueCount = QueuedListList.Count;
			for (int i = 0; i < queueCount; i++) {
				QueuedListList[i].Reset();
			}

			SavedItemRoom.CreateGlobalRooms();
		}

		private static List<ItemSpawnPoint> ConvertFixedPointToItemPoint(SpawnGroup spawnGroup, List<FixedPoint> pointList) {
			var newList = new List<ItemSpawnPoint>();

			var pointCount = pointList.Count;
			for (int i = 0; i < pointCount; i++) {
				newList.Add(new ItemSpawnPoint(spawnGroup, pointList[i]));
			}

			return newList;
		}

		#region Creating default text files

		private static void CreateDefaultSpawnPointsFile() {
			Log.Warn("Creating new Spawn Point file using default spawn points.");
			var ris = RandomItemSpawner.singleton;

			// Save Position data
			var positionData = ris.posIds;
			var positionDataLength = positionData.Length;

			var spawnPoints = _pointList.RawPoints;

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

				spawnPoints.Add(new SpawnPoint(dat.posID.ToLowerInvariant(), room.FixedName, room.Room.Zone, localItemPosition,
					localItemRotation));
			}

			PointIO.Save(_pointList, PointDataFilePath);
			_pointList.FixData();
		}


		private const string ItemSpawnTypesDescription =
		"# How to use this.\n" +
		"\n" +
		"# Note: A 'Spawn Group' is a group of Spawn Points." +
		"# When the server spawns items, it will...\n" +
		"# Go through all the 'Spawn Groups' and tell each group to spawn an item until ALL groups have spawned their items OR they are at Max Items Allowed" +
		"\n" +
		"# Each 'Spawn group' will spawn its items in order:" +
		"\n" +
		"# First: Spawn all items assigned to spawn. (* and 0 to 36)\n" +
		"# Example: This will force the HID room to spawn the MicroHID.\n" +
		"# [Spawn Groups]" +
		"# Hid:1:16\n" +
		"\n" +
		"# Second: Spawn an item from a 'Queued List' (Until the Queued list is empty).\n" +
		"# Example, this will make sure at least 2 checkpoint key cards will spawn somewhere in LCZ.\n" +
		"# [Queued Lists]\n" +
		"# SpawnLCZ:3,3\n" +
		"# [Spawn Groups]\n" +
		"# LCZ_Toilets:1:SpawnLCZ\n" +
		"# LCZ_372:1:SpawnLCZ\n" +
		"# LCZ_Cafe:1:SpawnLCZ\n" +
		"# LCZ_173:1:SpawnLCZ\n" +
		"\n" +
		"# Third: Any 'Item Lists' you attached to the 'Spawn Group' will spawn a random item from that list.\n" +
		"# Example: You can use this for rarities.\n" +
		"# [Item Lists]\n" +
		"# HighRarity:21,25\n" +
		"# LowRarity:12,14,15\n" +
		"# [Spawn Groups]\n" +
		"# LCZ_Armory:5:LowRarity,LowRarity,LowRarity,HighRarity,HighRarity\n" +
		"# (This will spawn 3 random items from the LowRarity list and 2 items from the HighRarity list in Light Containment Armory.\n" +
		"\n" +
		"# -Again, the difference between a Queued List and Item List is: A Queued list will spawn all the items inside it, across all the groups it is attached to. Where an Item List will only spawn 1 random item inside the list.\n" +
		"# -You can add an Item List to a Queued List, but you can't add a Queued List to an Item List, or an Item List to an Item List.\n" +
		"# -For spawn points inside duplicate rooms (like Plant Room), the items will be split across those rooms.\n\n";

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
				writer.WriteLine();
				writer.WriteLine("Garbage:0,1,15,19,22,26,28,29,35");
				writer.WriteLine("Common:2,3,4,12,14,23,25,33,34");
				writer.WriteLine("Uncommon:5,6,17,18,24,30,31,32");
				writer.WriteLine("Rare:7,8,9,13,20,21");
				writer.WriteLine("VeryRare:10,11,16");
				writer.WriteLine("10%ChanceToSpawnHID:36,36,36,36,36,36,36,36,36,16");
				writer.WriteLine();

				// Display Queued Lists

				writer.WriteLine("[Queued Lists]");
				writer.WriteLine();
				writer.WriteLine("SpawnOneOfEachItem:3,2,3,13,Garbage,Garbage,Uncommon,Common");
				writer.WriteLine();

				// Display Spawn Groups

				writer.WriteLine("[Spawn Groups]");
				writer.WriteLine("# Group Name : Item Data (1,6,Rare,Uncommon)");
				writer.WriteLine();

				// Create default Groups
				var defaultItems = RandomItemSpawner.singleton.pickups;
				var defaultItemsSize = defaultItems.Length;

				var itemTypes = Enum.GetNames(typeof(ItemType));

				var keyCounter = new Dictionary<string, StringBuilder>();

				for (int i = 0; i < defaultItemsSize; i++) {
					var item = defaultItems[i];

					var key = item.posID;

					string itemID = string.Empty;

					// Find item ID
					for (int j = 0; j < SavedItemType.ItemTypeLength; j++) {
						if (itemTypes[j].Equals(item.itemID.ToString(), StringComparison.InvariantCultureIgnoreCase))
							itemID = j.ToString();
					}

					if (!string.IsNullOrWhiteSpace(itemID) && keyCounter.TryGetValue(key, out var value)) {
						value.Append($",{itemID}");
					} else {
						keyCounter.Add(key, new StringBuilder($"{key}:{itemID}"));
					}
				}

				foreach (var stringPair in keyCounter) {
					writer.WriteLine(stringPair.Value.ToString());
				}
			}

			LoadItemData();
		}

		#endregion

		#region Loading the ItemDataFile

		internal enum Section {
			None,
			ItemLists,
			QueuedLists,
			SpawnGroups
		}

		private static readonly Dictionary<string, Section> Sections = new Dictionary<string, Section> {
			{"none", Section.None},
			{"spawn groups", Section.SpawnGroups},
			{"item lists", Section.ItemLists},
			{"queued lists", Section.QueuedLists},
		};

		private static readonly List<string> LoadedItemData = new List<string>();
		private static Section _lastFoundSection;

		private static readonly Regex SectionHeader = new Regex(@"\[(?<Name>[a-zA-Z\s]*)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private static void LoadItemData() {
			if (!FileManager.FileExists(ItemDataFilePath)) return;

			LoadedItemData.Clear();
			ItemTypeList.Clear();
			QueuedListDictionary.Clear();
			ItemListDictionary.Clear();
			spawnGroupItemDictionary.Clear();
			QueuedListList.Clear();

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckForSection(string line) {
			var match = SectionHeader.Match(line);
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
		private static void TooManySplitters(string key) => SectionKeyError(key, "Too many ':' splitters.");

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
					TooManySplitters(key);
					continue;
				}

				var data = sData[1].Split(',');
				var dataLength = data.Length;

				if (dataLength == 0) continue;

				switch (_lastFoundSection) {

					case Section.ItemLists:

						if (ItemListDictionary.TryGetValue(key, out var itemList)) {

							var theList = new List<SavedItemType>(dataLength);

							for (int k = 0; k < dataLength; k++) {
								var item = data[k].Trim();

								var instance = GetInstance(item);

								if (instance == null) continue;

								if (instance.GetType() != typeof(SavedItemType)) {
									SectionKeyError(key,
										$"Failed to add {item}. You can only add Items to Item Lists");
								} else {
									theList.Add((SavedItemType)instance);
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
								var item = data[k].Trim();

								var instance = GetInstance(item);

								if (instance == null) continue;

								theList.Add(instance);
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

						var groupExists = spawnGroupItemDictionary.TryGetValue(key, out var spawnGroup);

						if (!groupExists) spawnGroup = new SpawnGroupData();

						bool dataAttached = false;

						for (int j = 0; j < dataLength; j++) {
							var item = data[j].Trim();

							var instance = GetInstance(item);

							if (instance == null) continue;

							dataAttached = true;

							if (instance.GetType() == typeof(ItemList)) {
								spawnGroup.ItemLists.Add(instance);
							} else if (instance.GetType() == typeof(QueuedList)) {
								spawnGroup.QueuedLists.Add(instance);
							} else spawnGroup.Items.Add(instance);
						}

						if (!groupExists && dataAttached)
							spawnGroupItemDictionary.Add(key, spawnGroup);
						else if (groupExists)
							SectionKeyError(key, $"Key already exists, merging items...");
						else ListNotExistError(key);

						break;
				}
			}

			LoadedItemData.Clear();
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

			//SectionKeyError(key, "Could not find instance of key.");
			return null;
		}

		#endregion
	}
}