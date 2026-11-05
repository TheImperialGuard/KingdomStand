using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEditor;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Читает префаб уровня так же, как игра: Resources.Load по LevelConfig.PrefabPath.
    public static class LevelPrefabReader
    {
        public const int MIN_WAYPOINTS_COUNT = 2;

        // Места под башни нумеруются с единицы — так о них удобнее говорить.
        public const int FIRST_TOWER_SLOT_NUMBER = 1;

        public static LevelPrefabInfo Read(LevelConfig config)
        {
            LevelPrefabInfo info = new();

            if (string.IsNullOrWhiteSpace(config.PrefabPath))
            {
                info.Status = LevelPrefabStatus.PathMissing;
                return info;
            }

            GameObject prefab = Resources.Load<GameObject>(config.PrefabPath);

            if (prefab == null)
            {
                info.Status = LevelPrefabStatus.PrefabMissing;
                return info;
            }

            info.AssetPath = AssetDatabase.GetAssetPath(prefab);

            if (prefab.TryGetComponent(out Level level) == false)
            {
                info.Status = LevelPrefabStatus.LevelComponentMissing;
                return info;
            }

            info.Status = LevelPrefabStatus.Found;

            ReadTowerSlots(level, info);

            if (level.RoadPaths == null)
                return info;

            foreach (RoadPath roadPath in level.RoadPaths)
            {
                if (roadPath == null)
                {
                    info.MissingRoadsCount++;
                    continue;
                }

                info.Roads.Add(ReadRoad(roadPath));
            }

            return info;
        }

        private static void ReadTowerSlots(Level level, LevelPrefabInfo info)
        {
            if (level.TowersPositions == null)
                return;

            for (int i = 0; i < level.TowersPositions.Count; i++)
            {
                Transform towerPosition = level.TowersPositions[i];

                if (towerPosition == null)
                {
                    info.MissingTowerSlotsCount++;
                    continue;
                }

                info.TowerSlotsCount++;
                info.TowerSlots.Add(new LevelTowerSlot
                {
                    Number = i + FIRST_TOWER_SLOT_NUMBER,
                    Position = towerPosition.position,
                });
            }
        }

        private static LevelRoad ReadRoad(RoadPath roadPath)
        {
            LevelRoad road = new()
            {
                Number = roadPath.Number,
                Name = roadPath.name,
            };

            IReadOnlyList<Waypoint> waypoints = roadPath.Waypoints;

            if (waypoints == null)
                return road;

            road.WaypointsCount = waypoints.Count;

            foreach (Waypoint waypoint in waypoints)
            {
                if (waypoint == null)
                    road.MissingWaypointsCount++;
            }

            road.HasValidPath = RoadLanes.IsComplete(waypoints);

            if (road.HasValidPath == false)
                return road;

            foreach (PathLane lane in Enum.GetValues(typeof(PathLane)))
            {
                road.Lengths[lane] = RoadLanes.CalculateLength(waypoints, lane);
                road.LanePoints[lane] = RoadLanes.GetPoints(waypoints, lane);
            }

            return road;
        }
    }
}
