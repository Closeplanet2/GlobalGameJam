using CustomLibrary.Scripts.GameEventSystem;
using GloablGameJam.Scripts.Character;
using UnityEngine;

namespace GloablGameJam.Events
{
    public class CharacterStateUpdated : BaseEvent
    {
        public CharacterID CharacterID { get; }
        public CharacterState CharacterState { get; }

        public CharacterStateUpdated(CharacterID characterID, CharacterState characterState)
        {
            CharacterID = characterID;
            CharacterState = characterState;
        }
    }
}
