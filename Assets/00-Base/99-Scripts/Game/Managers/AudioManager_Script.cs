using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager_Script : MonoBehaviour
{
    public static AudioManager_Script Instance;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public bool loop;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        [Range(1f, 3f)]
        public float maxPitch = 1.5f;

        [HideInInspector]
        public float curPitch;

        [HideInInspector]
        public AudioSource source;
    }

    public Sound[] sounds;

    private Dictionary<string, Sound> soundDictionary;

    private bool vibration = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        soundDictionary = new Dictionary<string, Sound>();

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.loop = s.loop;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.curPitch = s.pitch;

            soundDictionary[s.name] = s;
        }
    }

    public void Play(string soundName, bool increasePitch = false)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            if (increasePitch)
            {
                AutomaticIncreasePitch(soundName);
            }


            if (!s.loop)
                s.source.PlayOneShot(s.clip, s.volume);
            else
                s.source.Play();

        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void Stop(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.Stop();
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void Pause(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.Pause();
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void UnPause(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.UnPause();
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void SetVolume(string soundName, float volume)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.volume = volume;
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void AutomaticIncreasePitch(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.curPitch += + 0.1f;

            if (s.curPitch > s.maxPitch)
                s.curPitch = s.pitch;
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }

        SetPitch(soundName, s.curPitch);
    }

    public void SetPitch(string soundName, float pitch)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.pitch = pitch;
        }
        else
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
    }

    public void ChangeVibrationStatus(bool state)
    {
        vibration = state;
    }
}
