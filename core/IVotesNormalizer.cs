namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers
{
    public interface IVotesNormalizer
    {
        double[] Normalize(double[] votes);
        EmergingPatternClassifier.ClassifierData Data { get; set; }
    }
}