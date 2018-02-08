using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeatGameAI.Neat
{
    public class Genome
    {
        private static Random random = new Random();

        public double Fitness { get; set; }
        public NeatConfig Config { get; set; }
        public List<ConnectionGene> Connections { get; set; }
        public List<NodeGene> Nodes { get; set; }

        public Genome(NeatConfig config)
        {
            Config = config;
            Fitness = 0;
            Connections = new List<ConnectionGene>();
            Nodes = new List<NodeGene>();

            InitializeNodes();            
        }

        private void InitializeNodes()
        {
            int currentNodeId = 0;

            // Add input nodes
            for (int i = 0; i < Config.InputNodesCount; i++)
            {
                var node = new NodeGene(currentNodeId++, NodeType.Input);
                Nodes.Add(node);
            }

            // Add output nodes
            for (int i = 0; i < Config.OutputNodesCount; i++)
            {
                var node = new NodeGene(currentNodeId++, NodeType.Output);
                Nodes.Add(node);
            }
        }

        private void AddNode(NodeType nodeType)
        {
            var node = new NodeGene(Nodes.Count, nodeType);
            Nodes.Add(node);
        }

        public double[] EvaluateNetwork(double[] inputs)
        {
            var outputs = new double[Config.OutputNodesCount];

            var outputsMap = new double[Nodes.Count]; // For saving the computed outputs. Prevents recomputation
            for (int i = 0; i < Config.OutputNodesCount; i++)
            {
                outputsMap[i] = double.NaN;
            }

            for (int i = 0; i < Config.OutputNodesCount; i++)
            {
                int neuronId = Config.InputNodesCount + i;
                outputs[i] = CalculateNeuronOutput(ref inputs, ref outputsMap, neuronId);
            }

            return outputs;
        }

        private double CalculateNeuronOutput(ref double[] inputs, ref double[] outputsMap, int neuronId)
        {
            // Recursion bottom. When we are at an input node
            if (neuronId < Config.InputNodesCount)
                return inputs[neuronId];

            // If already computed, return
            if (!double.IsNaN(outputsMap[neuronId]))
                return outputsMap[neuronId];

            // Sum the weights of the connections
            double sum = 0;
            foreach (var connection in Nodes[neuronId].Incoming)
            {
                if (connection.Enabled) // calculate only enabled connections
                {
                    double output = CalculateNeuronOutput(ref inputs, ref outputsMap, connection.Source);

                    outputsMap[connection.Source] = output;
                    sum += output * connection.Weight;
                }                
            }

            return Sigmoid(sum);
        }

        public void Mutate()
        {            
            if (random.NextDouble() <= Config.NodeMutateProbability)
                AddNodeMutation();
            if (random.NextDouble() <= Config.ConnectionMutateProbability)
                AddConnectionMutation();
            if (random.NextDouble() <= Config.WeightMutateProbability)
                WeightMutation();
            if (random.NextDouble() <= Config.DisableMutateProbability)
                DisableConnectionMutation();
            if (random.NextDouble() <= Config.EnableMutateProbability)
                EnableConnectionMutation();
        }

        private void AddNodeMutation()
        {
            if (Connections.Count > 0)
            {
                // Try to get a random connection that is enabled
                int attempt = 0;
                var randCon = Connections[random.Next(0, Connections.Count)];
                while (!randCon.Enabled)
                {
                    randCon = Connections[random.Next(0, Connections.Count)];
                    attempt++;
                    if (attempt > Config.FindRandomEnabledConnectionMaxAttepts)
                        return;
                }

                randCon.Enabled = false;

                // Create new hidden node to add
                var node = new NodeGene(Nodes.Count, NodeType.Hidden);
                Nodes.Add(node);

                // Create connections with the new node between the old connection nodes
                AddNewConnection(randCon.Source, node.Id, RandomWeight());
                AddNewConnection(node.Id, randCon.Destination, RandomWeight());
            }
        }

        private void AddNewConnection(int source, int destination, double weight)
        {
            var connection = new ConnectionGene(source, destination, weight);
            Connections.Add(connection);
            Nodes[source].Outgoing.Add(connection);
            Nodes[destination].Incoming.Add(connection);
        }

        private void AddConnectionMutation()
        {
            // Pick two nodes that are not connected and are not connected and are not reversed. First can't be output and second can't be input.
            NodeGene sourceNode = null;
            NodeGene destinationNode = null;

            int sourceNodeId = 0;
            int destinationNodeId = 0;

            int attempts = 0;
            int maxAttempts = Config.FindTwoNodesToConnectMaxAttempts;
            bool foundNodes = false;
            while (attempts < maxAttempts)
            {
                attempts++;

                // Get source node that is not from output (from input or hidden(if there are any))
                int randInput = random.Next(0, Config.InputNodesCount);
                if (Config.InputNodesCount + Config.OutputNodesCount < Nodes.Count)
                {
                    int randHidden = random.Next(Config.InputNodesCount + Config.OutputNodesCount, Nodes.Count);
                    sourceNodeId = random.Next(0, 2) == 0 ? randInput : randHidden;
                }
                else sourceNodeId = randInput;

                destinationNodeId = random.Next(Config.InputNodesCount, Nodes.Count);

                if (sourceNodeId < destinationNodeId)
                {
                    bool areConnected = false;
                    foreach (var con in Nodes[sourceNodeId].Outgoing)
                    {
                        if (con.Destination == destinationNodeId)
                        {
                            areConnected = true;
                            break;
                        }
                    }

                    if (!areConnected)
                    {
                        foundNodes = true;
                        break;
                    }
                }
            }

            if (foundNodes)
            {
                // Connection is possible between the two
                AddNewConnection(sourceNode.Id, destinationNode.Id, RandomWeight());
            }            
        }

        private void DisableConnectionMutation()
        {
            if (Connections.Count > 0)
            {
                var randCon = Connections[random.Next(0, Connections.Count)];
                randCon.Enabled = false;
            }
        }

        private void EnableConnectionMutation()
        {
            if (Connections.Count > 0)
            {
                var randCon = Connections[random.Next(0, Connections.Count)];
                randCon.Enabled = false;
            }
        }

        private void WeightMutation()
        {
            foreach (var connection in Connections)
            {
                if (random.NextDouble() < Config.WeightProbability)
                {
                    if (random.NextDouble() < Config.PerturbProbability)
                        connection.Weight += RandomWeight() * Config.PerterbEpsilon;
                    else
                        connection.Weight = RandomWeight();
                }
            }
        }

        private double RandomWeight()
        {
            return 2 * random.NextDouble() - 1;
        }

        private double Sigmoid(double d)
        {
            return 1.0 / (1 + Math.Exp(-4.9 * d));
        }

        public static Genome Crossover(Genome parent1, Genome parent2)
        {
            if (parent1.Fitness < parent2.Fitness)
            {
                var temp = parent1;
                parent1 = parent2;
                parent2 = temp;
            }

            var childGenome = new Genome(parent1.Config);

            // Add hidden nodes to child
            var childHiddenNodesCount = Math.Max(parent1.Nodes.Count, parent2.Nodes.Count) - parent1.Config.InputNodesCount - parent1.Config.OutputNodesCount;
            for (int i = 0; i < childHiddenNodesCount; i++)
            {
                childGenome.AddNode(NodeType.Hidden);
            }

            // Add all connections from the parents
            int p1ConIndex = 0, p2ConIndex = 0;
            while (p1ConIndex < parent1.Connections.Count && p2ConIndex < parent2.Connections.Count)
            {
                var p1Con = parent1.Connections[p1ConIndex];
                var p2Con = parent2.Connections[p2ConIndex];

                // Add connection with highest innovation
                if (p1Con.Innovation < p2Con.Innovation)
                {
                    childGenome.Connections.Add(new ConnectionGene(p2Con));
                    p2ConIndex++;
                }
                else // If same innovation parent1 connection is added, who has the higher fitness
                {                    
                    childGenome.Connections.Add(new ConnectionGene(p1Con));
                    p1ConIndex++;
                }
            }

            while (p1ConIndex < parent1.Connections.Count)
            {
                childGenome.Connections.Add(new ConnectionGene(parent1.Connections[p1ConIndex++]));
            }
            while (p2ConIndex < parent2.Connections.Count)
            {
                childGenome.Connections.Add(new ConnectionGene(parent2.Connections[p2ConIndex++]));
            }

            return childGenome;
        }
    }
}
