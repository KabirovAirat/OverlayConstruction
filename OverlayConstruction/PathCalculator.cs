using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

namespace OverlayConstruction
{
    public static class PathCalculator
    {
        public static readonly string PathSelectionStrategy = ConfigurationManager.AppSettings["PathSelectionStrategy"];

        public static void CalculatePaths(IEnumerable<Mix> mixes)
        {
            mixes.AsParallel().ForAll(mix =>
            {
                mix.Paths = new List<Path>();
                foreach (var rendezvous in mix.UnderlayNodesWithLatencies.Select(node => node.Key))
                {
                    List<Path> paths = new List<Path>();
                    // Get one relay paths (only in case when rendezvous != entry mix)
                    if (rendezvous != mix.Id)
                    {
                        var oneHopRelays = GetMiddleRelaysForTwoNodes(mixes, mix.Id, rendezvous);
                        foreach (var relay in oneHopRelays)
                        {
                            Path path = new Path();
                            path.RendezvousMix = rendezvous;
                            path.FirstMiddleRelay = relay;
                            path.Latency = mix.NeighborsWithLatencies[relay] + mixes.First(m => m.Id == rendezvous).NeighborsWithLatencies[relay];
                            paths.Add(path);
                        }
                    }

                    // Get 2 relay paths from entry mix if no 1 relay paths found or entry = rendezvous
                    if (!paths.Any())
                    {
                        foreach (var firstRelay in mix.NeighborsWithLatencies)
                        {
                            var twoHopRelays = GetMiddleRelaysForTwoNodes(mixes, firstRelay.Key, rendezvous);
                            foreach (var secondRelay in twoHopRelays)
                            {
                                Path path = new Path();
                                path.RendezvousMix = rendezvous;
                                path.FirstMiddleRelay = firstRelay.Key;
                                path.SecondMiddleRelay = secondRelay;
                                path.Latency = firstRelay.Value + mixes.First(m => m.Id == firstRelay.Key).NeighborsWithLatencies[secondRelay] + mixes.First(m => m.Id == rendezvous).NeighborsWithLatencies[secondRelay];
                                paths.Add(path);
                            }
                        }
                    }
                    // Get rid of cycles (case entry = rendezvous is allowed, 2 middle relays are required in such case)
                    HashSet<Guid> pathsWithCycles = new HashSet<Guid>();
                    foreach (var path in paths)
                    {
                        if (path.SecondMiddleRelay == path.FirstMiddleRelay
                        || path.SecondMiddleRelay == path.RendezvousMix
                        || path.SecondMiddleRelay == mix.Id
                        || path.FirstMiddleRelay == path.RendezvousMix
                        || path.FirstMiddleRelay == mix.Id)
                            pathsWithCycles.Add(path.Id);
                    }

                    paths = paths.Where(p => !pathsWithCycles.Contains(p.Id)).ToList();

                    if (PathSelectionStrategy == "Random")
                    {
                        foreach(var path in paths)
                        {
                            path.Probability = 1.0 / paths.Count();
                        }
                    }
                    else if (PathSelectionStrategy == "Latency-aware")
                    {
                        foreach (var path in paths)
                        {
                            path.Probability = path.Latency < 150000 ?
                                1.0 / paths.Count(p => p.Latency < 150000) : 0;
                        }
                    }
                    else
                        throw new Exception("Path selection strategy should be specified");

                    mix.Paths.AddRange(paths.Where(p => p.Probability > 0).ToList());

                }

            });
        }


        private static IEnumerable<int> GetMiddleRelaysForTwoNodes(IEnumerable<Mix> mixes, int firstNode, int secondNode)
        {
            var relays = mixes.First(m => m.Id == firstNode).NeighborsWithLatencies.Select(n => n.Key)
                .Intersect(mixes.First(m => m.Id == secondNode).NeighborsWithLatencies.Select(n => n.Key));
            return relays;

        }
    }
}
