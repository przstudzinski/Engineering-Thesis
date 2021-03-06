﻿using ApplicationToSupportAndControlDiet.Models;
using ApplicationToSupportAndControlDiet.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ApplicationToSupportAndControlDiet.Views
{
    public sealed partial class AddMeal : Page
    {
        private const int DEFAULT_QUANTITY = 100;
        private const string EMPTYMESSAGE = "Fill all the blank fields.";
        private const string CONFIRMMESSAGE = "Adding meal successful.";
        private const string VALUESMESSAGE = "{0} field value must be between {1} and {2}";

        private ObservableCollection<Product> items;
        private ObservableCollection<Ingridient> ingridients;

        private Repository<Product> productRepository;
        private ProductService productService;
        private Product selectedProduct;
        private Meal newMeal;

        private Boolean IsFailMessageSet;
        private Boolean IsSuccessMessageSet = false;
        private Style RedBorderStyle;
        private Style RedBorderStyleDate;
        private Style RedBorderStyleAutoSuggest;
        private Style DefaultStyle;

        public Nullable<DateTimeOffset> Date
        {
            get
            {
                return Globals.Date;
            }
            set
            {
                Globals.Date = value;
            }             
        }

        public AddMeal()
        {
            this.InitializeComponent();

            productService = new ProductService();
            productRepository = new Repository<Product>();
            items = new ObservableCollection<Product>();
            ingridients = new ObservableCollection<Ingridient>();
            this.SuggestProductsBox.ItemsSource = items;
            this.ItemsList.ItemsSource = ingridients;
            RedBorderStyle = Application.Current.Resources["TextBoxError"] as Style;
            RedBorderStyleDate = Application.Current.Resources["CalendarError"] as Style;
            RedBorderStyleAutoSuggest = Application.Current.Resources["AutoSuggestError"] as Style;
            DefaultStyle = null;
        }

        private void CalculateValuesFromAllChoosenProducts()
        {
            Meal abstractFutureMeal = new Meal();
            abstractFutureMeal.IngridientsInMeal.Clear();
            List<Ingridient> nowProductList = new List<Ingridient>(ingridients);
            abstractFutureMeal.IngridientsInMeal = nowProductList;
            String totalValues = "Total in meal: kcal =  " +  abstractFutureMeal.Energy.ToString("N1") + "  protein =  " + abstractFutureMeal.Protein.ToString("N1") 
                + "  carbohydrate =  " + abstractFutureMeal.Carbohydrate.ToString("N1") + "  fat =  " + abstractFutureMeal.Fat.ToString("N1") 
                + "  fiber =  " + abstractFutureMeal.Fiber.ToString("N1") + "  sugar =  " + abstractFutureMeal.Sugar.ToString("N1");
            this.TotalRunText.Text = totalValues;
        }

        private void SuggestProductsSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            selectedProduct = args.SelectedItem as Product;         
            this.MeasureBox.ItemsSource = InitializeMeasureComboBox(selectedProduct);
            this.MeasureBox.SelectedItem = Models.Measure.Gram;
        }

        private List<Measure> InitializeMeasureComboBox(Product product)
        {
            List<Measure> measures = new List<Measure>();
            measures.Add(Models.Measure.Gram);
            if (product != null)
            {
                if (product.WeightInTeaspoon != 0)
                {
                    measures.Add(Models.Measure.Teaspoon);
                    measures.Add(Models.Measure.Spoon);
                    measures.Add(Models.Measure.Glass);
                }
            }
            return measures;
        }

        private void SuggestProductsTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ClearConfirmValidationAndStyles();
            items.Clear();
            List<Product> result = productService.GetProductsLike(sender.Text);
            result.ForEach(x => items.Add(x));
        }

        private void AddIngridientClick(object sender, RoutedEventArgs e)
        {
            ClearTextBoxesStylesAndMessages();
            if (!ValidateEmptyIngridient()) return;
            float quantity = DEFAULT_QUANTITY;
            if (ValidateAndCheckInRange(QuantityBox, 0, 1000) && this.MeasureBox.SelectedItem != null)
            {
                quantity = Convert.ToSingle(this.QuantityBox.Text);
                Measure measure;
                Enum.TryParse<Measure>(this.MeasureBox.SelectedItem.ToString(), out measure);

                Ingridient ingridient = new Ingridient(selectedProduct, quantity, measure);

                ingridients.Add(ingridient);
                this.QuantityBox.Text = String.Empty;
                this.SuggestProductsBox.Text = String.Empty;
                this.MeasureBox.ItemsSource = null;
                this.selectedProduct = null;
                CalculateValuesFromAllChoosenProducts();
            }
        }

        private void DeleteProductClick(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var productToDelete = baseObject.DataContext as Ingridient;
            ingridients.Remove(productToDelete);
            CalculateValuesFromAllChoosenProducts();
        }

        private void FavouriteProductClick(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var selectedProduct = baseObject.DataContext as Ingridient;
            if (selectedProduct.Product.DisLike == true) return;
            selectedProduct.Product.Favourite = true;
            RefreshListView();
            productRepository.Update(selectedProduct.Product);
            
        }

        private void UnFavoriteProductClick(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var selectedProduct = baseObject.DataContext as Ingridient;
            selectedProduct.Product.Favourite = false;
            productRepository.Update(selectedProduct.Product);
            RefreshListView();           
        }

        private void DislikeProductClick(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var selectedProduct = baseObject.DataContext as Ingridient;
            if (selectedProduct.Product.Favourite == true) return;
            selectedProduct.Product.DisLike = true;
            productRepository.Update(selectedProduct.Product);
            RefreshListView();
        }

        private void LikeProductClick(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var selectedProduct = baseObject.DataContext as Ingridient;
            selectedProduct.Product.DisLike = false;
            productRepository.Update(selectedProduct.Product);
            RefreshListView();
        }

        private void RefreshListView()
        {
            this.ItemsList.ItemsSource = null;
            this.ItemsList.ItemsSource = ingridients;
        }

        private void SaveMealClick(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => SaveMealAsync());
            task.Start();
        }

        private async void SaveMealAsync()
        {
            Repository<Day> repository = new Repository<Day>();
            DayService dayService = new DayService();
            bool newItem = false;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    ValidateBeforeSaveMeal();
                    DateTime dateTime = GetDateTimeFromUi();
                    newMeal.DateTimeOfMeal = dateTime;
                    Day day = dayService.FindDayByDate(dateTime);
                    if (day == null)
                    {
                        day = new Day();
                        day.Date = dateTime;
                        newItem = true;
                    }
                    day.MealsInDay.Add(newMeal);
                    newMeal.Day = day;
                    newMeal.DayId = day.DayId;

                    if (IsFailMessageSet) return;
                    if (newItem == true)
                    {
                        if (repository.Save(day) > -1)
                        {
                            ClearTextBoxesAndSetConfirmMessage();
                        }
                    }
                    else
                    {
                        if (repository.Update(day) > -1)
                        {
                            ClearTextBoxesAndSetConfirmMessage();
                        }
                    }
                    newMeal = null;
                });
        }

        private DateTime GetDateTimeFromUi()
        {
            TimeSpan time = this.TimePicker.Time;
            DateTimeOffset date = this.DataPicker.Date.Value;
            DateTime dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds);
            return dateTime;
        }

        private Meal CreateMeal()
        {
            newMeal = new Meal();
            return newMeal;
        }

        private void ValidateBeforeSaveMeal()
        {
            ClearTextBoxesStylesAndMessages();
            bool newItem = false;
            if (newMeal == null)
            {
                newMeal = CreateMeal();
                newItem = true;
            }
            if (ValidateEmpty(NameBox))
            {
                newMeal.MealName = NameBox.Text;
            }
            if (ValidateChoosenProducts())
            {
                newMeal.IngridientsInMeal.Clear();
                foreach (Ingridient element in ingridients)
                {
                    newMeal.IngridientsInMeal.Add(element);
                    if (newItem == true)
                    {
                        element.IngridientId = 0;
                        element.Meal = newMeal;
                        element.MealId = newMeal.MealId;
                    }
                }
            }
        }

        private Boolean ValidateEmpty(TextBox textBox)
        {
            if (textBox.Text.Length == 0)
            {
                if (!IsFailMessageSet)
                {
                    IsFailMessageSet = true;
                    AppendToMessages(EMPTYMESSAGE);
                }
                textBox.Style = RedBorderStyle;
                return false;
            }
            else
            {
                return true;
            }
        }

        private Boolean ValidateEmptyIngridient() {
            if (selectedProduct == null) return false;
            return true;
        }

        private bool ValidateChoosenProducts()
        {
            if (ingridients.Count == 0)
            {
                if (!IsFailMessageSet)
                {
                    IsFailMessageSet = true;
                    AppendToMessages(EMPTYMESSAGE);
                }
                SuggestProductsBox.Style = RedBorderStyleAutoSuggest;
                return false;
            }
            else
            {
                return true;
            }
        }

        private void TextBoxNumericTextChanged(object sender, TextChangedEventArgs e)
        {
            ClearConfirmValidationAndStyles();
            TextBox textBox = sender as TextBox;
            Int32 selectionStart = textBox.SelectionStart;
            Int32 selectionLength = textBox.SelectionLength;
            String newText = String.Empty;
            int count = 0;
            foreach (Char c in textBox.Text.ToCharArray())
            {
                if (Char.IsDigit(c) || Char.IsControl(c) || (c == '.' && count == 0))
                {
                    newText += c;
                    if (c == '.')
                        count += 1;
                }
            }
            textBox.Text = newText;
            textBox.SelectionStart = selectionStart <= textBox.Text.Length ? selectionStart : textBox.Text.Length;
        }

        private void ClearTextBoxesStylesAndMessages()
        {
            ClearStyles();
            IsFailMessageSet = false;
            AddConfirm.Text = String.Empty;
            ValidationMessages.Text = String.Empty;
        }

        private async void ClearTextBoxesAndSetConfirmMessage()
        {
            ClearText();
            ClearList();
            ClearStyles();
            IsSuccessMessageSet = true;
            AddConfirm.Text = CONFIRMMESSAGE;
            await Task.Delay(500);
            IsSuccessMessageSet = false;
        }

        private void ClearTextBoxesAndStyles()
        {
            ClearText();
            ClearList();
            ClearStyles();
            IsFailMessageSet = false;
        }

        private void ClearStyles()
        {
            NameBox.Style = DefaultStyle;
            QuantityBox.Style = DefaultStyle;
            SuggestProductsBox.Style = DefaultStyle;
        }

        private void ClearConfirmValidationAndStyles() {
            if (!IsSuccessMessageSet)
            {
                AddConfirm.Text = String.Empty;
            }
            ValidationMessages.Text = String.Empty;
            ClearStyles();
        }

        private void ClearText()
        {
            AddConfirm.Text = String.Empty;
            ValidationMessages.Text = String.Empty;
            NameBox.Text = String.Empty;
            this.QuantityBox.Text = String.Empty;
            this.SuggestProductsBox.Text = String.Empty;
            this.TotalRunText.Text = String.Empty;
        }

        private void ClearList()
        {
            ingridients = new ObservableCollection<Ingridient>();
            this.ItemsList.ItemsSource = ingridients;
        }

        private void ClearMealClick(object sender, RoutedEventArgs e)
        {
            ClearTextBoxesAndStyles();
        }

        private void AppendToMessages(string message)
        {
            ValidationMessages.Text += (message + Environment.NewLine);
        }

        private Boolean ValidateAndCheckInRange(TextBox textBox, float min, float max)
        {
            if (textBox.Text.Length == 0)
            {
                if (!IsFailMessageSet)
                {
                    IsFailMessageSet = true;
                    AppendToMessages(EMPTYMESSAGE);
                }
                textBox.Style = RedBorderStyle;
                return false;
            }
            float value = float.Parse(textBox.Text);
            if (value >= min && value <= max)
            {
                return true;
            }
            else
            {
                textBox.Style = RedBorderStyle;
                AppendToMessages(String.Format(VALUESMESSAGE, textBox.Tag, min, max));
                if (!IsFailMessageSet)
                {
                    IsFailMessageSet = true;
                }
                return false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                KeyValuePair<bool, Meal> parameters = (KeyValuePair<bool, Meal>)e.Parameter;
                bool itsNewMeal = parameters.Key;
                Meal mealFromMealsPage = parameters.Value;
                this.ingridients.Clear();
                foreach(Ingridient product in mealFromMealsPage.IngridientsInMeal)
                {
                    this.ingridients.Add(product);
                }
                this.NameBox.Text = mealFromMealsPage.MealName;
                TimeSpan time = new TimeSpan(mealFromMealsPage.DateTimeOfMeal.ToLocalTime().Hour, mealFromMealsPage.DateTimeOfMeal.ToLocalTime().Minute,
                    mealFromMealsPage.DateTimeOfMeal.ToLocalTime().Second);
                this.TimePicker.Time = time;
                DateTimeOffset date = new DateTimeOffset(mealFromMealsPage.DateTimeOfMeal.Date);
                this.DataPicker.Date = date;
                if (itsNewMeal == false)
                {
                    newMeal = mealFromMealsPage;
                }
                CalculateValuesFromAllChoosenProducts();
            }
        }

        private void NameBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            ClearConfirmValidationAndStyles();
        }

        private async void MeasureComboBoxInListLoaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            var baseObject = sender as FrameworkElement;
            var product = baseObject.DataContext as Ingridient;
            if (product != null)
            {
                ComboBox comboBoxInList = baseObject as ComboBox;
                comboBoxInList.ItemsSource = InitializeMeasureComboBox(product.Product);
                comboBoxInList.SelectedItem = product.MeasureOfQuantity;
            }         
        }

        private void MeasureComboBoxInListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var product = baseObject.DataContext as Ingridient;
            ComboBox comboBoxInList = baseObject as ComboBox;
            Measure measure;
            if (comboBoxInList.SelectedItem != null)
            {
                Enum.TryParse<Measure>(comboBoxInList.SelectedItem.ToString(), out measure);
                if (measure != product.MeasureOfQuantity)
                {
                    product.MeasureOfQuantity = measure;
                    product.Update();     
                    CalculateValuesFromAllChoosenProducts();
                    RefreshListView();
                }
            }
        }

        private void QuantityTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var baseObject = sender as FrameworkElement;
            var product = baseObject.DataContext as Ingridient;
            TextBox quantityTextBoxInList = baseObject as TextBox;
            if (ValidateAndCheckInRange(quantityTextBoxInList, 0, 1000))
            {
                float quantity = Convert.ToSingle(quantityTextBoxInList.Text);
                if (quantity != product.QuantityInMeal)
                {
                    product.QuantityInMeal = quantity;
                    product.Update();
                    CalculateValuesFromAllChoosenProducts();
                    RefreshListView();
                }
            }
        }

    }
}