using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Sensotrend
{
    public class PageContent : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime? dateValue;
        private string typeText;
        private string unitText;
        private string inputText;
        private string okButtonEnabled;
        private ICommand buttonCallbackOk;
        private ICommand buttonCallbackCancel;


        private void NotifyPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        public string DataTypeText
        {
            get { return typeText; }
            set{ typeText = value;}
        }
        public string DataUnitText
        {
            get { return unitText; }
            set { unitText = value;}
        }
        public string DataInputText
        {
            get { return inputText; }
            set { 
                inputText = value;
                NotifyPropertyChanged("DataInputText");
            }
        }
        public string OkButtonEnabled
        {
            get { return okButtonEnabled; }
            set { 
                okButtonEnabled = value;
                NotifyPropertyChanged("OkButtonEnabled"); 
            }
        }
        public ICommand ButtonCallbackOk
        {
            get { return buttonCallbackOk; }
            set { buttonCallbackOk = value; }
        }
        public ICommand ButtonCallbackCancel
        {
            get { return buttonCallbackCancel; }
            set { buttonCallbackCancel = value; }
        }
        public DateTime? DateValue
        {
            get { return dateValue; }
            set { dateValue = value; }
        }
    }
}
