using GloablGameJam.Scripts.Animation;
using GloablGameJam.Scripts.Camera;
using UnityEngine;

namespace GloablGameJam.Scripts.Character
{
    public interface ICharacterManager
    {
        IAnimatorController IAnimatorController();
        ICameraManager ICameraManager();
        Rigidbody ICharacterRigidbody();
        bool ITryGetCharacterComponent<T>(out T value) where T : class, ICharacterComponent;
        void ISetCharacterState(CharacterState characterState);
    }
}
