using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace OverlayConstruction
{
    public static class TopologyConstructor
    {
        private static readonly int LatencyAwareChosenNeighborsCount = int.Parse(ConfigurationManager.AppSettings["LatencyAwareChosenNeighborsCount"]);

        public static readonly string OverlayStrategy = ConfigurationManager.AppSettings["OverlayStrategy"];


        public static IEnumerable<Mix> CreateTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            if (OverlayStrategy == "Random")
                return CreateRandomTopology(dataset);
            else if (OverlayStrategy == "Latency-aware")
                return CreateLatencyAwareTopology(dataset);
            else
                throw new Exception("No such overlay topology construction strategy");
        }

        public static IEnumerable<Mix> InitializeMixes(IEnumerable<UnderlayTopologyItem> dataset)
        {
            List<Mix> mixes = new List<Mix>();

            int mixesCount = dataset.Where(item => item.FirstNode == 0).Count() + 1;
            for (int i = 0; i < mixesCount; i++)
            {
                Mix mix = new Mix();
                mix.Id = i;
                foreach (var item in dataset)
                {
                    if (item.FirstNode == mix.Id)
                        mix.UnderlayNodesWithLatencies.Add(item.SecondNode, item.Latency);
                    if (item.SecondNode == mix.Id)
                        mix.UnderlayNodesWithLatencies.Add(item.FirstNode, item.Latency);
                }
                mix.UnderlayNodesWithLatencies.Add(mix.Id, 0);
                mixes.Add(mix);
            }
            return mixes;
        }

        public static IEnumerable<Mix> CreateRandomTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            var mixes = InitializeMixes(dataset);
            var optimalDegree = 2 * Math.Log(mixes.Count(), 2);
            Random rand = new Random(DateTime.Now.Millisecond);
            var mixesHavingLessNeighbors = mixes.Where(m => !m.isMaxNodeDegreeReached).OrderBy(m => rand.Next()).ToList();
            var overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
            bool stopCondition = false;
            while(mixesHavingLessNeighbors.Count > 1 && !stopCondition)
            {
                HashSet<int> chosenMixesIds = new HashSet<int>();
                foreach (var mix in mixesHavingLessNeighbors)
                {
                    if (mix.isMaxNodeDegreeReached) continue;
                    var neighborMix = mixesHavingLessNeighbors
                        .FirstOrDefault(m => m.Id != mix.Id 
                    && !mix.NeighborsWithLatencies.ContainsKey(m.Id) 
                    && !m.isMaxNodeDegreeReached
                    && !chosenMixesIds.Contains(m.Id));
                    if (neighborMix == null) continue;
                    chosenMixesIds.Add(neighborMix.Id);
                    mix.NeighborsWithLatencies.Add(neighborMix.Id, mix.UnderlayNodesWithLatencies[neighborMix.Id]);
                    neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                    if (mix.NeighborsWithLatencies.Count >= optimalDegree) mix.isMaxNodeDegreeReached = true;
                    if (neighborMix.NeighborsWithLatencies.Count >= optimalDegree) neighborMix.isMaxNodeDegreeReached = true;
                }
                mixesHavingLessNeighbors = mixes.Where(m => !m.isMaxNodeDegreeReached).OrderBy(m => rand.Next()).ToList();
                if (overallNeighborsCount < mixes.Sum(m => m.NeighborsWithLatencies.Count))
                    overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
                else
                    stopCondition = true;
            }
            return mixes;
        }

        public static IEnumerable<Mix> CreateLatencyAwareTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            var mixes = InitializeMixes(dataset);
            var optimalDegree = 2 * Math.Log(mixes.Count(), 2);
            Random rand = new Random(DateTime.Now.Millisecond);
            var mixesHavingLessNeighbors = mixes.Where(m => !m.isMaxNodeDegreeReached).OrderBy(m => rand.Next()).ToList();
            var overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
            bool stopCondition = false;
            while (mixesHavingLessNeighbors.Count > 1 && !stopCondition)
            {
                HashSet<int> chosenMixesIds = new HashSet<int>();
                foreach (var mix in mixesHavingLessNeighbors)
                {
                    if (mix.isMaxNodeDegreeReached) continue;
                    Mix neighborMix = null;
                    if (!mix.isLatencyAwareDegreeReached)
                    {
                        neighborMix = mixesHavingLessNeighbors.OrderBy(m => mix.UnderlayNodesWithLatencies[m.Id])
                            .FirstOrDefault(m => m.Id != mix.Id
                            && !mix.NeighborsWithLatencies.ContainsKey(m.Id)
                            && !m.isMaxNodeDegreeReached
                            && !chosenMixesIds.Contains(m.Id));
                        if (neighborMix != null && mix.NeighborsWithLatencies.Count - 1 == LatencyAwareChosenNeighborsCount)
                            mix.isLatencyAwareDegreeReached = true;
                    }
                    else
                        neighborMix = mixesHavingLessNeighbors
                            .FirstOrDefault(m => m.Id != mix.Id
                            && !mix.NeighborsWithLatencies.ContainsKey(m.Id)
                            && !m.isMaxNodeDegreeReached
                            && !chosenMixesIds.Contains(m.Id));

                    if (neighborMix == null) continue;
                    chosenMixesIds.Add(neighborMix.Id);
                    mix.NeighborsWithLatencies.Add(neighborMix.Id, mix.UnderlayNodesWithLatencies[neighborMix.Id]);
                    neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                    if (mix.NeighborsWithLatencies.Count >= optimalDegree) mix.isMaxNodeDegreeReached = true;
                    if (neighborMix.NeighborsWithLatencies.Count >= optimalDegree) neighborMix.isMaxNodeDegreeReached = true;
                }
                mixesHavingLessNeighbors = mixes.Where(m => !m.isMaxNodeDegreeReached).OrderBy(m => rand.Next()).ToList();
                if (overallNeighborsCount < mixes.Sum(m => m.NeighborsWithLatencies.Count))
                    overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
                else
                    stopCondition = true;
            }
            return mixes;
        }

    }
}
