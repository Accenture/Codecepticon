using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Utils.MarkovWordGenerator
{
    /*
     * Taken from https://github.com/MagicMau/ProceduralNameGenerator
     */
    class MarkovModel
    {
        private int order;
        private double smoothing;
        private List<char> alphabet;
        private Dictionary<string, List<char>> observations;
        private Dictionary<string, List<double>> chains;

        public MarkovModel(IEnumerable<string> trainingData, int order, double smoothing, List<char> alphabet)
        {
            this.order = order;
            this.smoothing = smoothing;
            this.alphabet = alphabet;

            observations = new Dictionary<string, List<char>>();
            Retrain(trainingData);
        }

        public char Generate(string context, Random rnd)
        {
            List<double> chain;
            if (chains.TryGetValue(context, out chain))
            {
                return alphabet[SelectIndex(chain, rnd)];
            }
                
            return '\0';
        }

        public void Retrain(IEnumerable<string> trainingData)
        {
            Train(trainingData);
            BuildChains();
        }

        private void Train(IEnumerable<string> trainingData)
        {
            foreach (var d in trainingData)
            {
                string data = new string('#', order) + d + '#';
                for (int i = 0; i < data.Length - order; i++)
                {
                    string key = data.Substring(i, order);
                    List<char> value;
                    if (!observations.TryGetValue(key, out value))
                    {
                        value = new List<char>();
                        observations[key] = value;
                    }
                    value.Add(data[i + order]);
                }
            }
        }

        private void BuildChains()
        {
            chains = new Dictionary<string, List<double>>();

            foreach (string context in observations.Keys)
            {
                foreach (char prediction in alphabet)
                {
                    List<double> chain;
                    if (!chains.TryGetValue(context, out chain))
                    {
                        chain = new List<double>();
                        chains[context] = chain;
                    }
                    int count = 0;
                    List<char> observation;
                    if (observations.TryGetValue(context, out observation))
                    {
                        count = observation.Count(c => c == prediction);
                    }
                        
                    chain.Add(smoothing + count);
                }
            }
        }

        private int SelectIndex(List<double> chain, Random rnd)
        {
            var totals = new List<double>();
            double accumulator = 0f;

            foreach (var weight in chain)
            {
                accumulator += weight;
                totals.Add(accumulator);
            }

            var rand = rnd.NextDouble() * accumulator;

            for (int i = 0; i < totals.Count; i++)
            {
                if (rand < totals[i])
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
