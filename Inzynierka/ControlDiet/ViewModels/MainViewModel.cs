﻿
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationToSupportAndControlDiet.Models;

namespace ApplicationToSupportAndControlDiet.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            WelcomeMessage = "Hi, it's main page";
            DatabaseConnection.ConnectToSqliteDatabase();  
        }
        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get
            {
                return _welcomeMessage;
            }
            set
            {
                Set(ref _welcomeMessage, value);
            }
        }
    }
}
