using System.Collections.Generic;

namespace Dataflow.SaicEnergyTracker
{
  

    public class DataflowOption
    {
        public int BatchSize { get; set; }

        public int MaxDegreeOfParallelism { get; set; }
        public int TriggerInterval { get; set; }
    }
}
