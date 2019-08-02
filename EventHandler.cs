using ArithFeather.ArithsToolKit.SpawnPointTools;
using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArithFeather.RandomItemSpawner
{
	public class EventHandler : IEventHandlerWaitingForPlayers, IEventHandlerCallCommand, IEventHandlerDecideTeamRespawnQueue, 
		IEventHandlerRoundStart, IEventHandlerPlayerDie
	{
		private const string ItemSpawnDataFileLocation = "sm_plugins/ATKRandomItemSpawnData.txt";
		private const string ItemRoomDataFileLocation = "sm_plugins/SSRooms.txt";

		private readonly RandomItemSpawner randomItemSpawner;

		private bool debugMode;

		public EventHandler(RandomItemSpawner randomItemSpawner) => this.randomItemSpawner = randomItemSpawner;

		private List<SpawnPoint> itemSpawnData;
		private List<SpawnPoint> ItemSpawnData => itemSpawnData ?? (itemSpawnData = new List<SpawnPoint>());

		private ItemSpawning itemRoomData;
		public ItemSpawning ItemRoomData => itemRoomData ?? (itemRoomData = new ItemSpawning());

		private bool isRoundStarted;

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (randomItemSpawner.DisablePlugin)
			{
				randomItemSpawner.PluginManager.DisablePlugin(randomItemSpawner);
				return;
			}

			debugMode = randomItemSpawner.GetConfigBool("debug_mode");

			isRoundStarted = false;

			// Load right after getting rooms to set their extra data
			RoomDataIO.LoadItemRoomData(ItemRoomData, ItemRoomDataFileLocation);

			var itemPointCount = ItemSpawnData.Count;
			var rooms = CustomRoomManager.Instance.Rooms;
			var roomCount = rooms.Count;

			// Create item spawn points on map
			for (var j = 0; j < itemPointCount; j++)
			{
				var p = ItemSpawnData[j];

				for (var i = 0; i < roomCount; i++)
				{
					var r = ItemRoomData.Rooms[i];

					if (p.RoomType != r.Room.Name) continue;

					r.ItemSpawnPoints.Add(new ItemSpawnPoint(p.RoomType, p.ZoneType,
						Tools.Vec3ToVec(r.Room.Transform.TransformPoint(Tools.VecToVec3(p.Position))) + new Vector(0, 0.1f, 0),
						Tools.Vec3ToVec(r.Room.Transform.TransformDirection(Tools.VecToVec3(p.Rotation)))));
				}
			}
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

		public void OnRoundStart(RoundStartEvent ev)
		{
			if (!randomItemSpawner.UseDefaultEvents) return;

			isRoundStarted = true;
			ItemRoomData.FreeRooms.AddRange(ItemRoomData.Rooms);
		}

		public void OnDecideTeamRespawnQueue(DecideRespawnQueueEvent ev)
		{
			if (!randomItemSpawner.UseDefaultEvents) return;

			ItemRoomData.LevelLoaded();
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			if (!randomItemSpawner.UseDefaultEvents || ev.Killer.TeamRole.Team == Smod2.API.Team.NONE) return;

			ItemRoomData.CheckSpawns();
			ItemRoomData.PlayerDead();
		}
	}
}
