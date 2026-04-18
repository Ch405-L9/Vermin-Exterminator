using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Data
{
    public enum ContractType
    {
        NestEradication,
        HoldTheLine,
        CleanSweep
    }

    [CreateAssetMenu(fileName = "ContractDefinition", menuName = "BarnSwarmSniper/Contracts/Contract Definition", order = 20)]
    public class ContractDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string contractId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Rules")]
        public ContractType type = ContractType.CleanSweep;
        public int requiredPlayerLevel = 1;
        public int basePelletReward = 5;
        public int difficultyIndex = 0;
        public List<ContractChallenge> challenges = new List<ContractChallenge>();

        [Header("Level Hints")]
        public List<string> tileTags = new List<string>();
        public List<string> lightingTags = new List<string>();
    }
}

