using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Level
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private List<RoadPath> _roadPaths;
        [SerializeField] private List<Transform> _towersPositions;
        [SerializeField] private GameObject _staticGroup;

        [Tooltip("Свет уровня. Пусто — свет сцены остаётся как есть")]
        [SerializeField] private LevelLightingConfig _lighting;

        [Header("Объекты вне игрового поля: тайл спавна врагов, концы дороги")]
        [SerializeField] private List<GameObject> _boundsExcludedObjects;

        [Tooltip("Ворота, которые открываются, когда появляется босс (двери хижины вождя)")]
        [SerializeField] private List<BossGate> _bossGates;

        private Bounds _fieldBounds;
        private bool _hasFieldBounds;

        public IReadOnlyList<RoadPath> RoadPaths => _roadPaths;
        public IReadOnlyList<Transform> TowersPositions => _towersPositions;
        public LevelLightingConfig Lighting => _lighting;
        public IReadOnlyList<BossGate> BossGates => _bossGates;

        public Bounds FieldBounds => _fieldBounds;
        public bool HasFieldBounds => _hasFieldBounds;

        public void CombineStaticGroup() => StaticBatchingUtility.Combine(CollectBatchedObjects(), _staticGroup);

        // Створки ворот босса не склеиваются: склеенная геометрия не поворачивается вместе с объектом
        private GameObject[] CollectBatchedObjects()
        {
            List<GameObject> batchedObjects = new List<GameObject>();

            foreach (MeshRenderer renderer in _staticGroup.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (IsBossGateDoor(renderer.transform))
                    continue;

                batchedObjects.Add(renderer.gameObject);
            }

            return batchedObjects.ToArray();
        }

        private bool IsBossGateDoor(Transform target)
        {
            foreach (BossGate gate in _bossGates)
            {
                if (gate.IsDoor(target))
                    return true;
            }

            return false;
        }

        // Границы считаются до CombineStaticGroup: после склейки геометрия
        // перестраивается, и опираться на неё для замеров не стоит.
        private void Awake()
        {
            _hasFieldBounds = TryCalculateFieldBounds(out _fieldBounds);

            if (_hasFieldBounds == false)
                Debug.LogWarning($"Не удалось вычислить границы поля для уровня {name}.", this);
        }

        private bool TryCalculateFieldBounds(out Bounds bounds)
        {
            bounds = new Bounds();

            if (_staticGroup == null)
                return false;

            MeshRenderer[] renderers = _staticGroup.GetComponentsInChildren<MeshRenderer>();
            bool isInitialized = false;

            foreach (MeshRenderer renderer in renderers)
            {
                if (IsExcluded(renderer.transform))
                    continue;

                if (isInitialized == false)
                {
                    bounds = renderer.bounds;
                    isInitialized = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return isInitialized;
        }

        private bool IsExcluded(Transform target)
        {
            if (_boundsExcludedObjects == null)
                return false;

            foreach (GameObject excluded in _boundsExcludedObjects)
            {
                if (excluded == null)
                    continue;

                if (target.IsChildOf(excluded.transform))
                    return true;
            }

            return false;
        }
    }
}
