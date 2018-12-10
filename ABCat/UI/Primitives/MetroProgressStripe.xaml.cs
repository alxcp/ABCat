using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace ABCat.UI.WPF.UI.Primitives
{
    /// <summary>
    ///     Interaction logic for MetroProgressStrip.xaml
    /// </summary>
    public partial class MetroProgressStripe
    {
        public MetroProgressStripe()
        {
            InitializeComponent();

            IsVisibleChanged += MetroProgressStripeIsVisibleChanged;
        }

        private void MetroProgressStripeIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                switch (Visibility)
                {
                    case Visibility.Visible:
                        ((Storyboard) ProgressBar.Resources["AnimationStoryBoard"]).Begin();
                        break;
                    case Visibility.Hidden:
                    case Visibility.Collapsed:
                        ((Storyboard) ProgressBar.Resources["AnimationStoryBoard"]).Stop();
                        break;
                }
            }
        }
    }
}