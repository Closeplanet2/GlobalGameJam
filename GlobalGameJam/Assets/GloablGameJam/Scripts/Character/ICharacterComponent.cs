using UnityEngine;

namespace GloablGameJam.Scripts.Character
{
    public interface ICharacterComponent
    {
        void ISetCharacterManager(ICharacterManager characterManager);
        void IHandleCharacterComponent();
    }
}
