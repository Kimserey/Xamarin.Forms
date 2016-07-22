using System.Collections.Generic;
using Java.Beans;
using Java.Lang;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.Android
{
	class NativeViewPropertyListener : Object, IPropertyChangeListener
	{
		readonly INativeViewBindableController nativeBindableController;

		public NativeViewPropertyListener(INativeViewBindableController nativeViewBindableController)
		{
			nativeBindableController = nativeViewBindableController;
		}

		public void PropertyChange(PropertyChangeEvent e)
		{
			nativeBindableController.OnNativePropertyChange(e.PropertyName);
		}
	}

	public class NativeViewWrapper : BindableNativeView
	{
		public NativeViewWrapper(global::Android.Views.View nativeView, GetDesiredSizeDelegate getDesiredSizeDelegate = null, OnLayoutDelegate onLayoutDelegate = null,
								 OnMeasureDelegate onMeasureDelegate = null)
		{
			GetDesiredSizeDelegate = getDesiredSizeDelegate;
			NativeView = nativeView;
			OnLayoutDelegate = onLayoutDelegate;
			OnMeasureDelegate = onMeasureDelegate;
			changes = new PropertyChangeSupport(NativeView);
		}

		public GetDesiredSizeDelegate GetDesiredSizeDelegate { get; }

		public global::Android.Views.View NativeView { get; }

		public OnLayoutDelegate OnLayoutDelegate { get; }

		public OnMeasureDelegate OnMeasureDelegate { get; }

		internal override object BindableNativeElement => NativeView;

		IPropertyChangeListener propertyListener;
		readonly PropertyChangeSupport changes;

		internal override void SubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener == null)
			{
				propertyListener = new NativeViewPropertyListener(this);
			}

			changes.AddPropertyChangeListener(item.Key.TargetPropertyName, propertyListener);
			base.SubscribeTwoWayNative(item);
		}

		internal override void UnSubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener != null)
			{
				changes.RemovePropertyChangeListener(item.Key.TargetPropertyName, propertyListener);
				propertyListener.Dispose();
			}

			propertyListener = null;
		}
	}
}