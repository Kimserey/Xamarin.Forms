using System;
namespace Xamarin.Forms
{
	interface INativeViewBindableController
	{
		void ApplyNativeBindings();
		void OnNativePropertyChange(string property, object newValue = null);
		void UnApplyNativeBindings();

	}
}

