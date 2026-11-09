using DG.Tweening;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Level
{
    // Ворота, из которых выходит босс (двери хижины вождя на уровне 6). Только отображение: створки поворачиваются
    // наружу вокруг своих петель — осей узлов модели — и остаются открытыми до конца уровня
    public class BossGate : MonoBehaviour
    {
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;

        [Tooltip("Угол открытия створок: левая поворачивается на +угол вокруг своей оси Y, правая — на −угол")]
        [SerializeField] private float _openAngle = 100f;
        [SerializeField, Min(0)] private float _openDuration = 1f;
        [SerializeField] private Ease _openEase = Ease.OutQuad;

        private Sequence _openSequence;
        private bool _isOpened;

        public bool IsDoor(Transform target) => target.IsChildOf(_leftDoor) || target.IsChildOf(_rightDoor);

        public void Open()
        {
            if (_isOpened)
                return;

            _isOpened = true;

            _openSequence = DOTween.Sequence()
                .Join(RotateDoor(_leftDoor, _openAngle))
                .Join(RotateDoor(_rightDoor, -_openAngle));
        }

        private Tween RotateDoor(Transform door, float angle)
        {
            Vector3 openedRotation = door.localEulerAngles + Vector3.up * angle;

            return door.DOLocalRotate(openedRotation, _openDuration).SetEase(_openEase);
        }

        private void OnDestroy()
        {
            if (_openSequence != null && _openSequence.IsActive())
                _openSequence.Kill();
        }
    }
}
