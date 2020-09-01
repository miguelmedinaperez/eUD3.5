using System.Collections.Generic;
using PRFramework.Core.Common;
using PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IEmergingPatternClassifier : ISupervisedClassifier
    {
        IEnumerable<IEmergingPattern> Patterns { get; set; }
        IEmergingPatternFilter[] Filters { get; set; }
        IPatternSelectionPolicy SelectionPolicy { get; set; }
        IVotesAggregator VotesAggregator { get; set; }
        IVotesNormalizer VotesNormalizer { get; set; }
        IEnumerable<Instance> TrainingInstances { get; set; }
    }
}