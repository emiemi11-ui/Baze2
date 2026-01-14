using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ECommerceAppPerfect.Converters
{
    // CLASA BOOLEANTOVISIBILITYCONVERTER - Converteste bool in Visibility
    //
    // CE ESTE UN VALUE CONVERTER?
    // In WPF Data Binding, uneori tipul din sursa nu corespunde cu cel din UI
    // Un Value Converter transforma valoarea in formatul potrivit
    //
    // DE CE AVEM NEVOIE?
    // In ViewModel avem: bool IsLoading
    // In XAML vrem: <ProgressBar Visibility="Visible/Hidden/Collapsed">
    // bool si Visibility sunt tipuri diferite!
    //
    // CUM FUNCTIONEAZA?
    // 1. Cream converter-ul si il declaram ca resursa in XAML
    // 2. Il folosim in binding cu Converter={StaticResource BoolToVis}
    // 3. WPF apeleaza Convert() cand sursa se schimba
    // 4. Rezultatul e folosit pentru UI
    //
    // EXEMPLU XAML:
    // <Window.Resources>
    //     <converters:BooleanToVisibilityConverter x:Key="BoolToVis" />
    // </Window.Resources>
    //
    // <ProgressBar Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />
    //
    // REZULTAT:
    // IsLoading = true  -> Visibility = Visible (se vede)
    // IsLoading = false -> Visibility = Collapsed (ascuns)
    //
    // DIFERENTA HIDDEN VS COLLAPSED:
    // Hidden: Elementul e invizibil DAR ocupa spatiu
    // Collapsed: Elementul e invizibil si NU ocupa spatiu
    // De obicei vrem Collapsed pentru a nu lasa spatii goale
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // METODA Convert - Transforma valoarea pentru UI
        //
        // PARAMETRI:
        // - value: Valoarea din sursa (bool)
        // - targetType: Tipul tinta (Visibility)
        // - parameter: Parametru optional din XAML
        // - culture: Cultura pentru formatare
        //
        // RETURNEAZA:
        // Visibility.Visible sau Visibility.Collapsed
        //
        // FOLOSIRE PARAMETRU:
        // Pentru a inversa logica, poti pasa parameter="Inverse"
        // <Element Visibility="{Binding IsHidden, Converter={StaticResource BoolToVis}, ConverterParameter=Inverse}" />
        // IsHidden = true  -> Collapsed (inversat!)
        // IsHidden = false -> Visible (inversat!)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificare tip - value trebuie sa fie bool
            if (value is not bool boolValue)
            {
                // Daca nu e bool, returnam Collapsed ca default
                return Visibility.Collapsed;
            }

            // Verificam daca trebuie sa inversam
            bool isInverse = parameter != null &&
                            parameter.ToString().ToLower() == "inverse";

            // Daca e inversat, negam valoarea
            if (isInverse)
            {
                boolValue = !boolValue;
            }

            // Returnam Visibility corespunzator
            // true -> Visible (se vede)
            // false -> Collapsed (ascuns)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        // METODA ConvertBack - Transforma valoarea inapoi la sursa
        //
        // CAND SE FOLOSESTE?
        // Pentru TwoWay binding, cand UI modifica valoarea
        // Exemplu: Un checkbox care modifica o proprietate bool
        //
        // PENTRU VISIBILITY:
        // De obicei nu avem nevoie de ConvertBack
        // (UI nu modifica Visibility direct)
        //
        // DAR il implementam pentru completitudine
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificare tip
            if (value is not Visibility visibility)
            {
                return false;
            }

            // Verificam daca e inversat
            bool isInverse = parameter != null &&
                            parameter.ToString().ToLower() == "inverse";

            // Convertim Visibility la bool
            bool result = visibility == Visibility.Visible;

            // Inversam daca e cazul
            if (isInverse)
            {
                result = !result;
            }

            return result;
        }
    }
}
