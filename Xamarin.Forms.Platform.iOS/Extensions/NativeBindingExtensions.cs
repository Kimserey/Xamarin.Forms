using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using System.Linq.Expressions;
using System.Reflection;
#if __UNIFIED__
using UIKit;

#else
using MonoTouch.UIKit;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this UIView self, Expression<Func<object>> memberLamda, Binding binding)
		{
			SetBinding(self, memberLamda, binding, null);
		}

		//this works better but maybe is slower
		public static void SetBinding(this UIView self, Expression<Func<object>> memberLamda, Binding binding, string eventName)
		{
			MemberExpression memberSelectorExpression = null;
			memberSelectorExpression = memberLamda.Body as MemberExpression;
			if (memberSelectorExpression == null)
			{
				var unaryExpression = memberLamda.Body as UnaryExpression;
				if (unaryExpression != null)
				{
					memberSelectorExpression = unaryExpression.Operand as MemberExpression;
				}
			}
			if (memberSelectorExpression == null)
				throw new ArgumentNullException(nameof(memberLamda));
			var property = memberSelectorExpression.Member as PropertyInfo;
			var proxy = new BindableProxy(self, property, eventName);
			SetBinding(self, binding, proxy);
		}

		public static void SetBinding(this UIView self, string propertyName, Binding binding, Action<object, object> callback = null, Func<object> getter = null)
		{
			var proxy = new BindableProxy(self, propertyName, callback, getter);
			SetBinding(self, binding, proxy);
		}

		static void SetBinding(UIView view, Binding binding, BindableProxy bindableProxy)
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

