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

            foreach(var mix in mixes)
            {
                Random rand = new Random(DateTime.Now.Millisecond);
                HashSet<int> chosenNeighbors = new HashSet<int>();
                for (int i = 0;i < InitiallyChosenNeighborsCount;i++)
                {
                    int neighbor = rand.Next(0, mixes.Count());
                    while (neighbor == mix.Id || chosenNeighbors.Contains(neighbor))
                        neighbor = rand.Next(0, mixes.Count());
                    chosenNeighbors.Add(neighbor);
                    if(!mix.NeighborsWithLatencies.ContainsKey(neighbor))
                    {
                        mix.NeighborsWithLatencies.Add(neighbor, mix.UnderlayNodesWithLatencies[neighbor]);
                        var neighborMix = mixes.First(m => m.Id == neighbor);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                    }
                }
            }
            return mixes;
        }

        private static IEnumerable<Mix> CreateLatencyAwareTopology(IEnumerable<UnderlayTopologyItem> dataset)
        {
            var mixes = InitializeMixes(dataset);

            foreach (var mix in mixes)
            {
                var latencyAwareNeighbors = mix.UnderlayNodesWithLatencies.Where(n => n.Key != mix.Id).OrderBy(pair => pair.Value).Take(LatencyAwareChosenNeighborsCount).ToList();
                foreach(var neighbor in latencyAwareNeighbors)
                {
                    if (!mix.NeighborsWithLatencies.ContainsKey(neighbor.Key))
                    {
                        mix.NeighborsWithLatencies.Add(neighbor.Key, neighbor.Value);
                        var neighborMix = mixes.First(m => m.Id == neighbor.Key);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                    }
                }

                Random rand = new Random(DateTime.Now.Millisecond);
                HashSet<int> chosenNeighbors = new HashSet<int>(latencyAwareNeighbors.Select(n => n.Key));
                for (int i = 0; i < InitiallyChosenNeighborsCount - LatencyAwareChosenNeighborsCount; i++)
                {
                    int neighbor = rand.Next(0, mixes.Count());
                    while (neighbor == mix.Id || chosenNeighbors.Contains(neighbor))
                        neighbor = rand.Next(0, mixes.Count());
                    chosenNeighbors.Add(neighbor);
                    if (!mix.NeighborsWithLatencies.ContainsKey(neighbor))
                    {
                        mix.NeighborsWithLatencies.Add(neighbor, mix.UnderlayNodesWithLatencies[neighbor]);
                        var neighborMix = mixes.First(m => m.Id == neighbor);
                        if (!neighborMix.NeighborsWithLatencies.ContainsKey(mix.Id))
                            neighborMix.NeighborsWithLatencies.Add(mix.Id, neighborMix.UnderlayNodesWithLatencies[mix.Id]);
                    }
                }
            }
            return mixes;
        }

        
    }
}
