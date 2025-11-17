/*
 This file is part of TaskMaster.
 Copyright (C) 2025 Aaron Semler

 TaskMaster is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using ListBox = System.Windows.Controls.ListBox;

namespace TaskMaster.Presentation.Behaviors;

/// <summary>
/// Attached behavior to auto-scroll a ListBox to the end when drawn or new items are added.
/// </summary>
public static class ListBoxAutoScroll
{
    private static readonly DependencyProperty HandlersProperty =
        DependencyProperty.RegisterAttached(
            "Handlers",
            typeof(Handlers),
            typeof(ListBoxAutoScroll),
            new PropertyMetadata(null));


    // Keeping references to detach cleanly and rewire on ItemsSource changes
    private sealed class Handlers
    {
        public RoutedEventHandler? LoadedHandler;
        public DependencyPropertyChangedEventHandler? VisibleHandler;
        public EventHandler? ItemsSourceChangedHandler;

        public INotifyCollectionChanged? CurrentNcc;
        public NotifyCollectionChangedEventHandler? CollectionHandler;
        public DependencyPropertyDescriptor? ItemsSourceDescriptor;
    }


    /// <summary>
    /// Identifies the AutoScrollToEnd attached dependency property.
    /// </summary>
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(ListBoxAutoScroll),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    /// <summary>
    /// Sets the AutoScrollToEnd attached property.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="value">The value to set.</param>
    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    /// <summary>
    /// Gets the AutoScrollToEnd attached property.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <returns>The value of the AutoScrollToEnd property.</returns>
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    /// <summary>
    /// Forces the ListBox to scroll to the end (when app is visible).
    /// </summary>
    /// <param name="listBox">The ListBox to scroll.</param>
    public static void ForceScrollToEnd(ListBox listBox)
    {
        if (listBox == null)
        {
            return;
        }

        if (!listBox.IsVisible || listBox.Items.Count == 0)
        {
            return;
        }

        var last = listBox.Items[listBox.Items.Count - 1];
        listBox.ScrollIntoView(last);
    }


    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            var pack = new Handlers();

            // Initial/first-time scroll when control loads
            pack.LoadedHandler = (_, __) => listBox.Dispatcher.InvokeAsync(
                () => ForceScrollToEnd(listBox), DispatcherPriority.ContextIdle);

            listBox.Loaded += pack.LoadedHandler;

            // When control becomes visible again (tab re-activated), defer a tick then scroll
            pack.VisibleHandler = (_, __) =>
            {
                if (listBox.IsVisible)
                {
                    listBox.Dispatcher.InvokeAsync(
                        () => ForceScrollToEnd(listBox),
                        DispatcherPriority.ContextIdle);
                }
            };

            listBox.IsVisibleChanged += pack.VisibleHandler;

            // Track ItemsSource swaps so always listening to current collection
            pack.ItemsSourceDescriptor =
                DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));

            pack.ItemsSourceChangedHandler = (_, __) => RewireCollection(listBox, pack);
            pack.ItemsSourceDescriptor.AddValueChanged(listBox, pack.ItemsSourceChangedHandler);

            // Wire to whatever ItemsSource is present right now
            RewireCollection(listBox, pack);

            listBox.SetValue(HandlersProperty, pack);
        }
        else
        {
            // Detach everything...
            if (listBox.GetValue(HandlersProperty) is Handlers pack)
            {
                if (pack.LoadedHandler != null)
                {
                    listBox.Loaded -= pack.LoadedHandler;
                }

                if (pack.VisibleHandler != null)
                {
                    listBox.IsVisibleChanged -= pack.VisibleHandler;
                }

                if (pack.CurrentNcc != null && pack.CollectionHandler != null)
                {
                    pack.CurrentNcc.CollectionChanged -= pack.CollectionHandler;
                }

                if (pack.ItemsSourceDescriptor != null && pack.ItemsSourceChangedHandler != null)
                {
                    pack.ItemsSourceDescriptor.RemoveValueChanged(listBox, pack.ItemsSourceChangedHandler);
                }
            }

            listBox.ClearValue(HandlersProperty);
        }
    }

    private static void RewireCollection(ListBox listBox, Handlers pack)
    {
        // Detach from previous collection (if any)
        if (pack.CurrentNcc != null && pack.CollectionHandler != null)
        {
            pack.CurrentNcc.CollectionChanged -= pack.CollectionHandler;
            pack.CurrentNcc = null;
            pack.CollectionHandler = null;
        }

        // Attach to current ItemsSource if it supports notifications
        if (listBox.ItemsSource is INotifyCollectionChanged ncc)
        {
            pack.CollectionHandler = (_, __) => ForceScrollToEnd(listBox);
            ncc.CollectionChanged += pack.CollectionHandler;
            pack.CurrentNcc = ncc;
        }

        listBox.Dispatcher.InvokeAsync(() => ForceScrollToEnd(listBox), DispatcherPriority.ContextIdle);
    }
}
