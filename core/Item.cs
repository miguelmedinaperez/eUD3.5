using System;
using PRFramework.Core.Common;
using System.Linq;
using System.Collections.Generic;
using PRFramework.Core.Core.SupervisedClassifiers.DecisionTrees.Vectors;
using System.Collections.ObjectModel;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public enum SubsetRelation
    {
        Unknown,
        Unrelated,
        Equal,
        Subset,
        Superset,
        Different,
    }

    [Serializable]
    public abstract class Item
    {
        public Feature Feature { get; set; }

        public InstanceModel Model { get; set; }

        public abstract bool IsMatch(Instance instance);

        public abstract SubsetRelation CompareTo(Item other);
    }

    [Serializable]
    public abstract class SingleValueItem : Item, IComparable
    {
        public double Value { get; set; }

        public int CompareTo(object obj)
        {
            if (obj.GetType() != GetType())
                return -1;
            SingleValueItem other = (SingleValueItem) obj;
            return (other.Value == Value && other.Feature == Feature) ? 0 : -1;
        }

    }
    [Serializable]
    public abstract class MultivariateSingleValueItem : Item, IComparable
    {
        public double Value { get; set; }

        public Feature[] Features { get; set; }

        public int FeaturesHash { get; set; }
        public readonly ReadOnlyDictionary<Feature, double> Weights;

        public double _parallel = 0.001;

        public MultivariateSingleValueItem(Dictionary<Feature, double> weights)
        {
            Weights = new ReadOnlyDictionary<Feature, double>(weights);
        }

        public Feature Feature = null;


        public int CompareTo(object obj)
        {
            if (obj.GetType() != GetType())
                return -1;

            MultivariateSingleValueItem other = (MultivariateSingleValueItem)obj;
            return (other.Value == Value &&
                other.Features.All(p => Features.Contains(p)) &&
                other.Weights.SequenceEqual(Weights)) ? 0 : -1;
        }

    }
    [Serializable]
    public class EqualThanItem : SingleValueItem
    {
        public override bool IsMatch(Instance instance)
        {
            double value = instance[Feature];
            if (FeatureValue.IsMissing(value))
                return true;
            return value == Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asEqual = other as EqualThanItem;
            if (asEqual != null)
                return Value == asEqual.Value ? SubsetRelation.Equal : SubsetRelation.Unrelated;

            var asDifferent = other as DifferentThanItem;
            if (asDifferent != null)
            {
                if (Value == asDifferent.Value)
                    return SubsetRelation.Unrelated;
                int numValues = ((NominalFeature)Feature).Values.Length;
                if (Value != asDifferent.Value)
                    return numValues == 2 ? SubsetRelation.Equal : SubsetRelation.Subset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            return string.Format("{0} == {1}", Feature.Name, Feature.ValueToString(Value));
        }


    }

    [Serializable]
    public class DifferentThanItem : SingleValueItem
    {
        public override bool IsMatch(Instance instance)
        {
            double value = instance[Feature];
            if (FeatureValue.IsMissing(value))
                return true;
            return value != Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asDifferent = other as DifferentThanItem;
            if (asDifferent != null)
                return Value == asDifferent.Value ? SubsetRelation.Equal : SubsetRelation.Unrelated;

            var asEqual = other as EqualThanItem;
            if (asEqual != null)
            {
                if (Value == asEqual.Value)
                    return SubsetRelation.Unrelated;
                int numValues = ((NominalFeature)Feature).Values.Length;
                if (Value != asEqual.Value)
                    return numValues == 2 ? SubsetRelation.Equal : SubsetRelation.Superset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            return string.Format("{0} != {1}", Feature.Name, Feature.ValueToString(Value));
        }


    }

    [Serializable]
    public class LessOrEqualThanItem : SingleValueItem
    {
        public override bool IsMatch(Instance instance)
        {
            double value = instance[Feature];
            if (FeatureValue.IsMissing(value))
                return true;
            return value <= Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asLess = other as LessOrEqualThanItem;
            if (asLess != null)
            {
                if (Value == asLess.Value)
                    return SubsetRelation.Equal;
                return Value > asLess.Value ? SubsetRelation.Superset : SubsetRelation.Subset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            return string.Format("{0} <= {1}", Feature.Name, Feature.ValueToString(Value));
        }


    }

    [Serializable]
    public class GreatherThanItem : SingleValueItem
    {
        public override bool IsMatch(Instance instance)
        {
            double value = instance[Feature];
            if (FeatureValue.IsMissing(value))
                return false;
            return value > Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asGreather = other as GreatherThanItem;
            if (asGreather != null)
            {
                if (Value == asGreather.Value)
                    return SubsetRelation.Equal;
                return Value > asGreather.Value ? SubsetRelation.Subset : SubsetRelation.Superset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            return string.Format("{0} > {1}", Feature.Name, Feature.ValueToString(Value));
        }


    }
    [Serializable]
    public class MultivariateLessOrEqualThanItem : MultivariateSingleValueItem
    {
 
        public MultivariateLessOrEqualThanItem(Dictionary<Feature, double> weights) : base(weights)
        {   
        }
        public override bool IsMatch(Instance instance)
        {
            double value = VectorHelper.ScalarProjection(instance, Features, Weights);
            if (Double.IsNaN(value))
                return false;
            return value <= Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asLess = other as MultivariateLessOrEqualThanItem;
            if (asLess != null)
            {
                if (FeaturesHash != asLess.FeaturesHash)
                    return SubsetRelation.Unrelated;
                if (asLess.Features.Length != Features.Length)
                    return SubsetRelation.Unrelated;
                if (!asLess.Features.All(x => Features.Contains(x)))
                    return SubsetRelation.Unrelated;

                double proportion = asLess.Weights.Values.First() / Weights.Values.First();
                foreach (var key in Weights.Keys)
                {
                    if (Math.Abs(Weights[key] * proportion - asLess.Weights[key]) > _parallel)
                        return SubsetRelation.Unrelated;
                }

                if (Math.Abs(Value * proportion - asLess.Value) < _parallel)
                    return SubsetRelation.Equal;
                return Value * proportion > asLess.Value ? SubsetRelation.Superset : SubsetRelation.Subset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            String linearComb = String.Join(" + ", Weights.Select(x => x.Value + " * " + x.Key.Name));
            return string.Format("{0} <= {1}", linearComb, Value);
        }


    }

    [Serializable]
    public class MultivariateGreatherThanItem : MultivariateSingleValueItem
    {

        public MultivariateGreatherThanItem(Dictionary<Feature, double> weights) : base(weights)
        {
        }
        public override bool IsMatch(Instance instance)
        {
            double value = VectorHelper.ScalarProjection(instance, Features, Weights);
            if (Double.IsNaN(value))
                return false;
            return value > Value;
        }

        public override SubsetRelation CompareTo(Item other)
        {
            var asGreather = other as MultivariateGreatherThanItem;
            if (asGreather != null)
            {
                if (FeaturesHash != asGreather.FeaturesHash)
                    return SubsetRelation.Unrelated;
                if (asGreather.Features.Length != Features.Length)
                    return SubsetRelation.Unrelated;
                if (!asGreather.Features.All(x => Features.Contains(x)))
                    return SubsetRelation.Unrelated;

                double proportion = asGreather.Weights.Values.First() / Weights.Values.First();
                foreach (var key in Weights.Keys)
                {
                    if (Math.Abs(Weights[key] * proportion - asGreather.Weights[key]) > _parallel)
                        return SubsetRelation.Unrelated;
                }

                if (Math.Abs(Value * proportion - asGreather.Value) < _parallel)
                    return SubsetRelation.Equal;
                return Value * proportion > asGreather.Value ? SubsetRelation.Subset : SubsetRelation.Superset;
            }
            return SubsetRelation.Unrelated;
        }

        public override string ToString()
        {
            String linearComb = String.Join(" + ", Weights.Select(x => x.Value + " * " + x.Key.Name));
            return string.Format("{0} > {1}", linearComb, Value);
        }


    }

}
