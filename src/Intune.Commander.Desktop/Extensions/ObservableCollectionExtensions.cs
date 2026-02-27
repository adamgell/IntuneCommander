using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Intune.Commander.Desktop.Extensions;

public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Efficiently replaces all items in an ObservableCollection.
    /// Clears the collection once, then adds items individually.
    /// This triggers only 1 clear event + N add events instead of recreating the collection.
    /// </summary>
    public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
