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
        void ISetCharacterState(CharacterState characterState);
    }
}
