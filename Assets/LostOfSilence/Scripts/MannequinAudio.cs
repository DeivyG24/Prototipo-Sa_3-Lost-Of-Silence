
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MannequinAudio : MonoBehaviour
{
    [Header("Estalos das articulacoes")]
    [SerializeField] private AudioClip[] estalos;

    [Header("Configuracao")]
    [SerializeField, Range(0f, 1f)]
    private float volume = 0.7f;

    [SerializeField] private float intervaloMinimo = 4f;

    private AudioSource audioSource;
    private float proximoSom;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
    }

    public void TocarEstalo()
    {
        if (estalos == null || estalos.Length == 0)
            return;

        if (audioSource.isPlaying || Time.time < proximoSom)
            return;

        AudioClip clip = estalos[Random.Range(0, estalos.Length)];

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
        proximoSom = Time.time + intervaloMinimo;
    }
}
