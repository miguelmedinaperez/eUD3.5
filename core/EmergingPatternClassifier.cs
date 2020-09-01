using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PRFramework.Core.Common;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns.Classifiers
{
    [Serializable]
    public class EmergingPatternClassifier : IEmergingPatternClassifier
    {
        public double[] Classify(Instance instance)
        {
            if (Patterns == null || !Patterns.Any())
                return null;
            if (!_isInitialized)
                Initialize();
            var matchedPattern = _filteredPatterns.Where(p => p.IsMatch(instance));
            if (!matchedPattern.Any())
                return null;
            var selectedPatterns = SelectionPolicy.SelectPatterns(instance, matchedPattern);
            if (!selectedPatterns.Any())
                return null;
            var votes = VotesAggregator.Aggregate(selectedPatterns);
            if (VotesNormalizer != null)
                votes = VotesNormalizer.Normalize(votes);
            return votes;
        }


        private bool _isInitialized = false;
        [NonSerialized]
        private ClassifierData _data;
        private IEmergingPattern[] _filteredPatterns;


        private void Initialize()
        {
            if (!_isInitialized)
            {
                _filteredPatterns = Filters != null ?
                    Patterns.Where(p => Filters.All(f => f.PassFilter(p))).ToArray() :
                    Patterns.ToArray();
                _data = new ClassifierData
                {
                    ClassFeature = Patterns.First().ClassFeature,
                    TrainingInstances = TrainingInstances,
                    AllPatterns = _filteredPatterns,
                };
                if (SelectionPolicy != null)
                    SelectionPolicy.Data = _data;
                if (VotesAggregator != null)
                    VotesAggregator.Data = _data;
                if (VotesNormalizer != null)
                    VotesNormalizer.Data = _data;
            }
            _isInitialized = true;
        }

        public IEnumerable<IEmergingPattern> Patterns { get; set; }

        public IEnumerable<Instance> TrainingInstances { get; set; }

        public IEmergingPatternFilter[] Filters { get; set; }

        public IPatternSelectionPolicy SelectionPolicy { get; set; }

        public IVotesAggregator VotesAggregator{ get; set; }
        
        public IVotesNormalizer VotesNormalizer{ get; set; }
        

        [Serializable]
        public class ClassifierData
        {
            public Feature ClassFeature { get; set; }
            public IEmergingPattern[] AllPatterns { get; set; }
            public IEnumerable<Instance> TrainingInstances { get; set; }
        }

    }
}
