using System.Collections.Generic;
using UnityEngine;

namespace BarnSwarmSniper.Data
{
    [CreateAssetMenu(fileName = "ContractSet", menuName = "BarnSwarmSniper/Contracts/Contract Set", order = 21)]
    public class ContractSet : ScriptableObject
    {
        public string setTag = "Story";
        public List<ContractDefinition> contracts = new List<ContractDefinition>();
    }
}

