using System;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor.Roads
{
    // Дороги и места под башни того, что сейчас открыто в Scene View: префаба уровня в Prefab Mode или сцен в Play Mode.
    // Списки пересобираются после изменений иерархии, а не на каждой перерисовке.
    [InitializeOnLoad]
    public static class StageRoads
    {
        private static readonly List<RoadPath> _roads = new();

        private static Level _level;
        private static bool _isOutdated = true;

        static StageRoads()
        {
            EditorApplication.hierarchyChanged += MarkOutdated;
            EditorApplication.playModeStateChanged += _ => MarkOutdated();
            PrefabStage.prefabStageOpened += _ => MarkOutdated();
            PrefabStage.prefabStageClosing += _ => MarkOutdated();
        }

        // Дороги по порядку номеров. Уничтоженная дорога — повод собрать список заново, даже без события.
        public static IReadOnlyList<RoadPath> Get()
        {
            if (_isOutdated || HasDestroyedRoads())
                Refresh();

            return _roads;
        }

        // Места под башни первого найденного Level — в том порядке, по которому они нумеруются; пустые элементы
        // списка остаются на своих местах.
        public static IReadOnlyList<Transform> GetTowerSlots()
        {
            if (_isOutdated || HasDestroyedRoads() || IsLevelDestroyed())
                Refresh();

            if (_level == null || _level.TowersPositions == null)
                return Array.Empty<Transform>();

            return _level.TowersPositions;
        }

        private static void MarkOutdated() => _isOutdated = true;

        // Unity-null: объект Level уничтожен, а ссылка на него осталась.
        private static bool IsLevelDestroyed() => ReferenceEquals(_level, null) == false && _level == null;

        private static bool HasDestroyedRoads()
        {
            foreach (RoadPath road in _roads)
            {
                if (road == null)
                    return true;
            }

            return false;
        }

        private static void Refresh()
        {
            StageHandle stage = StageUtility.GetCurrentStageHandle();
            RoadPath[] roads = stage.FindComponentsOfType<RoadPath>();

            _roads.Clear();
            _roads.AddRange(roads.OrderBy(road => road.Number));
            _level = stage.FindComponentsOfType<Level>().FirstOrDefault();
            _isOutdated = false;
        }
    }
}
