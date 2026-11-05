using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    public enum LevelPrefabStatus
    {
        Found,
        PathMissing,
        PrefabMissing,
        LevelComponentMissing,
    }

    // Что редактор уровней знает о префабе уровня.
    public class LevelPrefabInfo
    {
        public LevelPrefabStatus Status { get; set; }
        public string AssetPath { get; set; }
        public int TowerSlotsCount { get; set; }
        public int MissingTowerSlotsCount { get; set; }
        public int MissingRoadsCount { get; set; }
        public List<LevelTowerSlot> TowerSlots { get; set; } = new();
        public List<LevelRoad> Roads { get; set; } = new();
    }

    // Место под башню: номер — позиция в списке Level.TowersPositions с единицы, пустые элементы тоже занимают номер.
    public class LevelTowerSlot
    {
        public int Number { get; set; }
        public Vector3 Position { get; set; }
    }

    // Дорога префаба. Длины и точки полос есть, только если путь целый: от двух вейпоинтов и без пустых.
    public class LevelRoad
    {
        public PathNumber Number { get; set; }
        public string Name { get; set; }
        public int WaypointsCount { get; set; }
        public int MissingWaypointsCount { get; set; }
        public bool HasValidPath { get; set; }
        public Dictionary<PathLane, float> Lengths { get; set; } = new();
        public Dictionary<PathLane, Vector3[]> LanePoints { get; set; } = new();
    }
}
