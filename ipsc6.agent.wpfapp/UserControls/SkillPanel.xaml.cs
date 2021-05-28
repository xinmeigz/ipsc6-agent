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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace ipsc6.agent.wpfapp.UserControls
{
    /// <summary>
    /// SkillPanel.xaml 的交互逻辑
    /// </summary>
    public partial class SkillPanel : UserControl
    {
        public SkillPanel()
        {
            InitializeComponent();
            DataContext = ViewModels.MainViewModel.Instance;
        }

        private void ShowOrHideSkillPopup(object sender, RoutedEventArgs e)
        {
            var popup = FindName("SkillPopup") as Popup;
            popup.IsOpen = !popup.IsOpen;
        }
    }
}