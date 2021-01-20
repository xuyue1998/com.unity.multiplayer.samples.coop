using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Handles enemy AI. Contains AIStateLogics that handle some of the details,
    /// and has various utility functions that are called by those AIStateLogics
    /// </summary>
    public class AIBrain
    {
        private enum AIStateType
        {
            ATTACK,
            //WANDER,
            IDLE,
        }

        private ServerCharacter m_ServerCharacter;
        private ActionPlayer m_ActionPlayer;
        private AIStateType m_CurrentState;
        private Dictionary<AIStateType, AIState> m_Logics;
        private List<ServerCharacter> m_HatedEnemies;

        public AIBrain(ServerCharacter me, ActionPlayer myActionPlayer)
        {
            m_ServerCharacter = me;
            m_ActionPlayer = myActionPlayer;

            m_Logics = new Dictionary<AIStateType, AIState>
            {
                [AIStateType.IDLE] = new IdleAIState(this),
                //[ AIStateType.WANDER ] = new WanderAIState(this), // not written yet
                [AIStateType.ATTACK] = new AttackAIState(this, m_ActionPlayer),
            };
            m_HatedEnemies = new List<ServerCharacter>();
            m_CurrentState = AIStateType.IDLE;
        }

        /// <summary>
        /// Should be called by the AIBrain's owner each Update()
        /// </summary>
        public void Update()
        {
            AIStateType newState = FindBestEligibleAIState();
            if (m_CurrentState != newState)
            {
                m_Logics[newState].Initialize();
            }
            m_CurrentState = newState;
            m_Logics[m_CurrentState].Update();
        }

        private AIStateType FindBestEligibleAIState()
        {
            // for now we assume the AI states are in order of appropriateness,
            // which may be nonsensical when there are more states
            foreach (AIStateType type in Enum.GetValues(typeof(AIStateType)))
            {
                if (m_Logics[type].IsEligible())
                {
                    return type;
                }
            }
            Debug.LogError("No AI states are valid!?!");
            return AIStateType.IDLE;
        }

        /// <summary>
        /// Returns true if it be appropriate for us to murder this character, starting right now!
        /// </summary>
        public bool IsAppropriateFoe(ServerCharacter potentialFoe)
        {
            if (potentialFoe == null || potentialFoe.IsNPC || potentialFoe.NetState.NetworkLifeState.Value != LifeState.ALIVE)
            {
                return false;
            }

            // Also, we could use NavMesh.Raycast() to see if we have line of sight to foe?
            return true;
        }

        /// <summary>
        /// Notify the AIBrain that we should consider this character an enemy.
        /// </summary>
        /// <param name="character"></param>
        public void Hate(ServerCharacter character)
        {
            if (!m_HatedEnemies.Contains(character))
            {
                m_HatedEnemies.Add(character);
            }
        }

        /// <summary>
        /// Return the raw list of hated enemies -- treat as read-only!
        /// </summary>
        public List<ServerCharacter> GetHatedEnemies()
        {
            // first we clean the list -- remove any enemies that have disappeared (became null), are dead, etc.
            m_HatedEnemies.RemoveAll(enemy => !IsAppropriateFoe(enemy));
            return m_HatedEnemies;
        }

        /// <summary>
        /// Retrieve info about who we are. Treat as read-only!
        /// </summary>
        /// <returns></returns>
        public ServerCharacter GetMyServerCharacter()
        {
            return m_ServerCharacter;
        }
    }
}