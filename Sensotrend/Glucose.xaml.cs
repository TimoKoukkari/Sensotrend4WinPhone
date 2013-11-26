using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Sensotrend
{
    public partial class Glucose : PhoneApplicationPage
    {
        public Glucose()
        {
            InitializeComponent();   
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double glucoseValue = 0;
            try {
                glucoseValue = Double.Parse(glucoseText.Text);
            } catch (System.ArgumentNullException ex) {
                MessageBox.Show("Cannot parse " + glucoseText.Text);
            } catch (System.FormatException ex) {
                MessageBox.Show("Cannot parse " + glucoseText.Text);

            } catch (System.OverflowException ex) {
                MessageBox.Show("Cannot parse " + glucoseText.Text);
            }

            // Send glucose value to Taltioni
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }


        private void glucoseText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (glucoseText.Text == "")
                okButton.IsEnabled = false;
            else
                okButton.IsEnabled = true;
        }
    }
}