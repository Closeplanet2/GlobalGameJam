using UnityEngine;

namespace CustomLibrary.Scripts.Instance
{
    public abstract class MonoBehaviourInstance<TBase> : MonoBehaviourCast<TBase> where TBase : MonoBehaviourInstance<TBase>
    {
        private static TBase _singleton;

        [Header("MonoBehaviourInstance Settings")]
        [Tooltip("If true, duplicate instances of this class are allowed.")]
        [SerializeField] private bool allowDuplicateInstance = false;

        [Tooltip("If true, the instance persists across scene loads.")]
        [SerializeField] private bool allowPersistence = false;

        public static TBase Instance
        {
            get
            {
                if (_singleton == null)  _singleton = FindObjectOfType<TBase>();
                return _singleton;
            }
        }

        protected virtual void Awake()
        {
            if (_singleton != null && _singleton != this)
            {
                if (!allowDuplicateInstance)
                {
                    Debug.LogWarning( $"[MonoBehaviourInstance] Duplicate instance of {typeof(TBase)} found on {gameObject.name}. Destroying this instance.");
                    Destroy(gameObject);
                }
                return;
            }
            _singleton = this as TBase;
            if (allowPersistence)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
