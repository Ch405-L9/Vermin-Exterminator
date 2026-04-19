using System;
using BarnSwarmSniper.Weapon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarnSwarmSniper.UI
{
    /// <summary>
    /// Per-item row/tile in the Upgrade Shop list. Attach to the prefab that
    /// the UpgradeShopController instantiates for each part entry.
    /// </summary>
    public class ShopItemButton : MonoBehaviour
    {
        [Header("Core UI")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private TextMeshProUGUI _shortStatsText;

        [Header("State Overlays")]
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _ownedBadge;
        [SerializeField] private GameObject _equippedBadge;
        [SerializeField] private GameObject _newBadge;

        [Header("Visual Tint")]
        [SerializeField] private Color _normalTint = Color.white;
        [SerializeField] private Color _lockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        public string PartId { get; private set; }

        public event Action<string> OnClicked;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(WeaponPartDefinition part, bool locked, bool owned, bool equipped, bool isNew)
        {
            if (part == null) return;
            PartId = part.id;

            if (_nameText != null) _nameText.text = string.IsNullOrEmpty(part.displayName) ? part.id : part.displayName;
            if (_costText != null) _costText.text = owned ? (equipped ? "EQUIPPED" : "OWNED") : $"{part.costPellets} P";
            if (_rarityText != null) _rarityText.text = part.rarity.ToString().ToUpper();
            if (_shortStatsText != null) _shortStatsText.text = part.GetShortStatLine();
            if (_iconImage != null)
            {
                _iconImage.sprite = part.icon;
                _iconImage.enabled = part.icon != null;
                _iconImage.color = locked ? _lockedTint : _normalTint;
            }

            if (_lockIcon != null)    _lockIcon.SetActive(locked);
            if (_ownedBadge != null)  _ownedBadge.SetActive(owned && !equipped);
            if (_equippedBadge != null) _equippedBadge.SetActive(equipped);
            if (_newBadge != null)    _newBadge.SetActive(isNew && !owned);

            if (_button != null) _button.interactable = true; // always selectable (locked state shown visually; gating enforced on buy)
        }

        public void SetSelectedHighlight(bool selected)
        {
            // Optional visual; rely on Button's own selected state by default.
            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = selected ? new Color(1f, 0.85f, 0.25f, 1f) : _normalTint;
                _button.colors = colors;
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(PartId);
        }
    }
}
