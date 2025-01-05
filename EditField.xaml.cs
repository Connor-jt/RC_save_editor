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

namespace RC_save_editor
{
    /// <summary>
    /// Interaction logic for EditField.xaml
    /// </summary>
    public partial class EditField : UserControl
    {
        string? context = null;
        MainWindow? callback = null;
        bool isfloat = false;
        public EditField(string? context, string? current_value, MainWindow? callback, bool isfloat){
            InitializeComponent();
            
            this.callback = callback;
            this.context = context;
            this.isfloat = isfloat;

            if (context != null)
                context_box.Text = context;

            if (current_value != null)
                value_box.Text = current_value;
        }
        
        private void value_box_TextChanged(object sender, TextChangedEventArgs e) {
            if (callback == null || context == null) { throw new Exception("bad edit field box??"); }

            if (value_box.Text == "" 
            || (!isfloat && !int.TryParse(value_box.Text, out _)) 
            || ( isfloat && !float.TryParse(value_box.Text, out _))){
                value_box_validator.BorderBrush = Brushes.Red;
                return;
            }


            value_box_validator.BorderBrush = Brushes.Transparent;
            callback.field_edited(context, value_box.Text);
        }
    }
}
