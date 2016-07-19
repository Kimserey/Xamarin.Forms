﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Android.Widget;
using AView = Android.Views.View;

namespace Xamarin.Forms.Platform.Android
{
	public static class NativeBindingExtensions
	{
		internal static Dictionary<AView, Dictionary<BindableProxy, Binding>> NativeBindingPool = new Dictionary<AView, Dictionary<BindableProxy, Binding>>();

		public static void SetBinding(this AView self, Expression<Func<object>> memberLamda, Binding binding)
		{
			SetBinding(self, memberLamda, binding, null);
		}

		//this works better but maybe is slower
		public static void SetBinding(this AView self, Expression<Func<object>> memberLamda, Binding binding, string eventName)
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

		public static void SetBinding(this AView self, string propertyName, Binding binding, Action<object, object> callback = null, Func<object> getter = null)
		{
			//var methodGetName = $"Get{propertyName}";

			var proxy = new BindableProxy(self, propertyName, callback, getter);
			SetBinding(self, binding, proxy);
		}

		static void SetBinding(AView view, Binding binding, BindableProxy bindableProxy)
		{
			FindConverter(binding, bindableProxy);

			if (NativeBindingPool.ContainsKey(view))
			{
				NativeBindingPool[view].Add(bindableProxy, binding);
			}
			else
			{
				NativeBindingPool.Add(view, new Dictionary<BindableProxy, Binding> { { bindableProxy, binding } });
			}
		}

		static void FindConverter(Binding binding, BindableProxy proxy)
		{
			if (binding.Converter != null)
				return;

			var assembly = Assembly.GetExecutingAssembly();
			var converterClassName = string.Empty;
			//this needs to be done upfront and cached.
			if (proxy.TargetPropertyType != null)
			{
				converterClassName = $"{assembly.GetName().Name}.{proxy.TargetPropertyType.Name}Converter";
				var converter = assembly.CreateInstance(converterClassName) as IValueConverter;
				if (converter != null)
					binding.Converter = converter;
			}
			else
			{
				if (proxy.ParameterTypes != null)
				{
					foreach (var item in proxy.ParameterTypes)
					{
						if (binding.Converter != null)
							break;
						var shortName = item.Name.Split(new[] { '.' }).Last();
						converterClassName = $"{assembly.GetName().Name}.{shortName}Converter";
						var converter = assembly.CreateInstance(converterClassName) as IValueConverter;
						if (converter != null)
						{
							binding.Converter = converter;
						}

					}
				}
			}
		}
	}
}

