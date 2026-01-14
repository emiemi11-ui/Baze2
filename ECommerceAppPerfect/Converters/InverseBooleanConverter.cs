using System;
using System.Globalization;
using System.Windows.Data;

namespace ECommerceAppPerfect.Converters
{
    // CLASA INVERSEBOOLEANCONVERTER - Inverseaza valori booleene
    //
    // CE FACE ACEST CONVERTER?
    // Inverseaza o valoare booleana: true -> false, false -> true
    //
    // DE CE AVEM NEVOIE?
    // In MVVM, proprietatile au nume POZITIVE:
    // - IsEnabled, IsVisible, IsActive, CanEdit
    //
    // Dar uneori vrem sa legam de CONTRARIUL:
    // - IsEnabled="{Binding IsReadOnly}" - GRESIT! Ar fi enabled cand e read-only
    // - IsEnabled="{Binding IsReadOnly, Converter={StaticResource InverseBool}}" - CORECT!
    //
    // EXEMPLU:
    // ViewModel are: bool IsLoading { get; set; }
    // Vrem: Butonul sa fie disabled CAND se incarca
    //
    // GRESIT:
    // <Button IsEnabled="{Binding IsLoading}" />  // Enabled cand se incarca?!
    //
    // CORECT:
    // <Button IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBool}}" />
    // IsLoading = true  -> IsEnabled = false (disabled)
    // IsLoading = false -> IsEnabled = true (enabled)
    //
    // ALTERNATIVA:
    // Ai putea crea proprietate separata: bool IsNotLoading => !IsLoading;
    // Dar e mai elegant cu converter - nu polueaza ViewModel-ul
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        // METODA Convert - Inverseaza bool-ul
        //
        // INPUT: true -> OUTPUT: false
        // INPUT: false -> OUTPUT: true
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificare tip
            if (value is bool boolValue)
            {
                // Inversam cu operatorul NOT (!)
                return !boolValue;
            }

            // Daca nu e bool, returnam false ca default
            return false;
        }

        // METODA ConvertBack - Inverseaza inapoi
        //
        // Pentru TwoWay binding, aplicam aceeasi logica
        // (inversarea e simetrica: invers din invers = original)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Aceeasi logica - inversam
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return false;
        }
    }
}
