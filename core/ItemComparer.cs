using System;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public interface IItemComparer
    {
        SubsetRelation Compare(Item which, Item compareTo);
    }

    public class ItemComparer : IItemComparer
    {
        public SubsetRelation Compare(Item which, Item compareTo)
        {
            if (which.Feature == compareTo.Feature)
                return which.CompareTo(compareTo);
            return SubsetRelation.Unrelated;
        }
    }
    public class MultivariateItemComparer : IItemComparer
    {
        public SubsetRelation Compare(Item which, Item compareTo)
        {
            if (which.Feature == null)
                return which.CompareTo(compareTo);
            if (which.Feature == compareTo.Feature)
                return which.CompareTo(compareTo);
            return SubsetRelation.Unrelated;
        }
    }
}