using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Xamarin.Forms.Internals;

#if WINDOWS_UWP

namespace Xamarin.Forms.Platform.UWP
#else

namespace Xamarin.Forms.Platform.WinRT
#endif
{
	public class NativeViewWrapper : BindableNativeView
	{
		public NativeViewWrapper(FrameworkElement nativeElement, GetDesiredSizeDelegate getDesiredSizeDelegate = null, ArrangeOverrideDelegate arrangeOverrideDelegate = null,
								 MeasureOverrideDelegate measureOverrideDelegate = null)
		{
			GetDesiredSizeDelegate = getDesiredSizeDelegate;
			ArrangeOverrideDelegate = arrangeOverrideDelegate;
			MeasureOverrideDelegate = measureOverrideDelegate;
			NativeElement = nativeElement;
		}

		public ArrangeOverrideDelegate ArrangeOverrideDelegate
		{
			get; set;
		}

		public GetDesiredSizeDelegate GetDesiredSizeDelegate
		{
			get;
		}

		public MeasureOverrideDelegate MeasureOverrideDelegate
		{
			get; set;
		}

		public FrameworkElement NativeElement
		{
			get;
		}

		internal override object BindableNativeElement => NativeElement;

		internal override void SubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			if (watchers.ContainsKey(item.Key.TargetPropertyName))
				return;

			var watcher = new PropertyListener<object>(NativeElement, item.Key.TargetPropertyName);
			watcher.PropertyChanged += Watcher_PropertyChanged;
			watchers.Add(item.Key.TargetPropertyName, watcher);
			base.SubscribeTwoWayNative(item);
		}
		internal override void UnSubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			var watcher = watchers[item.Key.TargetPropertyName];
			watcher.PropertyChanged -= Watcher_PropertyChanged;
			watcher.Dispose();
			base.UnSubscribeTwoWayNative(item);
		}

		Dictionary<string, PropertyListener<object>> watchers = new Dictionary<string, PropertyListener<object>>();

		void Watcher_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			(this as INativeViewBindableController).OnNativePropertyChange(e.PropertyName);
		}

	}

	internal class PropertyListener<T> : DependencyObject, IDisposable
	{
		public static readonly DependencyProperty TargetPropertyValueProperty = DependencyProperty.Register(nameof(TargetPropertyValue), typeof(object), typeof(PropertyListener<T>), new PropertyMetadata(null, OnPropertyChanged));

		public event PropertyChangedEventHandler PropertyChanged;

		public PropertyListener(DependencyObject target, string propertyPath)
		{
			this.target = target;
			targetPropertyPath = propertyPath;
			BindingOperations.SetBinding(this, TargetPropertyValueProperty, new Windows.UI.Xaml.Data.Binding() { Source = this.target, Path = new PropertyPath(targetPropertyPath), Mode = Windows.UI.Xaml.Data.BindingMode.OneWay });
		}

		public void Dispose()
		{
			ClearValue(TargetPropertyValueProperty);
		}

		public T TargetPropertyValue
		{
			get
			{
				return (T)GetValue(TargetPropertyValueProperty);
			}
		}

		static void OnPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			PropertyListener<T> source = (PropertyListener<T>)sender;

			source.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(source.targetPropertyPath));
		}

		readonly DependencyObject target;
		readonly string targetPropertyPath;
	}
}