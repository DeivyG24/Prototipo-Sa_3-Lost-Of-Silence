using System.Collections;
using UnityEngine;

namespace LostOfSilence
{
    [RequireComponent(typeof(Light))]
    public sealed class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float minDelay = 2f;
        [SerializeField] private float maxDelay = 7f;
        [SerializeField] private int minBlinks = 2;
        [SerializeField] private int maxBlinks = 6;

        private Light targetLight;
        private float baseIntensity;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseIntensity = targetLight.intensity;
        }

        private void OnEnable()
        {
            StartCoroutine(FlickerRoutine());
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
                int blinks = Random.Range(minBlinks, maxBlinks + 1);

                for (int i = 0; i < blinks; i++)
                {
                    targetLight.intensity = Random.Range(0.05f, baseIntensity * 0.35f);
                    yield return new WaitForSeconds(Random.Range(0.035f, 0.11f));
                    targetLight.intensity = baseIntensity;
                    yield return new WaitForSeconds(Random.Range(0.025f, 0.12f));
                }
            }
        }
    }
}
