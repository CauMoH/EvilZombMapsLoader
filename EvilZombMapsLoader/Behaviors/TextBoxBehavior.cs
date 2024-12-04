using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace EvilZombMapsLoader.Behaviors
{
    public class TextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        { 
            AssociatedObject.MouseDoubleClick += AssociatedObject_OnMouseDown;
        }
        
        protected override void OnDetaching()
        {
            AssociatedObject.MouseDoubleClick -= AssociatedObject_OnMouseDown;
        }

        private void AssociatedObject_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            AssociatedObject.SelectAll();
        }
    }
}
