using System;
using System.Collections.Generic;

namespace PRFramework.Core.SupervisedClassifiers.EmergingPatterns
{
    public class FilteredCollection<T>
    {
        private Func<T, T, SubsetRelation> _comparer;
        private SubsetRelation _relationToFind, _inverseRelation;
        private List<T> _current;

        public Action<T, T> IsSubsetOrEqualOf { get; set; }

        public FilteredCollection(Func<T, T, SubsetRelation> comparer, SubsetRelation relationToFind, List<T> result = null)
        {
            _relationToFind = relationToFind;
            _inverseRelation = SubsetRelation.Unrelated;
            switch (_relationToFind)
            {
                case SubsetRelation.Superset:
                    _inverseRelation = SubsetRelation.Subset;
                    break;
                case SubsetRelation.Subset:
                    _inverseRelation = SubsetRelation.Superset;
                    break;
                case SubsetRelation.Equal:
                    _inverseRelation = SubsetRelation.Different;
                    break;
            }
            _comparer = comparer;
            if (result == null)
                _current = new List<T>();
            else
                _current = result;
        }


        public void SetResultCollection (List<T> current)
        {
            _current = current;
        }

        public void Add(T item)
        {
            if (_relationToFind != SubsetRelation.Unrelated)
            {
                for (int i = 0; i < _current.Count; )
                {
                    var relation = _comparer(item, _current[i]);
                    if (relation == SubsetRelation.Equal || relation == _inverseRelation)
                    {
                        if (IsSubsetOrEqualOf != null)
                            IsSubsetOrEqualOf(item, _current[i]);
                        return;
                    }
                    else if (relation == _relationToFind)
                    {
                        if (IsSubsetOrEqualOf != null)
                            IsSubsetOrEqualOf(_current[i], item);
                        _current.RemoveAt(i);
                    }
                    else
                        i++;
                }
            }
            _current.Add(item);
        }

        public IEnumerable<T> GetItems()
        {
            return _current;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public void Clear()
        {
            _current.Clear();
        }
    }
}
