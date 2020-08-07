using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using Exiled.API.Features;
using UnityEngine;
using SpawnPoint = ArithFeather.AriToolKit.PointEditor.Point;

namespace ArithFeather.CustomItemSpawner.ItemData {
	internal static class SpawnPointCreator {
		private const string PositionDataFileName = "ItemSpawnPoints";

		private static readonly string SavedItemFilePath = Path.Combine(Paths.Configs, "ItemSpawnInfo.txt");
		private static readonly string SavedSpawnPointPath = Path.Combine(PointIO.FolderPath, PositionDataFileName) + ".txt";

		private static readonly Vector3 ItemSpawnOffset = new Vector3(0, 0.05f, 0);

		private static PointList _pointList;

		// Used for recursive deserialization
		private static readonly List<SavedItemType> ItemTypeList = new List<SavedItemType>();
		private static readonly Dictionary<string, QueuedList> QueuedListDictionary = new Dictionary<string, QueuedList>();
		private static readonly Dictionary<string, ItemList> ItemListDictionary = new Dictionary<string, ItemList>();
		private static readonly Dictionary<string, RoomItemData> RoomItemDictionary = new Dictionary<string, RoomItemData>();
		// Used for randomizing queue lists
		private static readonly List<QueuedList> QueuedListList = new List<QueuedList>();

		/// <summary>
		/// Populated every new game.
		/// </summary>
		public static readonly List<ItemRoom> ItemRooms = new List<ItemRoom>();

		public static void Reload() {
			LoadItemData();

			if (PointAPI.PointLists.TryGetValue(PositionDataFileName, out var pointList)) {
				_pointList = pointList;
			} else if (File.Exists(SavedSpawnPointPath)) {
				Log.Error($"Could not Load ItemSpawnInfo.txt");
			}
		}

		public static void OnLoadSpawnPoints(int seed) {
			if (!File.Exists(SavedSpawnPointPath)) CreateDefaultSpawnPointsFile();

			if (_pointList == null || _pointList.RoomGroupedFixedPoints.Count == 0) {
				Log.Error("Could not load spawn points.");
				return;
			}

			UnityEngine.Random.InitState(seed);
			ItemRooms.Clear();

			var rooms = Rooms.CustomRooms;
			var roomCount = rooms.Count;
			var spawnPointsList = _pointList.RoomGroupedFixedPoints;

			for (var i = 0; i < roomCount; i++) {

				var spawnPoints = spawnPointsList[i];
				var customRoom = rooms[i];
				var roomName = customRoom.FixedName;

				if (spawnPoints != null && spawnPoints.Count != 0 &&
					RoomItemDictionary.TryGetValue(roomName, out var itemRoom)) {

					var list = new List<IItemObtainable>(itemRoom.Items.Count + itemRoom.QueuedLists.Count +
														 itemRoom.ItemLists.Count);

					// Shuffle the lists before adding them
					itemRoom.Items.UnityShuffle();
					itemRoom.QueuedLists.UnityShuffle();
					itemRoom.ItemLists.UnityShuffle();

					list.AddRange(itemRoom.Items);
					list.AddRange(itemRoom.QueuedLists);
					list.AddRange(itemRoom.ItemLists);

					var ric = new ItemRoom(customRoom, list);

					ric.Points = ConvertFixedPointToItemPoint(ric, spawnPoints);
					ric.MaxItemsAllowed = Mathf.Clamp(itemRoom.MaxItemsAllowed, 0, ric.Points.Count);

					ItemRooms.Add(ric);
				} else {
					ItemRooms.Add(new ItemRoom());
				}
			}
		}

		private static List<ItemSpawnPoint> ConvertFixedPointToItemPoint(ItemRoom ric, List<FixedPoint> pointList) {
			var newList = new List<ItemSpawnPoint>();

			var pointCount = pointList.Count;
			for (int i = 0; i < pointCount; i++) {
				newList.Add(new ItemSpawnPoint(ric, pointList[i]));
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

			var spawnPoints = new List<SpawnPoint>(positionDataLength);

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

				spawnPoints.Add(new SpawnPoint(room.FixedName, room.Room.Zone, localItemPosition,
					localItemRotation));
			}

			_pointList = new PointList(spawnPoints);

			PointAPI.PointLists.Add(PositionDataFileName, _pointList);
			PointIO.Save(_pointList, SavedSpawnPointPath);

			_pointList.FixData();
		}


		private static readonly string ItemSpawnTypesDescription =
		"# How to use this.\n" +
		"\n" +
		"# When the server spawns items, it will...\n" +
		"# First: Spawn all items assigned to spawn in a room.\n" +
		"# Example: This will force the HID room to spawn the MicroHID.\n" +
		"# HCZ_Hid:1:16\n" +
		"\n" +
		"# Second: Spawn the \"Queued Lists\" randomly across random rooms until the queue is empty.\n" +
		"# Example, this will make sure at least 2 checkpoint keycard will spawn somewhere in LCZ.\n" +
		"# Queued Lists:\n" +
		"# SpawnLCZ:3,3\n" +
		"# Rooms:\n" +
		"# LCZ_Toilets:SpawnLCZ\n" +
		"# LCZ_372:SpawnLCZ\n" +
		"# LCZ_Cafe:SpawnLCZ\n" +
		"# LCZ_Plants:SpawnLCZ\n" +
		"# LCZ_173:SpawnLCZ\n" +
		"\n" +
		"# Third: Any \"Item Lists\" you attached to the rooms will spawn a random item from that list.\n" +
		"# Example: You can use this for rarities.\n" +
		"# Item Lists:\n" +
		"# HighRarity:21,25\n" +
		"# LowRarity:12,14,15\n" +
		"# Rooms:\n" +
		"# LCZ_Armory:LowRarity,LowRarity,LowRarity,HighRarity,HighRarity\n" +
		"# (This will spawn 3 random items from the LowRarity list and 2 items from the HighRarity list in Light Containment Armory.\n" +
		"\n" +
		"# -Again, the difference between a Queued List and Item List is: A Queued list will spawn all the items inside it, across all the rooms it is attached to. Where an Item List will only spawn 1 random item inside the list.\n" +
		"# -You can add an Item List to a Queued List, but you can't add a Queued List to an Item List, or an Item List to an Item List.\n\n";

		private static void CreateItemRoomFile() {
			Log.Warn("Creating new ItemSpawnInfo file");

			using (var writer = new StreamWriter(File.Create(SavedItemFilePath))) {

				// Description
				writer.Write(ItemSpawnTypesDescription);

				// Display Items
				writer.WriteLine("# Items:\n");
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
				writer.WriteLine();

				// Display Queued Lists

				writer.WriteLine("[Queued Lists]");
				writer.WriteLine();
				writer.WriteLine("LCZQueue:3,2,3,13,Garbage,Garbage,Uncommon,Common");
				writer.WriteLine("EntranceQueue:*,*,*,VeryRare,Common,Uncommon,Uncommon,Garbage,Garbage,Garbage");
				writer.WriteLine();


				// Display Rooms
				writer.WriteLine("[Rooms]");
				writer.WriteLine("# RoomName : Max items allowed at a time : Item Data (1,6,Rare,Uncommon)\n");

				// Lazy, used an escape because unique rooms aren't guaranteed.
				writer.Write(
					"#Light Containment\nLCZ_ClassDSpawn:\nLCZ_Curve:\nLCZ_Toilets:3:LCZQueue\nLCZ_ChkpB:\nLCZ_ChkpA:\nLCZ_TCross:\nLCZ_Crossing:\nLCZ_372:1:LCZQueue\nLCZ_Straight:\nLCZ_Cafe:4:LCZQueue\nLCZ_Plants:1:LCZQueue\nLCZ_Armory:5:LCZQueue,Common,Common,Rare,Uncommon,*\nLCZ_Airlock:\nLCZ_173:1:LCZQueue\nLCZ_914:\nLCZ_012:2:LCZQueue\n\n#Heavy Containment\nHCZ_106:\nHCZ_Hid:1:16\nHCZ_EZ_Checkpoint:\nHCZ_Crossing:\nHCZ_457:2:VeryRare,Common,*\nHCZ_Tesla:\nHCZ_049:2:Uncommon,Rare,*,*\nHCZ_Room3ar:5:Common,Common,Uncommon,Rare,*\nHCZ_ChkpA:\nHCZ_Curve:\nHCZ_Room3:\nHCZ_Nuke:1:Uncommon\nHCZ_Testroom:\nHCZ_Servers:1:Common,*\nHCZ_079:\nHCZ_ChkpB:\nHCZ_Tesla:\n\n#Entrance\nEZ_Endoof:\nEZ_Crossing:\nEZ_GateB:\nEZ_Smallrooms2:2:EntranceQueue\nEZ_Cafeteria:2:EntranceQueue\nEZ_PCs_small:2:EntranceQueue\nEZ_Curve:\nEZ_Intercom:\nEZ_Straight:\nEZ_upstairs:\nEZ_PCs:2:EntranceQueue\nEZ_GateA:\nEZ_Shelter:\n\n#Surface\nRoot_*&*Outside Cams:");
			}
		}

		#endregion

		#region Loading the ItemRoomFile

		internal enum Section {
			None,
			ItemList,
			QueuedList,
			Rooms
		}

		private static readonly Dictionary<string, Section> Sections = new Dictionary<string, Section> {
			{"none", Section.None},
			{"rooms", Section.Rooms},
			{"item lists", Section.ItemList},
			{"queued lists", Section.QueuedList},
		};

		private static readonly List<string> LoadedItemData = new List<string>();
		private static Section _lastFoundSection;

		private static readonly Regex SectionHeader = new Regex(@"\[(?<Name>[a-zA-Z\s]*)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private static void LoadItemData() {
			if (!FileManager.FileExists(SavedItemFilePath))
				CreateItemRoomFile();

			LoadedItemData.Clear();
			ItemTypeList.Clear();
			QueuedListDictionary.Clear();
			ItemListDictionary.Clear();
			RoomItemDictionary.Clear();
			QueuedListList.Clear();

			using (var reader = File.OpenText(SavedItemFilePath)) {

				// Create SavedItemType instances for dictionary
				ItemTypeList.Add(new SavedItemType()); // Wildcard
				for (int i = 0; i < SavedItemType.ItemTypeLength; i++)
				{
					ItemTypeList.Add(i == 36 ? new SavedItemType(ItemType.None) : new SavedItemType((ItemType) i));
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

					if (_lastFoundSection == Section.ItemList) {
						ItemListDictionary.Add(key, new ItemList());
					} else if (_lastFoundSection == Section.QueuedList) {
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
		private static void SectionKeyError(string key, string error) => Log.Error($"Section \"{_lastFoundSection}\" Key \"{key}\"  {error}");
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CantLoadData(string key) => SectionKeyError(key, "Could not load data.");
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

				switch (_lastFoundSection) {

					case Section.ItemList:

						if (sDataLength > 2) {
							TooManySplitters(key);
							continue;
						}

						var data = sData[1].Split(',');
						var dataLength = data.Length;

						if (ItemListDictionary.TryGetValue(key, out var itemList)) {
							if (dataLength > 0) {
								var theList = new List<SavedItemType>(dataLength);

								for (int k = 0; k < dataLength; k++) {
									var item = data[k].Trim();

									var instance = GetInstance(item);

									if (instance == null) continue;

									if (instance.GetType() != typeof(SavedItemType)) {
										SectionKeyError(key, $"Failed to add {item}. You can only add Items to Item Lists");
									} else {
										theList.Add((SavedItemType)instance);
									}
								}

								if (theList.Count != 0) {
									itemList.Items = theList;
									continue;
								}
							}
						}

						CantLoadData(key);
						ItemListDictionary.Remove(key);

						break;

					case Section.QueuedList:

						if (sDataLength > 2) {
							TooManySplitters(key);
							continue;
						}

						data = sData[1].Split(',');
						dataLength = data.Length;

						if (QueuedListDictionary.TryGetValue(key, out var queuedList)) {
							if (dataLength > 0) {

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
						}

						CantLoadData(key);
						QueuedListList.Remove(queuedList);
						QueuedListDictionary.Remove(key);

						break;

					case Section.Rooms:

						if (sDataLength > 3) {
							TooManySplitters(key);
							continue;
						}

						// Parse Max Room
						var maxItems = sData[1].Trim();
						var room = new RoomItemData();

						if (int.TryParse(maxItems, out var result))
							room.MaxItemsAllowed = result;
						else {
							if (!string.IsNullOrWhiteSpace(maxItems))
								SectionKeyError(key, $"Invalid data: \"{maxItems}\"");
							continue;
						}

						// Parse Data
						bool dataAttached = false;

						var itemData = sData[2].Split(',');
						var itemDataLength = itemData.Length;

						for (int j = 0; j < itemDataLength; j++) {
							var item = itemData[j].Trim();

							var instance = GetInstance(item);

							if (instance == null) continue;

							dataAttached = true;

							if (instance.GetType() == typeof(ItemList)) {
								room.ItemLists.Add(instance);
							} else if (instance.GetType() == typeof(QueuedList)) {
								room.QueuedLists.Add(instance);
							} else room.Items.Add(instance);
						}

						if (dataAttached)
							RoomItemDictionary.Add(key, room);
						else {
							CantLoadData(key);
						}

						break;
				}
			}

			Log.Info($"Found {RoomItemDictionary.Count} rooms with items to spawn.");

			LoadedItemData.Clear();
		}

		private static IItemObtainable GetInstance(string key) {
			if (key.Equals("*")) return ItemTypeList[ItemTypeList.Count - 1];

			if (int.TryParse(key, out var result) && result < ItemTypeList.Count && result >= 0) {
				return ItemTypeList[result];
			}

			if (ItemListDictionary.TryGetValue(key, out var il)) {
				return il;
			}

			if (QueuedListDictionary.TryGetValue(key, out var ql)) {
				return ql;
			}

			Log.Error($"Section \"{_lastFoundSection}\"  Could not create list for {key}.");
			return null;
		}

		#endregion
	}
}