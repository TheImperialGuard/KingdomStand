using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using TMPro;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.BottomInfoPopup
{
    // Нижняя панель сведений: имя слева, плитки характеристик справа. У башни — урон, темп и дальность, у врага — здоровье
    // с полоской, защита и черты.
    public class BottomInfoPopupView : PopupViewBase
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Color _towerTitleColor = new(0.169f, 0.282f, 0.518f);
        [SerializeField] private Color _enemyTitleColor = new(0.478f, 0.141f, 0.122f);

        [SerializeField] private BottomInfoStatTileView _damageTile;
        [SerializeField] private BottomInfoStatTileView _attacksPerSecondTile;
        [SerializeField] private BottomInfoStatTileView _rangeTile;
        [SerializeField] private BottomInfoStatTileView _healthTile;
        [SerializeField] private BottomInfoStatTileView _resistanceTile;
        [SerializeField] private BottomInfoStatTileView _bossTile;
        [SerializeField] private BottomInfoStatTileView _flyingTile;
        [SerializeField] private RectTransform _healthBarFill;

        [SerializeField] private Sprite _physicalDamageIcon;
        [SerializeField] private Sprite _magicDamageIcon;
        [SerializeField] private Sprite _physicalResistanceIcon;
        [SerializeField] private Sprite _magicResistanceIcon;

        public void SetTowerInfo(string title, DamageTypes damageType, string damageCaption, string attacksPerSecond, string range)
        {
            SetTitle(title, _towerTitleColor);
            SetTowerTilesVisible(true);

            _damageTile.SetIcon(damageType == DamageTypes.Magic ? _magicDamageIcon : _physicalDamageIcon);
            _damageTile.SetCaption(damageCaption);

            _attacksPerSecondTile.SetValue(attacksPerSecond);
            _rangeTile.SetValue(range);
        }

        public void SetTowerDamage(string damage) => _damageTile.SetValue(damage);

        // Значение защиты — подпись её типа или «нет», если защиты у врага нет: тогда плитка приглушена.
        public void SetEnemyInfo(string title, DamageTypes resistanceType, string resistanceValue, bool isBoss, bool isFlying)
        {
            SetTitle(title, _enemyTitleColor);
            SetTowerTilesVisible(false);

            _bossTile.SetVisible(isBoss);
            _flyingTile.SetVisible(isFlying);

            switch (resistanceType)
            {
                case DamageTypes.Physic:
                    SetResistance(_physicalResistanceIcon, resistanceValue, false);
                    break;

                case DamageTypes.Magic:
                    SetResistance(_magicResistanceIcon, resistanceValue, false);
                    break;

                default:
                    SetResistance(_physicalResistanceIcon, resistanceValue, true);
                    break;
            }
        }

        public void SetEnemyHealth(string health, float healthShare)
        {
            _healthTile.SetValue(health);
            _healthBarFill.anchorMax = new Vector2(Mathf.Clamp01(healthShare), _healthBarFill.anchorMax.y);
        }

        private void SetTitle(string title, Color color)
        {
            _title.text = title;
            _title.color = color;
        }

        private void SetTowerTilesVisible(bool isVisible)
        {
            _damageTile.SetVisible(isVisible);
            _attacksPerSecondTile.SetVisible(isVisible);
            _rangeTile.SetVisible(isVisible);

            _healthTile.SetVisible(isVisible == false);
            _resistanceTile.SetVisible(isVisible == false);
            _bossTile.SetVisible(false);
            _flyingTile.SetVisible(false);
        }

        private void SetResistance(Sprite icon, string value, bool isMuted)
        {
            _resistanceTile.SetIcon(icon);
            _resistanceTile.SetValue(value);
            _resistanceTile.SetMuted(isMuted);
        }
    }
}
