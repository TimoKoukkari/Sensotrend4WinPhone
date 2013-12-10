using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Input;
using System.ComponentModel;

namespace Sensotrend
{
    public partial class Glucose : PhoneApplicationPage
    {
        private PageContent pageContent;

        public Glucose()
        {
            InitializeComponent();

            ICommand buttonCommandOk = new DelegateCommand(ButtonClickOk);
            ICommand buttonCommandCancel = new DelegateCommand(ButtonClickCancel);

            pageContent = new PageContent
            {
                DateValue = DateTime.Now,
                DataInputText = "",
                DataTypeText = "Glucose",
                DataUnitText = "mmol/l",
                OkButtonEnabled = "False",
                ButtonCallbackOk = buttonCommandOk,
                ButtonCallbackCancel = buttonCommandCancel
            };

            pageContent.PropertyChanged += new PropertyChangedEventHandler(pageContent_PropertyChanged);

            DataContext = pageContent;
        }

       void pageContent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DataInputText":
                    DataInputTextChanged(e);
                    break;
            }
        }


        public void ButtonClickOk(object parameter)
        {
            double glucoseValue = 0;
            try
            {
                glucoseValue = Double.Parse(pageContent.DataInputText);
            }
            catch (System.ArgumentNullException ex)
            {
                MessageBox.Show("Cannot parse " + pageContent.DataInputText);
            }
            catch (System.FormatException ex)
            {
                MessageBox.Show("Cannot parse " + pageContent.DataInputText);

            }
            catch (System.OverflowException ex)
            {
                MessageBox.Show("Cannot parse " + pageContent.DataInputText);
            }
            DateTime date = (DateTime) pageContent.DateValue;          
            App.SendToTaltioni(date, App.TaltioniDataType.BLOOD_GLUCOSE, (Type)pageContent.DataInputText);
            pageContent.DataInputText = "";
        }

        public void ButtonClickCancel(object parameter)
        {
            NavigationService.GoBack();
        }

        private void DataInputTextChanged(PropertyChangedEventArgs e)
        {
            if (pageContent.DataInputText == "")
            {
                pageContent.OkButtonEnabled = "False";
            }
            else
            {
                pageContent.OkButtonEnabled = "True";
            }
        }    
    }
}