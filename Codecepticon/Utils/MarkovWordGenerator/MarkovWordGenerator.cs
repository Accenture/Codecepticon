using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Utils.MarkovWordGenerator
{
    /*
     * Taken from https://github.com/MagicMau/ProceduralNameGenerator
     */
    class MarkovWordGenerator
    {
        public int order { get; set; }
        public double smoothing { get; set; }

        private List<MarkovModel> models;

        public MarkovWordGenerator(string trainingDataFile, int order, double smoothing)
        {
            string[] words = File.ReadAllLines(trainingDataFile).ToArray();
            Init(words, order, smoothing);
        }

        protected void Init(IEnumerable<string> trainingData, int order, double smoothing)
        {
            this.order = order;
            this.smoothing = smoothing;
            this.models = new List<MarkovModel>();

            // Identify and sort the alphabet used in the training data
            var letters = new HashSet<char>();
            foreach (var word in trainingData)
            {
                foreach (char c in word)
                {
                    letters.Add(c);
                }
            }
            var domain = letters.OrderBy(c => c).ToList();
            domain.Insert(0, '#');

            // create models
            for (int i = 0; i < order; i++)
            {
                this.models.Add(new MarkovModel(trainingData, order - i, smoothing, domain));
            }
        }

        public MarkovWordGenerator(IEnumerable<string> trainingData, int order, double smoothing)
        {
            Init(trainingData, order, smoothing);
        }

        private string Generate(Random rnd)
        {
            string name = new string('#', this.order);
            char letter = GetLetter(name, rnd);
            while (letter != '#' && letter != '\0')
            {
                name += letter;
                letter = GetLetter(name, rnd);
            }
            return name;
        }

        private char GetLetter(string name, Random rnd)
        {
            char letter = '\0';
            string context = name.Substring(name.Length - this.order);
            foreach (var model in this.models)
            {
                letter = model.Generate(context, rnd);
                if (letter == '\0')
                {
                    context = context.Substring(1);
                }
                else
                {
                    break;
                }
            }
            return letter;
        }

        public List<string> GenerateWords(int count, int minLength, int maxLength, Random rnd)
        {
            int invalidCount = 0;
            List<string> words = new List<string>();
            while (words.Count < count)
            {
                string word = GenerateWord(minLength, maxLength, rnd);
                if (word == null)
                {
                    if (++invalidCount == 100)
                    {
                        // We've tried 100 times to find a valid word, and couldn't. Return an empty dataset.
                        // We're not returning the "words" variable because it would contain less words than "count".
                        return new List<string>();
                    }
                    continue;
                }
                invalidCount = 0;
                words.Add(word);
            }
            return words;
        }

        private string GenerateWord(int minLength, int maxLength, Random rnd)
        {
            string word = Generate(rnd).Replace("#", "");
            if (word.Length < minLength || word.Length > maxLength)
            {
                return null;
            }

            return word;
        }
    }
}
