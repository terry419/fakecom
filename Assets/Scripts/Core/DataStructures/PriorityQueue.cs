using System;
using System.Collections.Generic;

namespace Core.DataStructures
{
    public class PriorityQueue<TElement>
    {
        private List<KeyValuePair<TElement, int>> _elements = new List<KeyValuePair<TElement, int>>();

        public int Count => _elements.Count;

        public void Clear()
        {
            _elements.Clear();
        }

        public void Enqueue(TElement item, int priority)
        {
            _elements.Add(new KeyValuePair<TElement, int>(item, priority));
            HeapifyUp(_elements.Count - 1);
        }

        public TElement Dequeue()
        {
            if (_elements.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            int lastIndex = _elements.Count - 1;
            TElement frontItem = _elements[0].Key;

            _elements[0] = _elements[lastIndex];
            _elements.RemoveAt(lastIndex);

            if (_elements.Count > 0)
            {
                HeapifyDown(0);
            }

            return frontItem;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_elements[index].Value >= _elements[parent].Value) break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = _elements.Count - 1;
            while (true)
            {
                int leftChild = index * 2 + 1;
                if (leftChild > lastIndex) break;

                int rightChild = leftChild + 1;
                int minIndex = leftChild;

                if (rightChild <= lastIndex && _elements[rightChild].Value < _elements[leftChild].Value)
                {
                    minIndex = rightChild;
                }

                if (_elements[index].Value <= _elements[minIndex].Value) break;

                Swap(index, minIndex);
                index = minIndex;
            }
        }

        private void Swap(int a, int b)
        {
            var temp = _elements[a];
            _elements[a] = _elements[b];
            _elements[b] = temp;
        }
    }
}