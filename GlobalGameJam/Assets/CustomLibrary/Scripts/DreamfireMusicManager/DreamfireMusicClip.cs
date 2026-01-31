using UnityEngine;

namespace CustomLibrary.Scripts.DreamfireMusicManager
{
    [CreateAssetMenu(order = 1, menuName = "DreamfireStudiosGameLibrary/DreamfireMusicManager/DreamfireMusicClip", fileName = "DreamfireMusicClip")]
    public class DreamfireMusicClip : ScriptableObject
    {
        [Header("Audio Configuration")]
        [SerializeField] private AudioClip clip;
        [SerializeField] private float startPoint = 0f;
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private bool loop = false;
        [SerializeField] private bool isMusic = true;

        public AudioClip Clip => clip;
        public float StartPoint => startPoint;
        public float StartDelay => startDelay;
        public bool Loop => loop;
        public bool IsMusic => isMusic;
        public float ClipLength
        {
            get
            {
                if (clip != null)
                {
                    return clip.length;
                }

                return 0f;
            }
        }

        public void Play(AudioSource source)
        {
            if (source == null)
            {
                Debug.LogWarning("[DreamfireMusicClip] Cannot play: AudioSource is null.");
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning("[DreamfireMusicClip] Cannot play: AudioClip is not assigned.");
                return;
            }

            float safeStart = Mathf.Clamp(startPoint, 0f, clip.length);
            source.clip = clip;
            source.time = safeStart;
            source.loop = loop;
            source.PlayDelayed(startDelay);
        }
    }
}