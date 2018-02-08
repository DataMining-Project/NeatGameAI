namespace NeatGameAI.Neat
{
    public class ConnectionGene
    {
        public static int LatestInnovation = 0;

        public int Source { get; set; }
        public int Destination { get; set; }
        public int Innovation { get; set; }
        public double Weight { get; set; }
        public bool Enabled { get; set; }

        public ConnectionGene(int source, int destination, double weight, bool enabled = true)
        {
            Source = source;
            Destination = destination;
            Innovation = LatestInnovation++;
            Weight = weight;
            Enabled = enabled;            
        }

        public ConnectionGene(ConnectionGene connectionGene)
        {
            Source = connectionGene.Source;
            Destination = connectionGene.Destination;
            Innovation = connectionGene.Innovation;
            Weight = connectionGene.Weight;
            Enabled = connectionGene.Enabled;
        }
    }
}
