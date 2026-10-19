using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NetP3DLib.P3D.Collections;

public class SizeAwareList<T> : ObservableCollection<T>
{
    private readonly Chunk _chunk;
    private readonly Action<uint> _onChange;

    public SizeAwareList(Chunk chunk, Action<uint> onChange, int capacity = 0) : base(new List<T>(capacity))
    {
        _chunk = chunk;
        _onChange = onChange ?? throw new ArgumentNullException(nameof(onChange));
    }

    private void TrackSizeChange(Action action)
    {
        uint oldSize = _chunk.HeaderSize;
        action();
        _onChange(oldSize);
    }

    protected override void InsertItem(int index, T item) => TrackSizeChange(() => base.InsertItem(index, item));

    protected override void RemoveItem(int index) => TrackSizeChange(() => base.RemoveItem(index));

    protected override void SetItem(int index, T item) => TrackSizeChange(() => base.SetItem(index, item));

    protected override void ClearItems() => TrackSizeChange(base.ClearItems);

    /// <summary>
    /// Adds a collection of items at the end.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemsList = items as IList<T> ?? [.. items];
        if (itemsList.Count == 0)
            return;

        CheckReentrancy();

        TrackSizeChange(() =>
        {
            if (Items is List<T> list)
                list.AddRange(itemsList);
            else
                foreach (var item in itemsList)
                    Items.Add(item);
        });

        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)itemsList));
    }

    /// <summary>
    /// Inserts a collection of items starting at a specific index.
    /// </summary>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemsList = items as IList<T> ?? [.. items];
        if (itemsList.Count == 0)
            return;

        CheckReentrancy();

        TrackSizeChange(() =>
        {
            if (Items is List<T> list)
            {
                list.InsertRange(index, itemsList);
            }
            else
            {
                int i = index;
                foreach (var item in itemsList)
                    Items.Insert(i++, item);
            }
        });

        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)itemsList, index));
    }

    /// <summary>
    /// Removes a range of items starting at a specific index.
    /// </summary>
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (index + count > Count)
            throw new ArgumentException("Range exceeds list bounds.");

        CheckReentrancy();

        var removedItems = new T[count];
        var j = 0;
        for (int i = index; i < index + count; i++)
            removedItems[j++] = Items[i];

        TrackSizeChange(() =>
        {
            if (Items is List<T> list)
            {
                list.RemoveRange(index, count);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    Items.RemoveAt(index);
            }
        });

        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
    }

    /// <summary>Finds the index of the first element matching the predicate.</summary>
    public int FindIndex(Predicate<T> match)
    {
        if (match == null) throw new ArgumentNullException(nameof(match));

        if (Items is List<T> list)
            return list.FindIndex(match);

        for (int i = 0; i < Count; i++)
            if (match(Items[i]))
                return i;

        return -1;
    }
}