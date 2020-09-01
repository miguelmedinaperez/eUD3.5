namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IEmergingPatternSimplifier
    {
        IEmergingPattern Simplify(IEmergingPattern p);
    }

    public class EmergingPatternSimplifier : IEmergingPatternSimplifier
    {
        private FilteredCollection<Item> _collection;

        public EmergingPatternSimplifier(IItemComparer comparer)
        {
            _collection = new FilteredCollection<Item>(comparer.Compare, SubsetRelation.Subset);
        }

        public IEmergingPattern Simplify(IEmergingPattern p)
        {
            EmergingPattern result = new EmergingPattern(p.Model, p.ClassFeature, p.ClassValue);
            if (p.Counts != null)
                result.Counts = (double[])p.Counts.Clone();
            if (p.Supports != null)
                result.Supports = (double[])p.Supports.Clone();
            _collection.SetResultCollection(result.Items);
            _collection.AddRange(p.Items);
            return result;
        }
    }
}
