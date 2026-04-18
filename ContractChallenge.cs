using System;

namespace BarnSwarmSniper.Data
{
    public enum ChallengeType
    {
        NoMiss,
        MinHeadshotPercent,
        TimeLimit,
        AmmoLimit,
        MinScore,
        MinKills
    }

    [Serializable]
    public class ContractChallenge
    {
        public string challengeId;
        public string description;
        public ChallengeType type;
        public float thresholdValue;
        public int bonusPelletReward;
    }
}

