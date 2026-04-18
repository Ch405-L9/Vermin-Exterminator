using UnityEngine;
using TMPro;
using BarnSwarmSniper.Scoring;

namespace BarnSwarmSniper.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _pelletsText;

        public void UpdateScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE: {score}";
            }
        }

        public void UpdateCombo(int combo)
        {
            if (_comboText != null)
            {
                _comboText.text = combo > 0 ? $"COMBO: {combo}X" : "";
            }
        }

        public void UpdateTimer(float timeRemaining)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                _timerText.text = $"TIME: {minutes:00}:{seconds:00}";
            }
        }

        public void UpdatePellets(int pellets)
        {
            if (_pelletsText != null)
            {
                _pelletsText.text = $"PELLETS: {pellets}";
            }
        }
    }
}
