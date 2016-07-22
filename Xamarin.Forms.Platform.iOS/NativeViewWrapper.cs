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

	public class NativeViewWrapper : BindableNativeView
	{
		public NativeViewWrapper(UIView nativeView, GetDesiredSizeDelegate getDesiredSizeDelegate = null, SizeThatFitsDelegate sizeThatFitsDelegate = null, LayoutSubviewsDelegate layoutSubViews = null)
		{
			GetDesiredSizeDelegate = getDesiredSizeDelegate;
			SizeThatFitsDelegate = sizeThatFitsDelegate;
			LayoutSubViews = layoutSubViews;
			NativeView = nativeView;
		}

		public GetDesiredSizeDelegate GetDesiredSizeDelegate { get; }

		public LayoutSubviewsDelegate LayoutSubViews { get; set; }

		public UIView NativeView { get; }

		public SizeThatFitsDelegate SizeThatFitsDelegate { get; set; }

		internal override object BindableNativeElement => NativeView;

		internal override void SubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener == null)
				propertyListener = new NativeViewPropertyListener(this);
			NativeView.AddObserver(propertyListener, new NSString(item.Key.TargetPropertyName), 0, IntPtr.Zero);

			base.SubscribeTwoWayNative(item);
		}

		internal override void UnSubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
			if (propertyListener != null)
			{
				NativeView.RemoveObserver(propertyListener, new NSString(item.Key.TargetPropertyName), IntPtr.Zero);
				propertyListener.Dispose();
			}
			propertyListener = null;

			base.UnSubscribeTwoWayNative(item);
		}

		NativeViewPropertyListener propertyListener;
	}
}