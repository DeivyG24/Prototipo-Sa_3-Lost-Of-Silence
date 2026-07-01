using System.Collections;
using UnityEngine;

namespace LostOfSilence
{
    public sealed class PsychologicalEventDirector : MonoBehaviour
    {
        [SerializeField] private Transform mannequin;
        [SerializeField] private Transform[] mannequinPositions;
        [SerializeField] private Light[] flickerTargets;
        [SerializeField] private AudioSource eventAudio;
        [SerializeField] private float minEventDelay = 12f;
        [SerializeField] private float maxEventDelay = 28f;

        private AudioClip noiseClip;
        private AudioClip lowToneClip;

        private void Start()
        {
            noiseClip = CreateNoiseClip("DistantStatic", 0.65f, 0.18f);
            lowToneClip = CreateToneClip("LowHouseHum", 54f, 0.9f, 0.16f);
            StartCoroutine(EventRoutine());
        }

        private IEnumerator EventRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minEventDelay, maxEventDelay));
                int eventType = Random.Range(0, 3);

                if (eventType == 0)
                {
                    MoveMannequin();
                }
                else if (eventType == 1)
                {
                    StartCoroutine(FlickerBurst());
                }
                else
                {
                    PlayRandomSound();
                }
            }
        }

        private void MoveMannequin()
        {
            if (mannequin == null || mannequinPositions == null || mannequinPositions.Length == 0)
            {
                return;
            }

            Transform target = mannequinPositions[Random.Range(0, mannequinPositions.Length)];
            mannequin.SetPositionAndRotation(target.position, target.rotation);
            PlayRandomSound();
        }

        private IEnumerator FlickerBurst()
        {
            if (flickerTargets == null || flickerTargets.Length == 0)
            {
                yield break;
            }

            PlayRandomSound();
            for (int i = 0; i < 5; i++)
            {
                foreach (Light targetLight in flickerTargets)
                {
                    if (targetLight != null)
                    {
                        targetLight.enabled = !targetLight.enabled;
                    }
                }

                yield return new WaitForSeconds(Random.Range(0.06f, 0.18f));
            }

            foreach (Light targetLight in flickerTargets)
            {
                if (targetLight != null)
                {
                    targetLight.enabled = true;
                }
            }
        }

        private void PlayRandomSound()
        {
            if (eventAudio == null)
            {
                return;
            }

            eventAudio.pitch = Random.Range(0.82f, 1.1f);
            eventAudio.PlayOneShot(Random.value > 0.45f ? noiseClip : lowToneClip, Random.Range(0.45f, 0.8f));
        }

        private static AudioClip CreateToneClip(string clipName, float frequency, float seconds, float volume)
        {
            const int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * seconds);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float fade = Mathf.Sin(Mathf.Clamp01(i / (float)samples) * Mathf.PI);
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * fade;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNoiseClip(string clipName, float seconds, float volume)
        {
            const int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * seconds);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float fade = Mathf.Sin(Mathf.Clamp01(i / (float)samples) * Mathf.PI);
                data[i] = Random.Range(-1f, 1f) * volume * fade;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
