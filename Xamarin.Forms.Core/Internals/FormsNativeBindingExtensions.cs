using System;
using System.Collections.Generic;

namespace Xamarin.Forms
{
	internal static class FormsNativeBindingExtensions
	{
		internal static Dictionary<object, Dictionary<BindableProxy, Binding>> NativeBindingPool = new Dictionary<object, Dictionary<BindableProxy, Binding>>();
	}
}

