using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xamarin.Forms.Internals;

#if __UNIFIED__
using CoreGraphics;
using Foundation;
using UIKit;

#else
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;

#endif

#if !__UNIFIED__
// Save ourselves a ton of ugly ifdefs below
using CGSize = System.Drawing.SizeF;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	class NativeViewPropertyListener : NSObject
	{
		readonly INativeViewBindableController nativeBindableController;

		public NativeViewPropertyListener(INativeViewBindableController nativeViewBindableController)
		{
			nativeBindableController = nativeViewBindableController;
		}

		public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
		{
			nativeBindableController.OnNativePropertyChange(keyPath);
		}
	}

	public class NativeViewWrapper : View, INativeViewBindableController
	{
		NativeViewPropertyListener propertyListener;
		NativeViewEventListener eventListener;

		public NativeViewWrapper(UIView nativeView, GetDesiredSizeDelegate getDesiredSizeDelegate = null, SizeThatFitsDelegate sizeThatFitsDelegate = null, LayoutSubviewsDelegate layoutSubViews = null)
		{
			GetDesiredSizeDelegate = getDesiredSizeDelegate;
			SizeThatFitsDelegate = sizeThatFitsDelegate;
			LayoutSubViews = layoutSubViews;
			NativeView = nativeView;
			bindableProxies = new Dictionary<BindableProxy, Binding>();
		}

		public GetDesiredSizeDelegate GetDesiredSizeDelegate { get; }

		public LayoutSubviewsDelegate LayoutSubViews { get; set; }

		public UIView NativeView { get; }

		public SizeThatFitsDelegate SizeThatFitsDelegate { get; set; }

		void INativeViewBindableController.UnApplyNativeBindings()
		{
			foreach (var item in bindableProxies)
			{
				item.Value.Unapply();
				item.Key.RemoveBinding(item.Key.Property);
				item.Key.BindingContext = null;

				if (item.Value.Mode == BindingMode.TwoWay)
				{
					UnSubscribeTwoWay(item);
				}
			}

			bindableProxies = null;
		}

		void INativeViewBindableController.ApplyNativeBindings()
		{
			if (NativeBindingExtensions.NativeBindingPool.ContainsKey(NativeView))
				bindableProxies = NativeBindingExtensions.NativeBindingPool[NativeView];

			foreach (var item in bindableProxies)
			{
				item.Key.SetBinding(item.Key.Property, item.Value);
				item.Key.BindingContext = BindingContext;

				if (item.Value.Mode == BindingMode.TwoWay)
				{
					SubscribeTwoWay(item);
				}
			}
		}

		void INativeViewBindableController.OnNativePropertyChange(string property, object newValue)
		{
			foreach (var item in bindableProxies)
			{
				if (item.Key.TargetPropertyName == property.ToString())
				{
					item.Key.OnTargetPropertyChanged(newValue, item.Value.Converter);
				}
			}
		}

		Dictionary<BindableProxy, Binding> bindableProxies;

		void SubscribeTwoWay(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener == null)
				propertyListener = new NativeViewPropertyListener(this);

			NativeView.AddObserver(propertyListener, new NSString(item.Key.TargetPropertyName), 0, IntPtr.Zero);

			if (!string.IsNullOrEmpty(item.Key.TargetEventName))
			{
				eventListener = new NativeViewEventListener(NativeView, item.Key.TargetEventName, item.Key.TargetPropertyName);
				eventListener.NativeViewEventFired += NativeViewEventFired;
			}
		}

		void UnSubscribeTwoWay(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener != null)
			{
				NativeView.RemoveObserver(propertyListener, new NSString(item.Key.TargetPropertyName), IntPtr.Zero);
				propertyListener.Dispose();
			}

			if (eventListener != null)
			{
				eventListener.NativeViewEventFired -= NativeViewEventFired;
				eventListener.Dispose();
			}

			propertyListener = null;
			eventListener = null;
		}

		void NativeViewEventFired(object sender, NativeViewEventFiredEventArgs e)
		{
			(this as INativeViewBindableController).OnNativePropertyChange(e.PropertyName, null);
		}
	}
}