using System;
using BarnSwarmSniper.Data;
using BarnSwarmSniper.Game;
using BarnSwarmSniper.Scoring;
using BarnSwarmSniper.Weapon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarnSwarmSniper.UI
{
    public class UpgradeShopController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WeaponPartCatalog _catalog;

        [Header("Optional UI")]
        [SerializeField] private TextMeshProUGUI _pelletsText;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Optional Debug Buttons")]
        [SerializeField] private Button _buySelectedButton;
        [SerializeField] private Button _equipSelectedButton;
        [SerializeField] private string _selectedPartId;

        private PlayerProgressManager _progressManager;
        private CurrencyManager _currencyManager;

        private void Awake()
        {
            if (_buySelectedButton != null) _buySelectedButton.onClick.AddListener(BuySelected);
            if (_equipSelectedButton != null) _equipSelectedButton.onClick.AddListener(EquipSelected);
        }

        private void Start()
        {
            ResolveManagers();
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_buySelectedButton != null) _buySelectedButton.onClick.RemoveListener(BuySelected);
            if (_equipSelectedButton != null) _equipSelectedButton.onClick.RemoveListener(EquipSelected);
        }

        public void SelectPart(string partId)
        {
            _selectedPartId = partId;
            RefreshUI();
        }

        public bool TryBuy(string partId, out string reason)
        {
            ResolveManagers();

            if (_catalog == null)
            {
                reason = "No catalog assigned.";
                return false;
            }

            if (_progressManager?.CurrentProgress == null || _currencyManager == null)
            {
                reason = "Missing managers.";
                return false;
            }

            if (!_catalog.TryGetPart(partId, out var part))
            {
                reason = "Unknown part id.";
                return false;
            }

            if (_progressManager.CurrentProgress.OwnsPart(partId))
            {
                reason = "Already owned.";
                return false;
            }

            if (_progressManager.CurrentProgress.playerLevel < part.requiredPlayerLevel)
            {
                reason = $"Requires level {part.requiredPlayerLevel}.";
                return false;
            }

            if (part.requiresPartIds != null)
            {
                for (int i = 0; i < part.requiresPartIds.Length; i++)
                {
                    var req = part.requiresPartIds[i];
                    if (!string.IsNullOrWhiteSpace(req) && !_progressManager.CurrentProgress.OwnsPart(req))
                    {
                        reason = $"Requires {req}.";
                        return false;
                    }
                }
            }

            if (!_currencyManager.TrySpendPellets(part.costPellets))
            {
                reason = "Not enough pellets.";
                return false;
            }

            _progressManager.CurrentProgress.AddOwnedPart(partId);
            _progressManager.UpdatePellets(_currencyManager.PelletsOwned);
            _progressManager.SaveProgress();

            reason = "Purchased.";
            RefreshUI(reason);
            return true;
        }

        public bool TryEquip(string partId, out string reason)
        {
            ResolveManagers();

            if (_catalog == null)
            {
                reason = "No catalog assigned.";
                return false;
            }

            if (_progressManager?.CurrentProgress == null)
            {
                reason = "Missing progress.";
                return false;
            }

            if (!_catalog.TryGetPart(partId, out var part))
            {
                reason = "Unknown part id.";
                return false;
            }

            if (!_progressManager.CurrentProgress.OwnsPart(partId))
            {
                reason = "Not owned.";
                return false;
            }

            _progressManager.CurrentProgress.EquipPart(part.category, partId);
            _progressManager.SaveProgress();

            reason = "Equipped.";
            RefreshUI(reason);
            return true;
        }

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

        private void RefreshUI(string status = null)
        {
            if (_currencyManager != null && _pelletsText != null)
            {
                _pelletsText.text = $"PELLETS: {_currencyManager.PelletsOwned}";
            }

            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
        }
    }
}

