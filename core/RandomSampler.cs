using System;
using System.Collections.Generic;

namespace PRFramework.Core.Samplers
{
    [Serializable]
    public class RandomSampler
    {
        public Random RandomGenerator { get; set; }

        public RandomSampler()
        {
            RandomGenerator = new Random(12);
        }

        public List<T> SampleWithoutRepetition<T>(IList<T> population, int sampleSize)
        {
            List<T> result = new List<T>();
            List<T> remaining = new List<T>(population);
            for (int i = 0; i < sampleSize && remaining.Count > 0; i++)
            {
                int idx = RandomGenerator.Next(remaining.Count);
                result.Add(remaining[idx]);
                remaining.RemoveAt(idx);
            }
            return result;
        }

        public List<T> SampleWithoutRepetition<T>(IList<T> population, double[] weights, int sampleSize)
        {
            List<T> result = new List<T>();
            if (population.Count != weights.Length) { throw new InvalidOperationException("Size mismatch: population and weights vector."); }
            if (sampleSize > population.Count) { throw new InvalidOperationException("Not enough values to sample from."); }
            List<T> remaining = new List<T>(population);
            List<double> remaining_weights = new List<double>(weights);
            while (result.Count < sampleSize)
            {
                int r1 = RandomGenerator.Next(remaining.Count);
                double r2 = RandomGenerator.NextDouble();
                if (r2 < remaining_weights[r1])
                {
                    result.Add(remaining[r1]);
                    remaining.RemoveAt(r1);
                    remaining_weights.RemoveAt(r1);
                }
            }
            return result;
        }
    }
}
