using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("Music & Ambience Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip menuMusicLoop;
    [SerializeField] private AudioClip lobbyMusicLoop;
    [SerializeField] private AudioClip gameAmbientHumLoop;
    [SerializeField] private AudioClip typingLoop;
    [SerializeField] private AudioClip eventAlarmLoop;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- MUSIC / AMBIENT ---

    public void PlayMenuMusic()
    {
        if (musicSource == null || menuMusicLoop == null) return;

        if (musicSource.clip != menuMusicLoop)
            musicSource.clip = menuMusicLoop;

        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayLobbyMusic()
    {
        if (musicSource == null || lobbyMusicLoop == null) return;

        if (musicSource.isPlaying && musicSource.clip == menuMusicLoop)
            return;

        if (musicSource.clip != lobbyMusicLoop)
            musicSource.clip = lobbyMusicLoop;

        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayGameAmbience()
    {
        if (ambientSource == null || gameAmbientHumLoop == null) return;

        if (ambientSource.clip != gameAmbientHumLoop)
            ambientSource.clip = gameAmbientHumLoop;

        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void StopGameAmbience()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    // --- SFX ---

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayAlarmLoop()
    {
        if (sfxSource == null || eventAlarmLoop == null)
            return;

        if (sfxSource.isPlaying && sfxSource.clip == eventAlarmLoop)
            return;

        sfxSource.clip = eventAlarmLoop;
        sfxSource.loop = true;
        sfxSource.Play();
    }

    public void StopAlarmLoop()
    {
        if (sfxSource == null)
            return;

        if (sfxSource.clip == eventAlarmLoop)
        {
            sfxSource.Stop();
            sfxSource.clip = null;
            sfxSource.loop = false;
        }
    }


    public void PlayTypingLoop()
    {
        if (sfxSource == null || typingLoop == null) return;
        if (sfxSource.isPlaying && sfxSource.clip == typingLoop) return;

        sfxSource.clip = typingLoop;
        sfxSource.loop = true;
        sfxSource.Play();
    }

    public void StopTypingLoop()
    {
        if (sfxSource == null) return;
        if (sfxSource.clip == typingLoop)
        {
            sfxSource.Stop();
            sfxSource.clip = null;
            sfxSource.loop = false;
        }
    }
}
