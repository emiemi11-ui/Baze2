using System;
using System.Globalization;
using System.Windows.Data;

namespace ECommerceAppPerfect.Converters
{
    // CLASA PRICEFORMATCONVERTER - Formateaza preturile pentru afisare
    //
    // CE FACE ACEST CONVERTER?
    // Transforma valori numerice (decimal, double, int) in format de pret
    // 1299.99 -> "1,299.99 RON"
    //
    // DE CE?
    // 1. CONSISTENTA: Acelasi format peste tot in aplicatie
    // 2. LOCALIZARE: Poti schimba formatul (RON, EUR, $) intr-un singur loc
    // 3. READABILITY: "1,299.99 RON" e mai usor de citit decat "1299.99"
    //
    // EXEMPLU XAML:
    // <TextBlock Text="{Binding Price, Converter={StaticResource PriceFormat}}" />
    //
    // REZULTAT:
    // Price = 1299.99 -> "1,299.99 RON"
    // Price = 50      -> "50.00 RON"
    // Price = null    -> "0.00 RON"
    //
    // PARAMETRU OPTIONAL:
    // ConverterParameter poate specifica simbolul monedei
    // ConverterParameter="EUR" -> "1,299.99 EUR"
    // ConverterParameter="$" -> "$1,299.99" (prefixat pentru dolari)
    [ValueConversion(typeof(decimal), typeof(string))]
    public class PriceFormatConverter : IValueConverter
    {
        // CONSTANTA DEFAULT CURRENCY
        //
        // Moneda default daca nu se specifica alta
        // RON = Leu romanesc
        private const string DefaultCurrency = "RON";

        // METODA Convert - Formateaza pretul pentru afisare
        //
        // PARAMETRI:
        // - value: Valoarea numerica (decimal, double, int, sau string)
        // - parameter: Simbolul monedei (optional)
        //
        // RETURNEAZA:
        // String formatat: "1,299.99 RON"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Extragem valoarea numerica
            decimal amount = ExtractDecimalValue(value);

            // Extragem simbolul monedei
            string currency = parameter?.ToString() ?? DefaultCurrency;

            // Formatam bazat pe tipul de moneda
            return FormatPrice(amount, currency);
        }

        // METODA ExtractDecimalValue - Extrage valoarea decimala din diferite tipuri
        //
        // ACCEPTA:
        // - decimal
        // - double
        // - int
        // - string (care poate fi parsat)
        // - null (returneaza 0)
        private decimal ExtractDecimalValue(object value)
        {
            // Null -> 0
            if (value == null)
                return 0;

            // Deja decimal
            if (value is decimal decimalValue)
                return decimalValue;

            // Double -> decimal
            if (value is double doubleValue)
                return (decimal)doubleValue;

            // Int -> decimal
            if (value is int intValue)
                return intValue;

            // String -> parse
            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal parsed))
                    return parsed;
            }

            // Default
            return 0;
        }

        // METODA FormatPrice - Formateaza pretul cu moneda
        //
        // LOGICA:
        // - Pentru $ (dolar): prefix ($1,299.99)
        // - Pentru alte monede: suffix (1,299.99 RON)
        // - Format numeric: N2 (doua zecimale, separator de mii)
        private string FormatPrice(decimal amount, string currency)
        {
            // Format numeric cu 2 zecimale si separator de mii
            // InvariantCulture foloseste . pentru zecimale si , pentru mii
            string formattedAmount = amount.ToString("N2", CultureInfo.InvariantCulture);

            // Monede cu prefix (dolari)
            if (currency == "$" || currency.ToUpper() == "USD")
            {
                return $"${formattedAmount}";
            }

            // Monede cu prefix (euro - depinde de tara)
            if (currency.ToUpper() == "EUR")
            {
                return $"{formattedAmount} EUR";
            }

            // Monede cu suffix (RON, GBP, etc.)
            return $"{formattedAmount} {currency}";
        }

        // METODA ConvertBack - Parseaza string de pret inapoi la decimal
        //
        // FOLOSIRE:
        // Pentru TextBox-uri unde utilizatorul introduce pretul
        // Scoate simbolul monedei si parseaza numarul
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificare null/empty
            if (value == null)
                return 0m;

            string text = value.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return 0m;

            // Scoatem caracterele non-numerice (simboluri moneda, spatii)
            string cleanText = CleanPriceText(text);

            // Parsam valoarea
            if (decimal.TryParse(cleanText, NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            // Daca nu reusim, returnam 0
            return 0m;
        }

        // METODA CleanPriceText - Curata textul de simboluri non-numerice
        //
        // INPUT: "$1,299.99 RON"
        // OUTPUT: "1299.99"
        private string CleanPriceText(string text)
        {
            // StringBuilder pentru eficienta
            var cleaned = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                // Pastram doar cifre, punct si minus
                if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    cleaned.Append(c);
                }
                // Virgula ca separator de mii o ignoram (nu o adaugam)
            }

            return cleaned.ToString();
        }
    }
}
