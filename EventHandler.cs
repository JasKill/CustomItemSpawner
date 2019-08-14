using ArithFeather.ArithSpawningKit.SpawnPointTools;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ArithFeather.RandomItemSpawner
{
	public class EventHandler : IEventHandlerWaitingForPlayers, IEventHandlerCallCommand, 
		IEventHandlerDecideTeamRespawnQueue, IEventHandlerPlayerDie
	{
		private const string ItemSpawnDataFileLocation = "sm_plugins/ASKRandomItemSpawnData.txt";
		private const string ItemRoomDataFileLocation = "sm_plugins/ASKItemSpawnRoomData.txt";

		private readonly RandomItemSpawner randomItemSpawner;

		private bool debugMode;

		public EventHandler(RandomItemSpawner randomItemSpawner) => this.randomItemSpawner = randomItemSpawner;

		private List<SpawnPoint> itemSpawnData;
		private List<SpawnPoint> ItemSpawnData => itemSpawnData ?? (itemSpawnData = new List<SpawnPoint>());

		private ItemSpawning itemRoomData;
		public ItemSpawning ItemRoomData => itemRoomData ?? (itemRoomData = new ItemSpawning());

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (randomItemSpawner.disablePlugin)
			{
				randomItemSpawner.PluginManager.DisablePlugin(randomItemSpawner);
				return;
			}

			debugMode = randomItemSpawner.ConfigManager.Config.GetBoolValue("sm_debug", false);

			// Load the room data with the Room Manager API rooms.
			RoomDataIO.LoadItemRoomData(ItemRoomData, ItemRoomDataFileLocation);
			// Get all the spawn points.
			LoadItemData();

			var itemPointCount = ItemSpawnData.Count;
			var rooms = ItemRoomData.Rooms;
			var roomCount = rooms.Count;
			var counter = 0;

			// Make sure every room gets a spawn point assigned to that room's type.
			for (var j = 0; j < itemPointCount; j++)
			{
				var p = ItemSpawnData[j];

				for (var i = 0; i < roomCount; i++)
				{
					var r = rooms[i];

					if (p.ZoneType != r.Room.Zone || p.RoomType != r.Room.Name) continue;

					counter++;
					r.IsFree = true;
					r.ItemSpawnPoints.Add(new ItemSpawnPoint(p.RoomType, p.ZoneType,
						Tools.Vec3ToVec(r.Room.Transform.TransformPoint(Tools.VecToVec3(p.Position))) + new Vector(0, 0.1f, 0),
						Tools.Vec3ToVec(r.Room.Transform.TransformDirection(Tools.VecToVec3(p.Rotation)))));
				}
			}

			if (counter == 0)
			{
				randomItemSpawner.Warn("There are no item spawn points set.");
				return;
			}

			// Shuffle spawns and rooms
			// Remove the rooms that have no spawns in them
			var freeRooms = ItemRoomData.FreeRooms;
			for (int i = roomCount - 1; i >= 0; i--)
			{
				var room = rooms[i];

				if (!room.IsFree)
				{
					rooms.RemoveAt(i);
					freeRooms.RemoveAt(i);
				}
				else
				{
					room.ItemSpawnPoints.Shuffle();
				}
			}

			ItemRoomData.FreeRooms.Shuffle();
		}

		#region Point Editing

		public void LoadItemData() => itemSpawnData = SpawnDataIO.Open(ItemSpawnDataFileLocation);

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			if (debugMode)
			{
				switch (ev.Command.ToUpper())
				{
					case "I ADD":
						var player = ev.Player;
						var scp049Component = ((GameObject)player.GetGameObject()).GetComponent<Scp049PlayerScript>();
						var scp106Component = (player.GetGameObject() as GameObject)?.GetComponent<Scp106PlayerScript>();
						var cameraRotation = scp049Component.plyCam.transform.forward;

						Physics.Raycast(scp049Component.plyCam.transform.position, cameraRotation, out RaycastHit where,
							40f, scp106Component.teleportPlacementMask);

						Vector rotation = new Vector(-cameraRotation.x, cameraRotation.y, -cameraRotation.z);
						var v3position = where.point + (Vector3.up * 0.1f);
						var position = Tools.Vec3ToVec(v3position);

						var closestRoom = FindClosestRoomToPoint(v3position);
						var roomName = closestRoom.Name;

						ItemSpawnData.Add(new SpawnPoint(roomName, closestRoom.Zone, Tools.Vec3ToVec(closestRoom.Transform.InverseTransformPoint(v3position)),
							Tools.Vec3ToVec(closestRoom.Transform.InverseTransformDirection(Tools.VecToVec3(rotation)))));
						ev.ReturnMessage = $"Created Item spawn point in {roomName}  ({closestRoom.Zone.ToString()})";
						break;

					case "I SAVE":
						SpawnDataIO.Save(ItemSpawnData, ItemSpawnDataFileLocation);
						ev.ReturnMessage = "Saved item spawn points";
						break;

					case "I LOAD":
						LoadItemData();
						ev.ReturnMessage = $"Loaded {ItemSpawnData.Count} item spawn points";
						break;
				}
			}
		}

		/// <summary>
		/// Shared between player and item spawn editing.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private CustomRoom FindClosestRoomToPoint(Vector3 point)
		{
			// Find closest room
			CustomRoom closestRoom = null;
			float distanceToClosest = 10000;

			var rooms = CustomRoomManager.Instance.Rooms;
			var roomLength = rooms.Count;

			for (int i = 0; i < roomLength; i++)
			{
				var r = rooms[i];
				var distance = Vector3.Distance(point, r.Transform.position);

				if (distance < distanceToClosest)
				{
					closestRoom = r;
					distanceToClosest = distance;
				}
			}

			return closestRoom;
		}

		#endregion

		public void OnDecideTeamRespawnQueue(DecideRespawnQueueEvent ev)
		{
			if (!randomItemSpawner.UseDefaultEvents) return;

			ItemRoomData.RoundStart();
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (!randomItemSpawner.UseDefaultEvents || ev.Killer.TeamRole.Team == Smod2.API.Team.NONE) return;

			ItemRoomData.CheckSpawns();
			ItemRoomData.PlayerDead();
		}
	}
}
