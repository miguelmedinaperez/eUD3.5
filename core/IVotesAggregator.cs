using System.Collections.Generic;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers
{
    public interface IVotesAggregator
    {
        double[] Aggregate(IEnumerable<IEmergingPattern> patterns);
        EmergingPatternClassifier.ClassifierData Data { get; set; }
    }
}