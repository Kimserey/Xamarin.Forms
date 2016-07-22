using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Xamarin.Forms
{
	internal class BindableProxy : BindableObject
	{
		public BindableProperty Property;

		public Type TargetPropertyType => propInfo?.PropertyType;
		public Type TargetObjectType => targetObject?.GetType();

		public string TargetPropertyName => targetProperty;
		public string TargetEventName => targetEvent;

		public List<Type> ParameterTypes => parameterPossibleTypes;

		public BindableProxy(object target, PropertyInfo targetPropInfo, string eventName = null)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (targetPropInfo == null)
				throw new ArgumentException("targetProperty should not be null or empty", nameof(targetPropInfo));
			targetProperty = targetPropInfo.Name;
			targetObject = target;
			targetEvent = eventName;

			propInfo = targetPropInfo;

			Init();
		}

		public BindableProxy(object target, string targetProp, Action<object, object> setValue = null, Func<object> getValue = null)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (string.IsNullOrEmpty(targetProp))
				throw new ArgumentException("targetProperty should not be null or empty", nameof(targetProp));
			targetProperty = targetProp;
			targetObject = target;
			callbackSetValue = setValue;
			callbackGetValue = getValue;

			propInfo = TargetObjectType.GetProperty(targetProperty);

			Init();
		}

		public void OnTargetPropertyChanged(object valueFromNative = null)
		{
			object convertedValue = null;
			//this comes converted, or not.. 
			var currentValue = GetValue(Property);

			var nativeValue = GetTargetValue();

			if (valueFromNative == null)
				valueFromNative = nativeValue;

			if (nativeValueConverter != null)
				convertedValue = nativeValueConverter.ConvertBack(valueFromNative, TargetPropertyType, null, CultureInfo.CurrentUICulture);

			var finalValue = convertedValue ?? valueFromNative;
			if (finalValue.Equals(currentValue))
				return;

			SetValueCore(Property, finalValue);
		}

		readonly object targetObject;
		readonly string targetProperty;
		readonly string targetEvent;
		readonly PropertyInfo propInfo;
		List<MethodInfo> setMethodsInfo;
		List<MethodInfo> getMethodsInfo;
		List<Type> parameterPossibleTypes;
		IValueConverter nativeValueConverter;
		Action<object, object> callbackSetValue;
		Func<object> callbackGetValue;

		void Init()
		{
			if (propInfo == null)
				FindPossibleMethods(TargetPropertyName, TargetObjectType, out getMethodsInfo, out setMethodsInfo, out parameterPossibleTypes);

			Property = BindableProperty.Create(TargetPropertyName, typeof(object), typeof(BindableProxy), propertyChanged: (bo, o, n) => ((BindableProxy)bo).OnPropertyChanged(o, n));

			var converter = Registrar.Registered.GetHandler(TargetPropertyType);
			if (converter != null)
				nativeValueConverter = converter as IValueConverter;
			else
				Log.Warning("NativeBinding", $"Converter not found for {TargetPropertyType}");
		}

		static void FindPossibleMethods(string targetProp, Type targetObjectType, out List<MethodInfo> gets, out List<MethodInfo> sets, out List<Type> parameterTypes)
		{
			gets = new List<MethodInfo>();
			sets = new List<MethodInfo>();
			parameterTypes = new List<Type>();
			var setMethodName = $"Set{targetProp}";
			var getMethodName = $"Get{targetProp}";

			foreach (var method in targetObjectType.GetRuntimeMethods())
			{
				System.Diagnostics.Debug.WriteLine(method);
				if (method.DeclaringType != targetObjectType)
					continue;

				if (method.Name == setMethodName)
				{
					sets.Add(method);
					foreach (var parameter in method.GetParameters())
					{
						parameterTypes.Add(parameter.ParameterType);
					}
				}

				if (method.Name == getMethodName)
				{
					gets.Add(method);
					parameterTypes.Add(method.ReturnType);
				}

			}
		}

		void OnPropertyChanged(object oldValue, object newValue)
		{
			if (callbackSetValue != null)
				callbackSetValue(oldValue, newValue);
			else
				SetTargetValue(newValue);
		}

		void SetTargetValue(object value)
		{
			if (value == null)
				return;

			bool wasSet = TrySetValueOnTarget(value);

			if (!wasSet)
				throw new InvalidCastException($"Can't bind properties of different types. The target property is {TargetPropertyType}, and the value is {value.GetType()}");
		}

		bool TrySetValueOnTarget(object value)
		{
			bool wasSet = false;
			object convertedValue = null;

			if (nativeValueConverter != null)
				convertedValue = nativeValueConverter.Convert(value, TargetPropertyType, null, CultureInfo.CurrentUICulture);

			if (propInfo != null)
				wasSet = SetPropertyInfo(convertedValue ?? value);

			if (setMethodsInfo != null && !wasSet)
				wasSet = SetSetMethodInfo(convertedValue ?? value);

			return wasSet;
		}

		object GetTargetValue()
		{
			if (callbackGetValue != null)
				return callbackGetValue();

			if (propInfo != null)
				return ReadPropertyInfo();

			if (getMethodsInfo != null)
				return ReadGetMethodInfo();

			return null;
		}

		bool SetSetMethodInfo(object value)
		{
			bool wasSet = false;

			foreach (var setMethod in setMethodsInfo)
			{
				try
				{
					setMethod.Invoke(targetObject, new object[] { value });
					wasSet = true;
					break;
				}
				catch (ArgumentException)
				{
					System.Diagnostics.Debug.WriteLine("Failed to convert");
				}
			}

			return wasSet;
		}

		object ReadGetMethodInfo()
		{
			foreach (var getMethod in getMethodsInfo)
			{
				try
				{
					var possibleValue = getMethod.Invoke(targetObject, new object[] { });
					if (possibleValue != null)
						return possibleValue;
					break;
				}
				catch (Exception ex)
				{
					throw (ex);
				}
			}
			return null;
		}

		object ReadPropertyInfo()
		{
			if (!propInfo.CanRead)
			{
				System.Diagnostics.Debug.WriteLine($"No GetMethod found for {TargetPropertyName}");
				return null;
			}

			var obj = propInfo.GetValue(targetObject);
			return obj;
		}

		bool SetPropertyInfo(object value)
		{
			if (!TargetPropertyType.IsAssignableFrom(value.GetType()))
				return false;

			if (!propInfo.CanWrite)
			{
				System.Diagnostics.Debug.WriteLine($"No SetMethod found for {TargetPropertyName}");
				return false;
			}

			propInfo.SetValue(targetObject, value);
			return true;
		}
	}
}

