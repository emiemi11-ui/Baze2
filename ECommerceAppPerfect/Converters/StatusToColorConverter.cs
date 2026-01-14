using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ECommerceAppPerfect.Converters
{
    // CLASA STATUSTOCOLORCONVERTER - Converteste status-uri in culori
    //
    // CE FACE ACEST CONVERTER?
    // Transforma string-uri de status (Pending, Processing, etc.)
    // in culori pentru afisare vizuala
    //
    // DE CE?
    // Utilizatorii inteleg mai rapid culorile decat textul
    // Verde = bine, Rosu = problema, Portocaliu = atentie
    // Imbunatateste UX semnificativ
    //
    // UNDE SE FOLOSESTE?
    // - Status comanda: Pending (orange), Delivered (verde), Cancelled (rosu)
    // - Status ticket: Open (albastru), InProgress (orange), Resolved (verde)
    // - Status stoc: In Stock (verde), Low Stock (orange), Out of Stock (rosu)
    // - Prioritati: Low (verde), Medium (orange), High (rosu)
    //
    // EXEMPLU XAML:
    // <TextBlock Text="{Binding OrderStatus}"
    //            Foreground="{Binding OrderStatus, Converter={StaticResource StatusToColor}}" />
    //
    // SAU pentru Background:
    // <Border Background="{Binding Status, Converter={StaticResource StatusToColor}}">
    //     <TextBlock Text="{Binding Status}" />
    // </Border>
    //
    // PARAMETRU OPTIONAL:
    // ConverterParameter specifica TIPUL de status pentru mapare corecta
    // "Order" -> mapare pentru OrderStatus
    // "Ticket" -> mapare pentru TicketStatus
    // "Stock" -> mapare pentru StockStatus
    // "Priority" -> mapare pentru Priority
    [ValueConversion(typeof(string), typeof(Brush))]
    public class StatusToColorConverter : IValueConverter
    {
        // METODA Convert - Transforma status in culoare
        //
        // PARAMETRI:
        // - value: String-ul de status
        // - parameter: Tipul de status (Order, Ticket, Stock, Priority)
        //
        // RETURNEAZA:
        // SolidColorBrush cu culoarea corespunzatoare
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Extragem status-ul ca string
            string status = value?.ToString() ?? "";

            // Extragem tipul de status din parametru
            string statusType = parameter?.ToString() ?? "";

            // Selectam paleta de culori bazat pe tip
            string hexColor = statusType.ToLower() switch
            {
                "order" => GetOrderStatusColor(status),
                "ticket" => GetTicketStatusColor(status),
                "stock" => GetStockStatusColor(status),
                "priority" => GetPriorityColor(status),
                _ => GetGenericStatusColor(status)
            };

            // Convertim HEX la Brush
            return CreateBrushFromHex(hexColor);
        }

        // METODA GetOrderStatusColor - Culori pentru status comenzi
        //
        // PALETTE:
        // Pending -> Orange (in asteptare)
        // Processing -> Blue (in lucru)
        // Shipped -> Purple (in drum)
        // Delivered -> Green (livrat - success!)
        // Cancelled -> Red (anulat - probleme)
        private string GetOrderStatusColor(string status)
        {
            return status switch
            {
                "Pending" => "#FF9800",      // Orange - Material Design
                "Processing" => "#2196F3",   // Blue
                "Shipped" => "#9C27B0",      // Purple
                "Delivered" => "#4CAF50",    // Green
                "Cancelled" => "#F44336",    // Red
                _ => "#757575"               // Grey - default
            };
        }

        // METODA GetTicketStatusColor - Culori pentru status ticket-uri
        //
        // PALETTE:
        // Open -> Blue (deschis, asteapta)
        // InProgress -> Orange (cineva lucreaza)
        // Resolved -> Green (rezolvat)
        // Closed -> Grey (inchis definitiv)
        private string GetTicketStatusColor(string status)
        {
            return status switch
            {
                "Open" => "#2196F3",         // Blue
                "InProgress" => "#FF9800",   // Orange
                "Resolved" => "#4CAF50",     // Green
                "Closed" => "#9E9E9E",       // Grey
                _ => "#757575"
            };
        }

        // METODA GetStockStatusColor - Culori pentru status stoc
        //
        // PALETTE:
        // In Stock -> Green (OK)
        // Low Stock -> Orange (atentie)
        // Out of Stock -> Red (probleme)
        private string GetStockStatusColor(string status)
        {
            // Verificam daca status-ul CONTINE anumite keyword-uri
            // pentru ca formatul poate varia: "In Stock", "In Stock (50)", etc.
            if (status.Contains("Out of Stock") || status == "Out of Stock")
                return "#F44336"; // Red

            if (status.Contains("Low Stock") || status == "Low Stock")
                return "#FF9800"; // Orange

            if (status.Contains("In Stock") || status == "In Stock")
                return "#4CAF50"; // Green

            return "#757575"; // Grey default
        }

        // METODA GetPriorityColor - Culori pentru prioritati
        //
        // PALETTE:
        // Low -> Green (nu e urgent)
        // Medium -> Orange (normal)
        // High -> Red (urgent!)
        private string GetPriorityColor(string priority)
        {
            return priority switch
            {
                "Low" => "#4CAF50",     // Green
                "Medium" => "#FF9800",  // Orange
                "High" => "#F44336",    // Red
                _ => "#757575"
            };
        }

        // METODA GetGenericStatusColor - Culori generice
        //
        // Folosit cand nu se specifica tipul de status
        // Incearca sa ghiceasca bazat pe cuvinte cheie comune
        private string GetGenericStatusColor(string status)
        {
            // Convertim la lowercase pentru comparatii
            string lower = status.ToLower();

            // Cuvinte cheie pentru SUCCESS (verde)
            if (lower.Contains("success") || lower.Contains("complete") ||
                lower.Contains("delivered") || lower.Contains("resolved") ||
                lower.Contains("active") || lower.Contains("in stock"))
                return "#4CAF50";

            // Cuvinte cheie pentru WARNING (portocaliu)
            if (lower.Contains("pending") || lower.Contains("processing") ||
                lower.Contains("progress") || lower.Contains("waiting") ||
                lower.Contains("low"))
                return "#FF9800";

            // Cuvinte cheie pentru ERROR (rosu)
            if (lower.Contains("error") || lower.Contains("fail") ||
                lower.Contains("cancelled") || lower.Contains("rejected") ||
                lower.Contains("out of stock") || lower.Contains("high"))
                return "#F44336";

            // Cuvinte cheie pentru INFO (albastru)
            if (lower.Contains("open") || lower.Contains("new") ||
                lower.Contains("info"))
                return "#2196F3";

            // Default: grey
            return "#757575";
        }

        // METODA HELPER CreateBrushFromHex - Creaza Brush din cod HEX
        //
        // PARAMETRU: hex - Codul de culoare (#RRGGBB)
        //
        // RETURNEAZA: SolidColorBrush cu culoarea
        private Brush CreateBrushFromHex(string hex)
        {
            try
            {
                // ColorConverter poate parsa "#RRGGBB" direct
                Color color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Daca conversia esueaza, returnam gri
                return Brushes.Gray;
            }
        }

        // METODA ConvertBack - Nu e implementata
        //
        // Nu are sens sa convertim culoare inapoi la status
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
