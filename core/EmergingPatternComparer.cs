using System.Linq;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IEmergingPatternComparer
    {
        SubsetRelation Compare(IEmergingPattern pat1, IEmergingPattern pat2);
    }

    public class EmergingPatternComparer : IEmergingPatternComparer
    {
        private IItemComparer comparer;

        public EmergingPatternComparer(IItemComparer comparer)
        {
            this.comparer = comparer;
        }
        
        public SubsetRelation Compare(IEmergingPattern pat1, IEmergingPattern pat2)
        {
            bool directSubset = IsSubset(pat1, pat2);
            bool inverseSubset = IsSubset(pat2, pat1);
            if (directSubset && inverseSubset)
                return SubsetRelation.Equal;
            else if (directSubset)
                return SubsetRelation.Subset;
            else if (inverseSubset)
                return SubsetRelation.Superset;
            else
                return SubsetRelation.Unrelated;
        }

        private bool IsSubset(IEmergingPattern pat1, IEmergingPattern pat2)
        {
            return pat2.Items.All(x => pat1.Items.Any(y =>
                                              {
                                                  SubsetRelation relation = comparer.Compare(y, x);
                                                  return relation == SubsetRelation.Equal ||
                                                         relation == SubsetRelation.Subset;
                                              }));
        }
    }
}
