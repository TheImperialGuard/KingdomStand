using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.CommonViews
{
    // Ценник под медальоном: лента с монетой и ценой. Ширина ленты — по содержимому (группа раскладки). Когда золота не
    // хватает, лента темнеет, цена краснеет; на максимуме вместо цены — надпись («МАКС») без монеты.
    public class MedallionPriceView : MonoBehaviour
    {
        [SerializeField] private Image _ribbon;
        [SerializeField] private GameObject _coin;
        [SerializeField] private TMP_Text _price;

        [SerializeField] private Sprite _ribbonSprite;
        [SerializeField] private Sprite _unaffordableRibbonSprite;
        [SerializeField] private Material _priceMaterial;
        [SerializeField] private Material _unaffordablePriceMaterial;
        [SerializeField] private Color _priceColor;
        [SerializeField] private Color _unaffordablePriceColor;

        public void SetCost(int cost, bool isAffordable)
        {
            _coin.SetActive(true);
            _price.text = cost.ToString();
            ApplyStyle(isAffordable);
        }

        public void SetMax(string text)
        {
            _coin.SetActive(false);
            _price.text = text;
            ApplyStyle(true);
        }

        private void ApplyStyle(bool isAffordable)
        {
            _ribbon.sprite = isAffordable ? _ribbonSprite : _unaffordableRibbonSprite;
            _price.fontSharedMaterial = isAffordable ? _priceMaterial : _unaffordablePriceMaterial;
            _price.color = isAffordable ? _priceColor : _unaffordablePriceColor;
        }
    }
}
