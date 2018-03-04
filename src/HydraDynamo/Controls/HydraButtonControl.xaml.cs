using System;
using System.Windows;
using System.Windows.Forms;
using Hydra.HydraHelperFunctions;


namespace Hydra
{
    public partial class HydraShareControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public HydraShareControl()
        {
            InitializeComponent();
            DataContext = new HydraNodeModel();
        }

        // Function that is triggered when 'Browse' Button is pressed
        private void SetDirectory(object sender, EventArgs e)
        {
            FolderBrowserDialog openFolderDialog = new FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == DialogResult.OK)
            {
                targetFolder.Text = openFolderDialog.SelectedPath;
                var hydraNodeModel = this.DataContext as HydraNodeModel;
                hydraNodeModel.TargetFolder = targetFolder.Text;
            }
        }

        // Function that is triggered when 'Collapse' Button is pressed
        private void Collapse(object sender, EventArgs e)
        {
            if(MainFields.Visibility == Visibility.Visible)
            {
                CollapseButtonText.Text = "Expand";
                LowerButtons.Orientation = System.Windows.Controls.Orientation.Vertical;
                MainFields.Visibility = Visibility.Collapsed;
            }

            else
            {
                CollapseButtonText.Text = "Collapse";
                LowerButtons.Orientation = System.Windows.Controls.Orientation.Horizontal;
                MainFields.Visibility = Visibility.Visible;
            }
        }
    }
}
