namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers
{
    public interface IEmergingPatternFilter
    {
        bool PassFilter(IEmergingPattern pattern);
        EmergingPatternClassifier.ClassifierData Data { get; set; }

    }
}