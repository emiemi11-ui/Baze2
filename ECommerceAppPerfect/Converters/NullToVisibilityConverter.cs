using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ECommerceAppPerfect.Converters
{
    // CLASA NULLTOVISIBILITYCONVERTER - Converteste null/non-null in Visibility
    //
    // CE FACE ACEST CONVERTER?
    // Transforma prezenta/absenta unei valori in vizibilitate:
    // - Valoare != null -> Visible
    // - Valoare == null -> Collapsed
    //
    // DE CE AVEM NEVOIE?
    // Adesea vrem sa afisam elemente DOAR daca exista date
    //
    // EXEMPLE:
    //
    // 1. Afiseaza sectiune doar daca e selectat un produs:
    // <Border Visibility="{Binding SelectedProduct, Converter={StaticResource NullToVis}}">
    //     <TextBlock Text="{Binding SelectedProduct.ProductName}" />
    // </Border>
    //
    // 2. Afiseaza mesaj "No data" cand lista e goala:
    // <TextBlock Text="No products found"
    //            Visibility="{Binding Products, Converter={StaticResource NullToVis}, ConverterParameter=Inverse}" />
    //
    // 3. Afiseaza imagine doar daca exista URL:
    // <Image Source="{Binding ImageURL}"
    //        Visibility="{Binding ImageURL, Converter={StaticResource NullToVis}}" />
    //
    // PARAMETRU "Inverse":
    // Normal: non-null -> Visible, null -> Collapsed
    // Inverse: null -> Visible, non-null -> Collapsed
    // Util pentru "No data" messages
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        // METODA Convert - Transforma valoarea in Visibility
        //
        // LOGICA:
        // 1. Verificam daca valoarea e null sau "goala"
        // 2. Aplicam inversarea daca e ceruta
        // 3. Returnam Visible sau Collapsed
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificam daca valoarea este "goala"
            bool hasValue = HasValue(value);

            // Verificam daca trebuie inversat
            bool isInverse = parameter != null &&
                            parameter.ToString().ToLower() == "inverse";

            // Aplicam inversarea daca e ceruta
            if (isInverse)
            {
                hasValue = !hasValue;
            }

            // Returnam Visibility
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        // METODA HasValue - Verifica daca valoarea "exista"
        //
        // Considera ca valoare GOALA:
        // - null
        // - string gol sau doar spatii
        // - colectie goala
        // - Visibility.Collapsed (pentru nested conversions)
        private bool HasValue(object value)
        {
            // Null -> nu are valoare
            if (value == null)
                return false;

            // String gol sau whitespace -> nu are valoare
            if (value is string stringValue)
            {
                return !string.IsNullOrWhiteSpace(stringValue);
            }

            // Colectie goala -> nu are valoare
            if (value is System.Collections.ICollection collection)
            {
                return collection.Count > 0;
            }

            // IEnumerable gol -> nu are valoare
            if (value is System.Collections.IEnumerable enumerable)
            {
                // Verificam daca are cel putin un element
                var enumerator = enumerable.GetEnumerator();
                bool hasElements = enumerator.MoveNext();

                // Dispose daca e IDisposable
                if (enumerator is IDisposable disposable)
                    disposable.Dispose();

                return hasElements;
            }

            // Orice altceva non-null -> are valoare
            return true;
        }

        // METODA ConvertBack - Nu e implementata
        //
        // Nu are sens sa convertim Visibility inapoi la obiect
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
