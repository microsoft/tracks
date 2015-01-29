/*
 * Copyright (c) 2015 Microsoft
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.  
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Tracks.Common
{
    /// <summary>
    /// Implementation of IObservableMap that supports reentrancy for use as a default view model.
    /// </summary>
    public class ObservableDictionary : IObservableMap<string, object>
    {
        #region Private members
        /// <summary>
        /// Represents a collection of items for this instance.
        /// </summary>
        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        #endregion

        #region Events
        /// <summary>
        /// Event that triggers when the map has changed.
        /// </summary>
        public event MapChangedEventHandler<string, object> MapChanged;
        #endregion

        /// <summary>
        /// Returns the keys from the collection.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return this._dictionary.Keys; }
        }

        /// <summary>
        /// Returns the values from the collection.
        /// </summary>
        public ICollection<object> Values
        {
            get { return this._dictionary.Values; }
        }

        /// <summary>
        /// Counts elements in the collection.
        /// </summary>
        public int Count
        {
            get { return this._dictionary.Count; }
        }

        /// <summary>
        /// Determines if collection is readonly.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns item at given index.
        /// </summary>
        /// <param name="key">Index for element in collection.</param>
        /// <returns>Element at given key</returns>
        public object this[string key]
        {
            get
            {
                return this._dictionary[key];
            }
            set
            {
                this._dictionary[key] = value;
                this.InvokeMapChanged(CollectionChange.ItemChanged, key);
            }
        }

        /// <summary>
        /// Add an item to the collection based on the key and value.
        /// </summary>
        /// <param name="key">Index for element in collection.</param>
        /// <param name="value">Value for element in collection.</param>
        public void Add(string key, object value)
        {
            this._dictionary.Add(key, value);
            this.InvokeMapChanged(CollectionChange.ItemInserted, key);
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">Item to be added in collection.</param>
        public void Add(KeyValuePair<string, object> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes item with the given key from the collection.
        /// </summary>
        /// <param name="key">Index for item to be removed.</param>
        /// <returns>True if item in collection has been removed. False otherwise.</returns>
        public bool Remove(string key)
        {
            if (this._dictionary.Remove(key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the given item from the collection.
        /// </summary>
        /// <param name="item">Item to be removed from collection.</param>
        /// <returns>True if item has been removed. False otherwise.</returns>
        public bool Remove(KeyValuePair<string, object> item)
        {
            object currentValue;
            if (this._dictionary.TryGetValue(item.Key, out currentValue) && object.Equals(item.Value, currentValue) && this._dictionary.Remove(item.Key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all elements in the collection.
        /// </summary>
        public void Clear()
        {
            var priorKeys = this._dictionary.Keys.ToArray();
            this._dictionary.Clear();
            foreach (var key in priorKeys)
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
            }
        }

        /// <summary>
        /// Determines if the collection contains the given key.
        /// </summary>
        /// <param name="key">Index of item to be checked for existance.</param>
        /// <returns>True if key exists. False otherwise.</returns>
        public bool ContainsKey(string key)
        {
            return this._dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns the value of an item based on the given key.
        /// </summary>
        /// <param name="key">Key for requested item.</param>
        /// <param name="value">Value or requested item.</param>
        /// <returns>True if item can be returned. False otherwise.</returns>
        public bool TryGetValue(string key, out object value)
        {
            return this._dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Determines if the collection contains the given item.
        /// </summary>
        /// <param name="item">Item to be checked for existance.</param>
        /// <returns>True if item exists in collection. False otherwise.</returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
            return this._dictionary.Contains(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>Enumerator that iterates through a collection</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>Returns an enumerator that iterates through a collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._dictionary.GetEnumerator();
        }

        /// <summary>
        /// Copies each element from the current instance of the collection to an array.
        /// </summary>
        /// <param name="array">Array where to copy objects.</param>
        /// <param name="arrayIndex">Index in array where to copy an object.</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            int arraySize = array.Length;
            foreach (var pair in this._dictionary)
            {
                if (arrayIndex >= arraySize)
                {
                    break;
                }
                array[arrayIndex++] = pair;
            }
        }

        /// <summary>
        /// Invoked when a change has been made o the map.
        /// </summary>
        /// <param name="change">Action which will cause a change to the collection.</param>
        /// <param name="key">Key of the item that has changed in the collection.</param>
        private void InvokeMapChanged(CollectionChange change, string key)
        {
            var eventHandler = this.MapChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
            }
        }

        /// <summary>
        /// Implementation of the IMapChangedEventArgs for the dictionary change event.
        /// </summary>
        private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<string>
        {
            /// <summary>
            /// Executes when a change has been made to the collection.
            /// </summary>
            /// <param name="change">Action which will cause a change to the collection.</param>
            /// <param name="key">Key of the item that has changed in the collection.</param>
            public ObservableDictionaryChangedEventArgs(CollectionChange change, string key)
            {
                this.CollectionChange = change;
                this.Key = key;
            }

            /// <summary>
            /// Represents a change that has been made to the collection.
            /// </summary>
            public CollectionChange CollectionChange { get; private set; }

            /// <summary>
            /// Represents the key of the item that has changed in the collection.
            /// </summary>
            public string Key { get; private set; }
        }
    }
}
