using System.Collections.ObjectModel;
using Intune.Commander.Core.Extensions;

namespace Intune.Commander.Core.Tests.Extensions;

public class ObservableCollectionExtensionsTests
{
    [Fact]
    public void ReplaceAll_EmptySource_ClearsCollection()
    {
        var collection = new ObservableCollection<int> { 1, 2, 3 };

        collection.ReplaceAll([]);

        Assert.Empty(collection);
    }

    [Fact]
    public void ReplaceAll_EmptyTarget_AddsAllItems()
    {
        var collection = new ObservableCollection<int>();

        collection.ReplaceAll([10, 20, 30]);

        Assert.Equal(3, collection.Count);
        Assert.Equal([10, 20, 30], collection);
    }

    [Fact]
    public void ReplaceAll_LargeCollection_ReplacesAllItems()
    {
        var collection = new ObservableCollection<int>(Enumerable.Range(1, 10));
        var replacement = Enumerable.Range(1000, 1000).ToList();

        collection.ReplaceAll(replacement);

        Assert.Equal(1000, collection.Count);
        Assert.Equal(1000, collection[0]);
        Assert.Equal(1999, collection[^1]);
    }

    [Fact]
    public void ReplaceAll_RaisesCollectionChangedEvents()
    {
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        var eventCount = 0;

        collection.CollectionChanged += (_, _) => eventCount++;

        collection.ReplaceAll([4, 5]);

        Assert.True(eventCount >= 1);
    }
}
