using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace OverlayConstruction
{
    public static class TopologyConstructor
    {
        private static readonly int InitiallyChosenNeighborsCount = int.Parse(ConfigurationManager.AppSettings["InitiallyChosenNeighborsCount"]);

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

        private static IEnumerable<Mix> InitializeMixes(IEnumerable<UnderlayTopologyItem> dataset)
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

        private static IEnumerable<Mix> CreateRandomTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            var mixes = InitializeMixes(dataset);
            var maxNodeDegree = 2 * Math.Log(mixes.Count(), 2);
            foreach(var mix in mixes)
            {
                if (mix.isMaxNodeDegreeReached) continue;
                Random rand = new Random(DateTime.Now.Millisecond);
                HashSet<int> chosenNeighbors = new HashSet<int>();
                for (int i = 0;i < InitiallyChosenNeighborsCount;i++)
                {
                    int neighbor = rand.Next(0, mixes.Count());
                    while (neighbor == mix.Id || chosenNeighbors.Contains(neighbor) || mixes.First(m => m.Id == neighbor).isMaxNodeDegreeReached)
                        neighbor = rand.Next(0, mixes.Count());
                    chosenNeighbors.Add(neighbor);
                    if(!mix.NeighborsWithLatencies.ContainsKey(neighbor))
                    {
                        mix.NeighborsWithLatencies.Add(neighbor, mix.UnderlayNodesWithLatencies[neighbor]);
                        var neighborMix = mixes.First(m => m.Id == neighbor);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                        {
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                            if (neighborMix.NeighborsWithLatencies.Count >= maxNodeDegree) neighborMix.isMaxNodeDegreeReached = true;
                        }
                        if(mix.NeighborsWithLatencies.Count() >= maxNodeDegree)
                        {
                            mix.isMaxNodeDegreeReached = true;
                            break;
                        }
                    }
                }
            }
            PostProcessingRandom(mixes);
            return mixes;
        }

        private static IEnumerable<Mix> CreateLatencyAwareTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            var mixes = InitializeMixes(dataset);
            var maxNodeDegree = 2 * Math.Log(mixes.Count(), 2);
            foreach (var mix in mixes)
            {
                if (mix.isMaxNodeDegreeReached) continue;
                var sortedMixes = mixes.Where(m => m.Id != mix.Id && !m.isMaxNodeDegreeReached).OrderBy(m => mix.UnderlayNodesWithLatencies[m.Id]).ToList();
                HashSet<int> chosenNeighbors = new HashSet<int>();
                foreach (var neighborMix in sortedMixes)
                {
                    if (chosenNeighbors.Count == LatencyAwareChosenNeighborsCount) break;
                    if (!mix.NeighborsWithLatencies.ContainsKey(neighborMix.Id))
                    {
                        mix.NeighborsWithLatencies.Add(neighborMix.Id, mix.UnderlayNodesWithLatencies[neighborMix.Id]);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                        {
                            chosenNeighbors.Add(neighborMix.Id);
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                            if (neighborMix.NeighborsWithLatencies.Count >= maxNodeDegree) neighborMix.isMaxNodeDegreeReached = true;
                        }
                        if (mix.NeighborsWithLatencies.Count >= maxNodeDegree)
                        {
                            mix.isMaxNodeDegreeReached = true;
                            break;
                        }
                    }
                }
                if (mix.isMaxNodeDegreeReached) continue;
                Random rand = new Random(DateTime.Now.Millisecond);
                for (int i = 0; i < InitiallyChosenNeighborsCount - chosenNeighbors.Count; i++)
                {
                    int neighbor = rand.Next(0, mixes.Count());
                    while (neighbor == mix.Id || chosenNeighbors.Contains(neighbor) || mixes.First(m => m.Id == neighbor).isMaxNodeDegreeReached)
                        neighbor = rand.Next(0, mixes.Count());
                    chosenNeighbors.Add(neighbor);
                    if (!mix.NeighborsWithLatencies.ContainsKey(neighbor))
                    {
                        mix.NeighborsWithLatencies.Add(neighbor, mix.UnderlayNodesWithLatencies[neighbor]);
                        var neighborMix = mixes.First(m => m.Id == neighbor);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                        {
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                            if (neighborMix.NeighborsWithLatencies.Count >= maxNodeDegree) neighborMix.isMaxNodeDegreeReached = true;
                        }
                        if (mix.NeighborsWithLatencies.Count() >= maxNodeDegree)
                        {
                            mix.isMaxNodeDegreeReached = true;
                            break;
                        }
                    }
                }
            }
            PostProcessingLatencyAware(mixes);
            return mixes;
        }

        private static void PostProcessingRandom(IEnumerable<Mix> mixes)
        {
            var optimalDegree = 2 * Math.Log(mixes.Count(), 2);
            Random rand = new Random(DateTime.Now.Millisecond);
            var mixesHavingLessNeighbors = mixes.Where(m => m.NeighborsWithLatencies.Count < optimalDegree).OrderBy(m => rand.Next()).ToList();
            var overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
            bool stopCondition = false;
            while(mixesHavingLessNeighbors.Count > 1 && !stopCondition)
            {
                foreach(var mix in mixesHavingLessNeighbors)
                {
                    if (mix.NeighborsWithLatencies.Count == optimalDegree) continue;
                    var neighborMix = mixesHavingLessNeighbors.FirstOrDefault(m => m.Id != mix.Id 
                    && !mix.NeighborsWithLatencies.ContainsKey(m.Id) 
                    && m.NeighborsWithLatencies.Count < optimalDegree);
                    if (neighborMix == null) continue;
                    mix.NeighborsWithLatencies.Add(neighborMix.Id, mix.UnderlayNodesWithLatencies[neighborMix.Id]);
                    neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                }
                mixesHavingLessNeighbors = mixes.Where(m => m.NeighborsWithLatencies.Count < optimalDegree).OrderBy(m => rand.Next()).ToList();
                if (overallNeighborsCount < mixes.Sum(m => m.NeighborsWithLatencies.Count))
                    overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
                else
                    stopCondition = true;
            }
        }

        private static void PostProcessingLatencyAware(IEnumerable<Mix> mixes)
        {
            var optimalDegree = 2 * Math.Log(mixes.Count(), 2);
            var mixesHavingLessNeighbors = mixes.Where(m => m.NeighborsWithLatencies.Count < optimalDegree).ToList();
            var overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
            bool stopCondition = false;
            while (mixesHavingLessNeighbors.Count > 1 && !stopCondition)
            {
                foreach (var mix in mixesHavingLessNeighbors)
                {
                    if (mix.NeighborsWithLatencies.Count == optimalDegree) continue;
                    var neighborMix = mixesHavingLessNeighbors.OrderBy(m => mix.UnderlayNodesWithLatencies[m.Id])
                        .FirstOrDefault(m => m.Id != mix.Id
                    && !mix.NeighborsWithLatencies.ContainsKey(m.Id)
                    && m.NeighborsWithLatencies.Count < optimalDegree);
                    if (neighborMix == null) continue;
                    mix.NeighborsWithLatencies.Add(neighborMix.Id, mix.UnderlayNodesWithLatencies[neighborMix.Id]);
                    neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                }
                mixesHavingLessNeighbors = mixes.Where(m => m.NeighborsWithLatencies.Count < optimalDegree).ToList();
                if (overallNeighborsCount < mixes.Sum(m => m.NeighborsWithLatencies.Count))
                    overallNeighborsCount = mixes.Sum(m => m.NeighborsWithLatencies.Count);
                else
                    stopCondition = true;
            }
        }

    }
}
