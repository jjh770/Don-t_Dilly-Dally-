using System.Collections;
using TMPro;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public sealed class CountdownView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private GameObject _countdownPanel;

        public void StartCountdown(float duration)
        {
            _countdownPanel.SetActive(true);
            StartCoroutine(CountdownCoroutine(duration));
        }

        private IEnumerator CountdownCoroutine(float duration)
        {
            int remaining = Mathf.CeilToInt(duration);

            while (remaining > 0)
            {
                _countdownText.text = remaining.ToString();
                yield return new WaitForSeconds(1f);
                remaining--;
            }

            _countdownText.text = "START!";
            yield return new WaitForSeconds(0.5f);
            _countdownPanel.SetActive(false);
        }
    }
}
