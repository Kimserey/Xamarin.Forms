using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	public class View : VisualElement, IViewController
	{
		public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create("VerticalOptions", typeof(LayoutOptions), typeof(View), LayoutOptions.Fill,
			propertyChanged: (bindable, oldvalue, newvalue) => ((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.VerticalOptionsChanged));

		public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create("HorizontalOptions", typeof(LayoutOptions), typeof(View), LayoutOptions.Fill,
			propertyChanged: (bindable, oldvalue, newvalue) => ((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.HorizontalOptionsChanged));

		public static readonly BindableProperty MarginProperty = BindableProperty.Create("Margin", typeof(Thickness), typeof(View), default(Thickness), propertyChanged: MarginPropertyChanged);

		readonly ObservableCollection<IGestureRecognizer> _gestureRecognizers = new ObservableCollection<IGestureRecognizer>();

		protected internal View()
		{
			_gestureRecognizers.CollectionChanged += (sender, args) =>
			{
				switch (args.Action)
				{
					case NotifyCollectionChangedAction.Add:
						foreach (IElement item in args.NewItems.OfType<IElement>())
						{
							ValidateGesture(item as IGestureRecognizer);
							item.Parent = this;
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						foreach (IElement item in args.OldItems.OfType<IElement>())
							item.Parent = null;
						break;
					case NotifyCollectionChangedAction.Replace:
						foreach (IElement item in args.NewItems.OfType<IElement>())
						{
							ValidateGesture(item as IGestureRecognizer);
							item.Parent = this;
						}
						foreach (IElement item in args.OldItems.OfType<IElement>())
							item.Parent = null;
						break;
					case NotifyCollectionChangedAction.Reset:
						foreach (IElement item in _gestureRecognizers.OfType<IElement>())
							item.Parent = this;
						break;
				}
			};
		}

		public IList<IGestureRecognizer> GestureRecognizers
		{
			get { return _gestureRecognizers; }
		}

		public LayoutOptions HorizontalOptions
		{
			get { return (LayoutOptions)GetValue(HorizontalOptionsProperty); }
			set { SetValue(HorizontalOptionsProperty, value); }
		}

		public Thickness Margin
		{
			get { return (Thickness)GetValue(MarginProperty); }
			set { SetValue(MarginProperty, value); }
		}

		public LayoutOptions VerticalOptions
		{
			get { return (LayoutOptions)GetValue(VerticalOptionsProperty); }
			set { SetValue(VerticalOptionsProperty, value); }
		}

		protected override void OnBindingContextChanged()
		{
			var gotBindingContext = false;
			object bc = null;

			for (var i = 0; i < GestureRecognizers.Count; i++)
			{
				var bo = GestureRecognizers[i] as BindableObject;
				if (bo == null)
					continue;

				if (!gotBindingContext)
				{
					bc = BindingContext;
					gotBindingContext = true;
				}

				SetInheritedBindingContext(bo, bc);
			}

			base.OnBindingContextChanged();
		}

		static void MarginPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((View)bindable).InvalidateMeasureInternal(InvalidationTrigger.MarginChanged);
		}

		void ValidateGesture(IGestureRecognizer gesture)
		{
			if (gesture == null)
				return;
			if (gesture is PinchGestureRecognizer && _gestureRecognizers.GetGesturesFor<PinchGestureRecognizer>().Count() > 1)
				throw new InvalidOperationException($"Only one {nameof(PinchGestureRecognizer)} per view is allowed");
		}
	}

	public static class FormsNativeBindingExtensions
	{
		internal static Dictionary<object, Dictionary<BindableProxy, Binding>> NativeBindingPool = new Dictionary<object, Dictionary<BindableProxy, Binding>>();

	}

	public abstract class BindableNativeView : View, INativeViewBindableController
	{
		public BindableNativeView()
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

			foreach (var item in bindableProxies) {
				item.Key.SetBinding(item.Key.Property, item.Value);
				item.Key.BindingContext = BindingContext;

				if (item.Value.Mode == BindingMode.TwoWay)
					SubscribeTwoWay(item);
			}
		}

		void INativeViewBindableController.OnNativePropertyChange(string property, object newValue)
		{
			foreach (var item in bindableProxies) {
				if (item.Key.TargetPropertyName == property.ToString()) {
					item.Key.OnTargetPropertyChanged(newValue, item.Value.Converter);
				}
			}
		}

		void INativeViewBindableController.UnApplyNativeBindings()
		{
			foreach (var item in bindableProxies) {
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
		
			if (!string.IsNullOrEmpty(item.Key.TargetEventName)) {
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
			
			if (eventListener != null) {
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