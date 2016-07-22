using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;


#if WINDOWS_UWP

namespace Xamarin.Forms.Platform.UWP
#else

namespace Xamarin.Forms.Platform.WinRT
#endif
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this FrameworkElement self, Expression<Func<object>> memberLamda, Binding binding)
		{
			SetBinding(self, memberLamda, binding, null);
		}

		//this works better but maybe is slower
		public static void SetBinding(this FrameworkElement self, Expression<Func<object>> memberLamda, Binding binding, string eventName)
		{
			var memberSelectorExpression = memberLamda.Body as MemberExpression;
			if (memberSelectorExpression != null)
			{
				var property = memberSelectorExpression.Member as PropertyInfo;
				var proxy = new BindableProxy(self, property, eventName);
				SetBinding(self, binding, proxy);
			}
		}

		public static void SetBinding(this FrameworkElement self, string propertyName, Binding binding, Action<object, object> callback = null, Func<object> getter = null)
		{
			var proxy = new BindableProxy(self, propertyName, callback, getter);
			SetBinding(self, binding, proxy);
		}

		static void SetBinding(FrameworkElement view, Binding binding, BindableProxy bindableProxy)
		{
			if (FormsNativeBindingExtensions.NativeBindingPool.ContainsKey(view))
			{
				FormsNativeBindingExtensions.NativeBindingPool[view].Add(bindableProxy, binding);
			}
			else
			{
				FormsNativeBindingExtensions.NativeBindingPool.Add(view, new Dictionary<BindableProxy, Binding> { { bindableProxy, binding } });
			}
		}
	}
}
