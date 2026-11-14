using System.Collections.Generic;
using Assets._Project.Develop.Runtime.UI.Core.Views;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.EnemyHints
{
    // Стопка медальонов у левого края safe area, по центру высоты. Сам объект стоит на оси медальонов, медальоны —
    // друг под другом в порядке появления, стопка центрируется по их числу
    public class EnemyHintsView : MonoBehaviour, IView
    {
        private const float Step = 156f;

        // Медальон выезжает из-за края экрана: половина круга и ещё 8 px макета за краем. «!» правее круга не выходит
        private const float HalfWidthBeyondScreenEdge = 72f;

        [SerializeField] private RectTransform _container;
        [SerializeField] private RectTransform _safeArea;

        private readonly List<EnemyHintView> _hints = new();

        public Transform Container => _container;

        public void Add(EnemyHintView hint)
        {
            _hints.Add(hint);

            int index = _hints.Count - 1;

            MoveToPlacesExcept(hint);

            hint.PlayAppear(new Vector2(GetStartX(), GetPlaceY(index)), GetPlace(index));
        }

        // Медальон уходит из стопки, оставшиеся встают на новые места; сам он исчезает своей анимацией
        public void Remove(EnemyHintView hint)
        {
            if (_hints.Remove(hint) == false)
                return;

            MoveToPlacesExcept(null);
        }

        private void MoveToPlacesExcept(EnemyHintView skippedHint)
        {
            for (int i = 0; i < _hints.Count; i++)
            {
                if (_hints[i] != skippedHint)
                    _hints[i].MoveTo(GetPlace(i));
            }
        }

        private Vector2 GetPlace(int index) => new(0f, GetPlaceY(index));

        private float GetPlaceY(int index) => ((_hints.Count - 1) / 2f - index) * Step;

        // Левый край экрана от оси медальонов: ось стоит от края safe area, а safe area — от края экрана на вырез
        private float GetStartX()
        {
            RectTransform screenArea = (RectTransform)_safeArea.parent;
            float cutoutWidth = _safeArea.anchorMin.x * screenArea.rect.width;

            return -(_container.anchoredPosition.x + cutoutWidth + HalfWidthBeyondScreenEdge);
        }
    }
}
