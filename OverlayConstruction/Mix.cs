using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlayConstruction
{
    public class Mix
    {
        public int Id { get; set; }

        public Dictionary<int, int> UnderlayNodesWithLatencies { get; set; } = new Dictionary<int, int>();

        public Dictionary<int, int> NeighborsWithLatencies { get; set; } = new Dictionary<int, int>();

        public int BandwidthCapacity { get; set; } = 420;

        public List<Path> Paths { get; set; }
    }

    public class UnderlayTopologyItem
    {
        public int FirstNode { get; set; }

        public int SecondNode { get; set; }

        public int Latency { get; set; }
    }

    public class Path
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int RendezvousMix { get; set; }

        public int FirstMiddleRelay { get; set; }

        public int? SecondMiddleRelay { get; set; } 

        public int? Latency { get; set; }

        public double Probability { get; set; }
    }

}
