using Codecepticon.CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecepticon.Utils
{
    class NameGenerator
    {
        public enum RandomNameGeneratorMethods
        {
            None = 0,
            RandomCombinations = 1,
            AvoidTwinCharacters = 2,
            DictionaryWords = 3,
            Markov = 4
        }

        private static readonly Random RandomSeed = new Random();

        private readonly RandomNameGeneratorMethods _generationMethod;

        private readonly string _characterSet;

        private readonly int _length;

        private readonly List<string> _dictionaryWords;

        public NameGenerator(RandomNameGeneratorMethods generationMethod, string characterSet)
        {

            _generationMethod = generationMethod;
            _characterSet = characterSet;
            _length = 0;
            _dictionaryWords = new List<string>();
        }

        public NameGenerator(RandomNameGeneratorMethods generationMethod, string characterSet, int length)
        {

            _generationMethod = generationMethod;
            _characterSet = characterSet;
            _length = length;
            _dictionaryWords = new List<string>();
        }

        public NameGenerator(RandomNameGeneratorMethods generationMethod, string characterSet, int length, List<string> dictionaryWords)
        {
            _generationMethod = generationMethod;
            _characterSet = characterSet;
            _length = length;
            _dictionaryWords = dictionaryWords;
        }

        public string Generate()
        {
            switch (_generationMethod)
            {
                case RandomNameGeneratorMethods.RandomCombinations:
                    return GenerateRandomString();
                case RandomNameGeneratorMethods.AvoidTwinCharacters:
                    return GenerateNoTwinString();
                case RandomNameGeneratorMethods.DictionaryWords:
                    return GenerateDictionaryString();
                case RandomNameGeneratorMethods.Markov:
                    return GenerateMarkovWords();
                    
            }
            return "";
        }

        protected string GenerateRandomString()
        {
            int l = (_length > 0) ? _length : RandomSeed.Next(2, 16);
            return new string(Enumerable.Repeat(_characterSet, l).Select(s => s[RandomSeed.Next(s.Length)]).ToArray());
        }

        protected string GenerateNoTwinString()
        {
            int l = (_length > 0) ? _length : RandomSeed.Next(2, 16);
            char[] value = new char[l];
            int attemptLimit = 100;
            // Set the first char.
            value[0] = _characterSet[RandomSeed.Next(_characterSet.Length)];

            for (int i = 1; i < l; i++)
            {
                char c;
                int attemptCount = 0;
                do
                {
                    c = _characterSet[RandomSeed.Next(_characterSet.Length)];
                } while (c == value[i - 1] && ++attemptCount < attemptLimit);

                if (attemptCount >= attemptLimit)
                {
                    return "";
                }

                value[i] = c;
            }

            return new string(value);
        }

        protected string GenerateDictionaryString()
        {
            int l = (_length > 0) ? _length : RandomSeed.Next(2, 16);
            string value = "";
            for (int i = 0; i < l; i++)
            {
                value += _dictionaryWords[RandomSeed.Next(_dictionaryWords.Count)];
            }
            return value;
        }

        protected string GenerateMarkovWords()
        {
            CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            int count = RandomSeed.Next(CommandLineData.Global.Markov.MinWords, CommandLineData.Global.Markov.MaxWords);
            List<string> words = CommandLineData.Global.Markov.Generator.GenerateWords(count, CommandLineData.Global.Markov.MinLength, CommandLineData.Global.Markov.MaxLength, RandomSeed);
            words = words.Select(w => culture.TextInfo.ToTitleCase(w.ToLower())).ToList();
            return String.Join("", words);
        }
    }
}
