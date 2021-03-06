using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ipsc6.agent.wpfapp.Views
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();

            DataContext = ViewModels.ConfigViewModel.Instance;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.ConfigViewModel;
            viewModel.Load(sender);
        }

        private void IpscServerAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BindingOperations.GetBindingExpression(sender as DependencyObject, TextBox.TextProperty).UpdateSource();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
