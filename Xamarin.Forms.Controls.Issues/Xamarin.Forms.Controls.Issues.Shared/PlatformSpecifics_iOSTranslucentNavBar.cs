using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Controls
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 60000, "Platform Specifics - iOS Translucent Navigation Bar")]
	public class PlatformSpecifics_iOSTranslucentNavBar : TestNavigationPage
	{
		protected override void Init()
		{
			BackgroundColor = Color.Pink;

			var button = new Button { Text = "Toggle Translucent", BackgroundColor = Color.Yellow };

			button.Clicked += (sender, args) => OniOS().IsNavigationBarTranslucent = !OniOS().IsNavigationBarTranslucent;

			var content = new ContentPage
			{
				Title = "iOS Translucent Navigation Bar",
				Content = new StackLayout
				{
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center,
					Children = { button }
				}
			};

			PushAsync(content);
		}
	}
}