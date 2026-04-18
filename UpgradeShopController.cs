using System.Collections.Generic;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;
using BarnSwarmSniper.Scoring;
using BarnSwarmSniper.Weapon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarnSwarmSniper.UI
{
    /// <summary>
    /// Full Upgrade Shop screen. Dynamically builds category tabs + part list
    /// from WeaponPartCatalog and PlayerProgress, shows locked / owned /
    /// equipped / NEW state, a stat preview panel (current vs with part), and
    /// handles buy/equip with human-readable messages.
    /// </summary>
    public class UpgradeShopController : MonoBehaviour
    {
        // ---------- Inspector refs ----------

        [Header("Data")]
        [SerializeField] private WeaponPartCatalog _catalog;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _pelletsText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Button _closeButton;

        [Header("Category Tabs")]
        [SerializeField] private Transform _categoryTabsContainer;
        [SerializeField] private Button _categoryTabPrefab;   // a Button with a child TMP_Text
        [SerializeField] private Color _categoryActiveColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color _categoryInactiveColor = Color.white;

        [Header("Item List")]
        [SerializeField] private Transform _itemListContainer;
        [SerializeField] private ShopItemButton _itemButtonPrefab;

        [Header("Detail Panel")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailRarity;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailRequirements;
        [SerializeField] private TextMeshProUGUI _detailStatsCurrent;
        [SerializeField] private TextMeshProUGUI _detailStatsWith;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _equipButton;
        [SerializeField] private TextMeshProUGUI _buyButtonLabel;
        [SerializeField] private TextMeshProUGUI _equipButtonLabel;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateLabel;

        // ---------- Runtime ----------

        private PlayerProgressManager _progressManager;
        private CurrencyManager _currencyManager;
        private bool _pelletsSubscribed;

        private readonly List<WeaponPartCategory> _populatedCategories = new();
        private readonly List<Button> _categoryTabs = new();
        private readonly List<ShopItemButton> _itemButtons = new();

        private WeaponPartCategory _selectedCategory;
        private string _selectedPartId;

        // ---------- Unity lifecycle ----------

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_buyButton != null) _buyButton.onClick.AddListener(BuySelected);
            if (_equipButton != null) _equipButton.onClick.AddListener(EquipSelected);
        }

        private void OnEnable()
        {
            ResolveManagers();
            SubscribeToPellets();
            RebuildCategoryTabs();
            if (_populatedCategories.Count > 0)
            {
                SelectCategory(_populatedCategories[0]);
            }
            RefreshHeader();
        }

        private void OnDisable()
        {
            UnsubscribeFromPellets();
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            if (_buyButton != null) _buyButton.onClick.RemoveListener(BuySelected);
            if (_equipButton != null) _equipButton.onClick.RemoveListener(EquipSelected);
            UnsubscribeFromPellets();
        }

        // ---------- Public API (kept backward-compatible) ----------

        public void SelectPart(string partId)
        {
            _selectedPartId = partId;
            if (!string.IsNullOrEmpty(partId) && _progressManager?.CurrentProgress != null)
            {
                _progressManager.CurrentProgress.MarkPartSeen(partId);
                _progressManager.SaveProgress();
            }
            RefreshHighlights();
            RefreshDetailPanel();
        }

        public bool TryBuy(string partId, out string reason)
        {
            ResolveManagers();

            if (_catalog == null) { reason = "No catalog assigned."; SetStatus(reason, true); return false; }
            if (_progressManager?.CurrentProgress == null || _currencyManager == null)
            { reason = "Missing managers."; SetStatus(reason, true); return false; }

            if (!_catalog.TryGetPart(partId, out var part))
            { reason = "Unknown part."; SetStatus(reason, true); return false; }

            var progress = _progressManager.CurrentProgress;

            if (progress.OwnsPart(partId))
            { reason = "Already owned."; SetStatus(reason, false); return false; }

            if (progress.playerLevel < part.requiredPlayerLevel)
            { reason = $"Requires level {part.requiredPlayerLevel}."; SetStatus(reason, true); return false; }

            if (part.requiresPartIds != null)
            {
                for (int i = 0; i < part.requiresPartIds.Length; i++)
                {
                    var req = part.requiresPartIds[i];
                    if (!string.IsNullOrWhiteSpace(req) && !progress.OwnsPart(req))
                    {
                        var nice = _catalog.TryGetPart(req, out var rp) && rp != null && !string.IsNullOrEmpty(rp.displayName) ? rp.displayName : req;
                        reason = $"Requires {nice}."; SetStatus(reason, true); return false;
                    }
                }
            }

            if (part.requiresContractIds != null)
            {
                for (int i = 0; i < part.requiresContractIds.Length; i++)
                {
                    var cid = part.requiresContractIds[i];
                    if (!string.IsNullOrWhiteSpace(cid) && !progress.HasCompletedContract(cid))
                    {
                        reason = $"Requires contract '{cid}'."; SetStatus(reason, true); return false;
                    }
                }
            }

            if (!_currencyManager.TrySpendPellets(part.costPellets))
            { reason = "Not enough pellets."; SetStatus(reason, true); return false; }

            progress.AddOwnedPart(partId);
            _progressManager.UpdatePellets(_currencyManager.PelletsOwned);
            _progressManager.SaveProgress();

            reason = $"Purchased: {(!string.IsNullOrEmpty(part.displayName) ? part.displayName : part.id)}";
            SetStatus(reason, false);
            RefreshItemList();
            RefreshDetailPanel();
            RefreshHeader();
            return true;
        }

        public bool TryEquip(string partId, out string reason)
        {
            ResolveManagers();

            if (_catalog == null) { reason = "No catalog assigned."; SetStatus(reason, true); return false; }
            if (_progressManager?.CurrentProgress == null) { reason = "Missing progress."; SetStatus(reason, true); return false; }

            if (!_catalog.TryGetPart(partId, out var part))
            { reason = "Unknown part."; SetStatus(reason, true); return false; }

            var progress = _progressManager.CurrentProgress;
            if (!progress.OwnsPart(partId)) { reason = "Not owned."; SetStatus(reason, true); return false; }

            // Ammo is a "selected" slot rather than a body-part slot, but we still honour one-per-category.
            progress.EquipPart(part.category, partId);
            if (part.category == WeaponPartCategory.Ammo)
            {
                progress.SetSelectedAmmoId(partId);
            }
            _progressManager.SaveProgress();

            reason = $"Equipped: {(!string.IsNullOrEmpty(part.displayName) ? part.displayName : part.id)}";
            SetStatus(reason, false);
            RefreshItemList();
            RefreshDetailPanel();
            return true;
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // ---------- Category tabs ----------

        private void RebuildCategoryTabs()
        {
            ClearChildren(_categoryTabsContainer);
            _categoryTabs.Clear();
            _populatedCategories.Clear();

            if (_catalog == null) return;

            foreach (var cat in _catalog.GetPopulatedCategories())
            {
                _populatedCategories.Add(cat);
            }
            _populatedCategories.Sort((a, b) => ((int)a).CompareTo((int)b));

            if (_categoryTabPrefab == null || _categoryTabsContainer == null) return;

            for (int i = 0; i < _populatedCategories.Count; i++)
            {
                var cat = _populatedCategories[i];
                var btn = Instantiate(_categoryTabPrefab, _categoryTabsContainer);
                btn.gameObject.SetActive(true);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = cat.ToString().ToUpper();

                var captured = cat;
                btn.onClick.AddListener(() => SelectCategory(captured));
                _categoryTabs.Add(btn);
            }
        }

        private void SelectCategory(WeaponPartCategory category)
        {
            _selectedCategory = category;
            _selectedPartId = null;

            for (int i = 0; i < _categoryTabs.Count; i++)
            {
                var btn = _categoryTabs[i];
                if (btn == null) continue;
                var colors = btn.colors;
                colors.normalColor = (_populatedCategories[i] == category) ? _categoryActiveColor : _categoryInactiveColor;
                btn.colors = colors;
            }

            RefreshItemList();
            RefreshDetailPanel();
        }

        // ---------- Item list ----------

        private void RefreshItemList()
        {
            ClearChildren(_itemListContainer);
            _itemButtons.Clear();

            if (_catalog == null || _itemButtonPrefab == null || _itemListContainer == null)
            {
                if (_emptyStateLabel != null) _emptyStateLabel.SetActive(true);
                return;
            }

            var progress = _progressManager?.CurrentProgress;
            int count = 0;
            foreach (var part in _catalog.GetPartsByCategory(_selectedCategory))
            {
                if (part == null) continue;
                bool owned    = progress != null && progress.OwnsPart(part.id);
                bool equipped = progress != null && progress.GetEquippedPartId(part.category) == part.id;
                bool locked   = progress == null || !MeetsRequirements(progress, part, out _);
                bool seen     = progress != null && progress.HasSeenPart(part.id);

                var btn = Instantiate(_itemButtonPrefab, _itemListContainer);
                btn.gameObject.SetActive(true);
                btn.Bind(part, locked, owned, equipped, !seen);
                btn.OnClicked += SelectPart;
                _itemButtons.Add(btn);
                count++;
            }

            if (_emptyStateLabel != null) _emptyStateLabel.SetActive(count == 0);
            RefreshHighlights();
        }

        private void RefreshHighlights()
        {
            for (int i = 0; i < _itemButtons.Count; i++)
            {
                var b = _itemButtons[i];
                if (b == null) continue;
                b.SetSelectedHighlight(b.PartId == _selectedPartId);
            }
        }

        // ---------- Detail panel ----------

        private void RefreshDetailPanel()
        {
            if (string.IsNullOrEmpty(_selectedPartId) || _catalog == null ||
                !_catalog.TryGetPart(_selectedPartId, out var part) || part == null)
            {
                if (_detailName != null) _detailName.text = "— Select a part —";
                if (_detailRarity != null) _detailRarity.text = string.Empty;
                if (_detailDescription != null) _detailDescription.text = string.Empty;
                if (_detailRequirements != null) _detailRequirements.text = string.Empty;
                if (_detailStatsCurrent != null) _detailStatsCurrent.text = string.Empty;
                if (_detailStatsWith != null) _detailStatsWith.text = string.Empty;
                if (_detailIcon != null) { _detailIcon.sprite = null; _detailIcon.enabled = false; }
                if (_buyButton != null) _buyButton.interactable = false;
                if (_equipButton != null) _equipButton.interactable = false;
                return;
            }

            var progress = _progressManager?.CurrentProgress;
            bool owned    = progress != null && progress.OwnsPart(part.id);
            bool equipped = progress != null && progress.GetEquippedPartId(part.category) == part.id;
            bool canBuy   = progress != null && MeetsRequirements(progress, part, out _) && !owned;

            if (_detailIcon != null)
            {
                _detailIcon.sprite = part.icon;
                _detailIcon.enabled = part.icon != null;
            }
            if (_detailName != null) _detailName.text = string.IsNullOrEmpty(part.displayName) ? part.id : part.displayName;
            if (_detailRarity != null) _detailRarity.text = $"{part.rarity.ToString().ToUpper()} · {part.category.ToString().ToUpper()}";
            if (_detailDescription != null) _detailDescription.text = part.description ?? string.Empty;
            if (_detailRequirements != null) _detailRequirements.text = BuildRequirementsText(progress, part);

            // Stat preview: current aggregate vs "with this part" aggregate
            if (_detailStatsCurrent != null) _detailStatsCurrent.text = BuildAggregateStatsText(progress, null);
            if (_detailStatsWith != null) _detailStatsWith.text = BuildAggregateStatsText(progress, part);

            if (_buyButton != null)
            {
                _buyButton.interactable = canBuy;
                if (_buyButtonLabel != null)
                {
                    _buyButtonLabel.text = owned ? "OWNED" : $"BUY ({part.costPellets}P)";
                }
            }
            if (_equipButton != null)
            {
                _equipButton.interactable = owned && !equipped;
                if (_equipButtonLabel != null)
                {
                    _equipButtonLabel.text = equipped ? "EQUIPPED" : "EQUIP";
                }
            }
        }

        private string BuildRequirementsText(PlayerProgress progress, WeaponPartDefinition part)
        {
            var lines = new List<string>(4);
            if (part.requiredPlayerLevel > 1)
            {
                bool ok = progress != null && progress.playerLevel >= part.requiredPlayerLevel;
                lines.Add((ok ? "[OK]" : "[X]") + $" Level {part.requiredPlayerLevel}");
            }
            if (part.requiresPartIds != null)
            {
                for (int i = 0; i < part.requiresPartIds.Length; i++)
                {
                    var req = part.requiresPartIds[i];
                    if (string.IsNullOrWhiteSpace(req)) continue;
                    bool ok = progress != null && progress.OwnsPart(req);
                    var nice = _catalog != null && _catalog.TryGetPart(req, out var rp) && rp != null && !string.IsNullOrEmpty(rp.displayName) ? rp.displayName : req;
                    lines.Add((ok ? "[OK]" : "[X]") + $" Owns {nice}");
                }
            }
            if (part.requiresContractIds != null)
            {
                for (int i = 0; i < part.requiresContractIds.Length; i++)
                {
                    var cid = part.requiresContractIds[i];
                    if (string.IsNullOrWhiteSpace(cid)) continue;
                    bool ok = progress != null && progress.HasCompletedContract(cid);
                    lines.Add((ok ? "[OK]" : "[X]") + $" Contract '{cid}'");
                }
            }
            return string.Join("\n", lines);
        }

        private string BuildAggregateStatsText(PlayerProgress progress, WeaponPartDefinition addPart)
        {
            var agg = new WeaponStatModifiers();
            if (progress != null && progress.equippedParts != null && _catalog != null)
            {
                for (int i = 0; i < progress.equippedParts.Length; i++)
                {
                    var slot = progress.equippedParts[i];
                    if (slot == null || string.IsNullOrWhiteSpace(slot.partId)) continue;
                    if (!_catalog.TryGetPart(slot.partId, out var p) || p == null) continue;
                    // When previewing "with this part", replace same-category currently equipped
                    if (addPart != null && p.category == addPart.category) continue;
                    StackInto(agg, p.modifiers);
                }
            }
            if (addPart != null) StackInto(agg, addPart.modifiers);

            return
                $"Fire Rate: x{agg.fireRateMultiplier:0.00}\n" +
                $"Recoil: x{agg.recoilAmountMultiplier:0.00} (recovery x{agg.recoilRecoveryMultiplier:0.00})\n" +
                $"Sway: x{agg.swayMultiplier:0.00}\n" +
                $"Aim Assist: radius x{agg.aimAssistFrictionRadiusMultiplier:0.00}, strength x{agg.aimAssistFrictionStrengthMultiplier:0.00}\n" +
                $"Zoom: x{agg.zoomMultiplier:0.00}\n" +
                $"Noise: x{agg.noiseRadiusMultiplier:0.00}\n" +
                $"Magazine: {(agg.magazineSizeDelta >= 0 ? "+" : "")}{agg.magazineSizeDelta}\n" +
                $"Optics Tier Delta: {(agg.maxOpticsTierDelta >= 0 ? "+" : "")}{agg.maxOpticsTierDelta}";
        }

        private static void StackInto(WeaponStatModifiers agg, WeaponStatModifiers m)
        {
            if (m == null) return;
            agg.fireRateMultiplier *= m.fireRateMultiplier;
            agg.recoilAmountMultiplier *= m.recoilAmountMultiplier;
            agg.recoilRecoveryMultiplier *= m.recoilRecoveryMultiplier;
            agg.aimAssistFrictionStrengthMultiplier *= m.aimAssistFrictionStrengthMultiplier;
            agg.aimAssistFrictionRadiusMultiplier *= m.aimAssistFrictionRadiusMultiplier;
            agg.maxOpticsTierDelta += m.maxOpticsTierDelta;
            agg.zoomMultiplier *= m.zoomMultiplier;
            agg.swayMultiplier *= m.swayMultiplier;
            agg.noiseRadiusMultiplier *= m.noiseRadiusMultiplier;
            agg.magazineSizeDelta += m.magazineSizeDelta;
        }

        private bool MeetsRequirements(PlayerProgress progress, WeaponPartDefinition part, out string firstUnmet)
        {
            firstUnmet = null;
            if (progress == null || part == null) { firstUnmet = "missing data"; return false; }

            if (progress.playerLevel < part.requiredPlayerLevel)
            { firstUnmet = $"level {part.requiredPlayerLevel}"; return false; }

            if (part.requiresPartIds != null)
            {
                for (int i = 0; i < part.requiresPartIds.Length; i++)
                {
                    var req = part.requiresPartIds[i];
                    if (!string.IsNullOrWhiteSpace(req) && !progress.OwnsPart(req))
                    { firstUnmet = req; return false; }
                }
            }

            if (part.requiresContractIds != null)
            {
                for (int i = 0; i < part.requiresContractIds.Length; i++)
                {
                    var cid = part.requiresContractIds[i];
                    if (!string.IsNullOrWhiteSpace(cid) && !progress.HasCompletedContract(cid))
                    { firstUnmet = cid; return false; }
                }
            }
            return true;
        }

        // ---------- Header / status ----------

        private void RefreshHeader()
        {
            if (_pelletsText != null)
            {
                int pellets = _currencyManager != null
                    ? _currencyManager.PelletsOwned
                    : (_progressManager?.CurrentProgress?.pelletsOwned ?? 0);
                _pelletsText.text = $"PELLETS: {pellets}";
            }
        }

        private void SetStatus(string message, bool isError)
        {
            if (_statusText == null) return;
            _statusText.text = message ?? string.Empty;
            _statusText.color = isError ? new Color(1f, 0.35f, 0.35f) : new Color(0.7f, 1f, 0.7f);
        }

        // ---------- Buttons ----------

        private void BuySelected()
        {
            if (string.IsNullOrWhiteSpace(_selectedPartId)) return;
            TryBuy(_selectedPartId, out _);
        }

        private void EquipSelected()
        {
            if (string.IsNullOrWhiteSpace(_selectedPartId)) return;
            TryEquip(_selectedPartId, out _);
        }

        // ---------- Wiring ----------

        private void ResolveManagers()
        {
            var gm = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

            if (gm != null)
            {
                _progressManager = gm.PlayerProgressManager;
            }

            if (_progressManager == null)
            {
                _progressManager = FindFirstObjectByType<PlayerProgressManager>(FindObjectsInactive.Include);
            }

            if (_currencyManager == null)
            {
                _currencyManager = FindFirstObjectByType<CurrencyManager>(FindObjectsInactive.Include);
            }
        }

        private void SubscribeToPellets()
        {
            if (_pelletsSubscribed || _currencyManager == null) return;
            _currencyManager.OnPelletsChanged += HandlePelletsChanged;
            _pelletsSubscribed = true;
        }

        private void UnsubscribeFromPellets()
        {
            if (!_pelletsSubscribed || _currencyManager == null) return;
            _currencyManager.OnPelletsChanged -= HandlePelletsChanged;
            _pelletsSubscribed = false;
        }

        private void HandlePelletsChanged(int _)
        {
            RefreshHeader();
        }

        // ---------- Helpers ----------

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c != null) Destroy(c.gameObject);
            }
        }
    }
}
