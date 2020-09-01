using System.Collections.Generic;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers
{
    public interface IPatternSelectionPolicy
    {
        IEnumerable<IEmergingPattern> SelectPatterns(Instance instance, IEnumerable<IEmergingPattern> patterns);
        EmergingPatternClassifier.ClassifierData Data { get; set; }
    }
}