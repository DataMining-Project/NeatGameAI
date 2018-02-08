using System.Collections.Generic;

namespace NeatGameAI.Neat
{
    public class NodeGene
    {
        public int Id { get; set; }
        public NodeType Type { get; set; }
        public List<ConnectionGene> Incoming { get; set; }


        public NodeGene(int id, NodeType type)
        {
            Id = id;
            Type = type;
            Incoming = new List<ConnectionGene>();
        }
    }
}
