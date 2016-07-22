using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	internal abstract class BindableNativeView : View, INativeViewBindableController
	{
		protected BindableNativeView()
		{
			bindableProxies = new Dictionary<BindableProxy, Binding>();
		}

		internal abstract object BindableNativeElement
		{
			get;
		}

		internal virtual void SubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
		}

		internal virtual void UnSubscribeTwoWayNative(KeyValuePair<BindableProxy, Binding> item)
		{
		}

		void INativeViewBindableController.ApplyNativeBindings()
		{
			if (FormsNativeBindingExtensions.NativeBindingPool.ContainsKey(BindableNativeElement))
				bindableProxies = FormsNativeBindingExtensions.NativeBindingPool[BindableNativeElement];

			foreach (var item in bindableProxies)
			{
				item.Key.SetBinding(item.Key.Property, item.Value);
				item.Key.BindingContext = BindingContext;

				if (item.Value.Mode == BindingMode.TwoWay)
					SubscribeTwoWay(item);
			}
		}

		void INativeViewBindableController.OnNativePropertyChange(string property, object newValue)
		{
			foreach (var item in bindableProxies)
			{
				if (item.Key.TargetPropertyName == property.ToString())
				{
					item.Key.OnTargetPropertyChanged(newValue);
				}
			}
		}

		void INativeViewBindableController.UnApplyNativeBindings()
		{
			foreach (var item in bindableProxies)
			{
				item.Value.Unapply();
				item.Key.RemoveBinding(item.Key.Property);
				item.Key.BindingContext = null;

				if (item.Value.Mode == BindingMode.TwoWay)
					UnSubscribeTwoWay(item);
			}

			bindableProxies = null;
		}

		Dictionary<BindableProxy, Binding> bindableProxies;
		NativeViewEventListener eventListener;

		void SubscribeTwoWay(KeyValuePair<BindableProxy, Binding> item)
		{
			var incp = BindableNativeElement as INotifyPropertyChanged;
			if (incp != null)
				incp.PropertyChanged += Incp_PropertyChanged;

			if (!string.IsNullOrEmpty(item.Key.TargetEventName))
			{
				eventListener = new NativeViewEventListener(BindableNativeElement, item.Key.TargetEventName, item.Key.TargetPropertyName);
				eventListener.NativeViewEventFired += NativeViewEventFired;
			}

			SubscribeTwoWayNative(item);
		}

		void UnSubscribeTwoWay(KeyValuePair<BindableProxy, Binding> item)
		{
			var incp = BindableNativeElement as INotifyPropertyChanged;
			if (incp != null)
				incp.PropertyChanged -= Incp_PropertyChanged;

			if (eventListener != null)
			{
				eventListener.NativeViewEventFired -= NativeViewEventFired;
				eventListener.Dispose();
			}

			eventListener = null;

			UnSubscribeTwoWayNative(item);
		}

		void NativeViewEventFired(object sender, NativeViewEventFiredEventArgs e)
		{
			(this as INativeViewBindableController).OnNativePropertyChange(e.PropertyName, null);
		}

		void Incp_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			(this as INativeViewBindableController).OnNativePropertyChange(e.PropertyName);
		}

	}
}

