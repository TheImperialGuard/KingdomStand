using System;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.StartStagesPopup
{
    // Окно вызова волны — старт боя или досрочный вызов следующей: медальон срабатывает со второго нажатия, первое
    // раскрывает подсказку. Указатель обучения встраивается в эту логику, а не повторяет её: узнаёт о первом нажатии и
    // может его отменить, как щелчок мимо.
    public interface IStageCallPopup
    {
        event Action InfoShown;

        event Action<PopupPresenterBase, bool> CloseRequest;

        RectTransform ButtonRect { get; }

        void CancelInfo();
    }
}
