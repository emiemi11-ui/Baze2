using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceAppPerfect.Models
{
    // CLASA STORESETTING - Entitatea pentru setarile magazinului
    //
    // CE ESTE STORESETTINGS?
    // Un tabel KEY-VALUE pentru configurarile magazinului
    // Permite stocarea si modificarea setarilor FARA a schimba codul
    //
    // DE CE KEY-VALUE SI NU COLOANE FIXE?
    // 1. FLEXIBILITATE: Poti adauga setari noi fara sa modifici schema DB
    // 2. DINAMIC: Setarile pot fi modificate din UI fara rebuild
    // 3. EXTENSIBIL: Noi feature-uri pot veni cu setari proprii
    //
    // STRUCTURA:
    // - SettingKey: Numele setarii (unic)
    // - SettingValue: Valoarea ca string
    // - SettingType: Tipul valorii (Text, Color, Number, Boolean)
    //
    // EXEMPLE DE SETARI:
    // | Key                    | Value           | Type    |
    // |------------------------|-----------------|---------|
    // | StoreName              | TechStore       | Text    |
    // | PrimaryColor           | #2196F3         | Color   |
    // | TaxRate                | 19              | Number  |
    // | AllowGuestCheckout     | true            | Boolean |
    //
    // ACCESARE IN COD:
    // var storeName = settings.FirstOrDefault(s => s.SettingKey == "StoreName")?.SettingValue;
    //
    // SAU PRIN SERVICE:
    // var storeName = storeSettingsService.GetSetting("StoreName");
    [Table("StoreSettings")]
    public partial class StoreSetting
    {
        // PROPRIETATI - Coloanele din tabel

        // SettingID - Cheia primara
        //
        // ID numeric pentru fiecare setare
        // Desi am putea folosi SettingKey ca PK (e unic),
        // avem ID separat pentru consistenta cu celelalte tabele
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SettingID { get; set; }

        // SettingKey - Cheia setarii (numele)
        //
        // UNIQUE in baza de date - fiecare setare are un nume unic
        // Conventie: PascalCase fara spatii
        //
        // EXEMPLE:
        // - "StoreName"
        // - "PrimaryColor"
        // - "TaxRate"
        // - "AllowGuestCheckout"
        // - "FreeShippingThreshold"
        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; }

        // SettingValue - Valoarea setarii (ca string)
        //
        // TOATE valorile sunt stocate ca STRING!
        // Conversia la tipul corect se face in cod, bazat pe SettingType
        //
        // EXEMPLE:
        // - Text: "TechStore Premium"
        // - Color: "#2196F3"
        // - Number: "19" (se converteste la int/decimal)
        // - Boolean: "true" sau "false"
        [StringLength(500)]
        public string SettingValue { get; set; }

        // SettingType - Tipul valorii
        //
        // Indica cum sa interpretam SettingValue
        // Folosit pentru:
        // 1. Validare in UI (input corect)
        // 2. Conversie in cod
        // 3. Afisare corecta (color picker pentru Color, checkbox pentru Boolean)
        //
        // VALORI POSIBILE:
        // - "Text" - string simplu
        // - "Color" - cod HEX (#RRGGBB)
        // - "Number" - numar (int sau decimal)
        // - "Boolean" - true/false
        [Required]
        [StringLength(50)]
        public string SettingType { get; set; }

        // Description - Descrierea setarii
        //
        // Explica ce face setarea
        // Afisata in UI ca tooltip sau help text
        //
        // EXEMPLU:
        // Key: "TaxRate"
        // Description: "VAT percentage applied to all orders"
        [StringLength(500)]
        public string Description { get; set; }

        // LastUpdated - Data ultimei modificari
        //
        // Se actualizeaza automat la fiecare modificare
        // Util pentru audit si debugging
        public DateTime LastUpdated { get; set; }

        // PROPRIETATI CALCULATE - Conversii

        // ValueAsInt - Valoarea ca integer
        //
        // Converteste SettingValue la int
        // Returneaza 0 daca conversia esueaza
        //
        // FOLOSIRE:
        // var taxRate = setting.ValueAsInt;
        [NotMapped]
        public int ValueAsInt
        {
            get
            {
                if (int.TryParse(SettingValue, out int result))
                    return result;

                return 0;
            }
        }

        // ValueAsDecimal - Valoarea ca decimal
        //
        // Converteste SettingValue la decimal
        // Util pentru preturi, procente cu zecimale
        [NotMapped]
        public decimal ValueAsDecimal
        {
            get
            {
                if (decimal.TryParse(SettingValue, out decimal result))
                    return result;

                return 0;
            }
        }

        // ValueAsBool - Valoarea ca boolean
        //
        // Converteste SettingValue la bool
        // Accepta: "true", "false", "1", "0", "yes", "no"
        [NotMapped]
        public bool ValueAsBool
        {
            get
            {
                if (string.IsNullOrEmpty(SettingValue))
                    return false;

                var lower = SettingValue.ToLower();
                return lower == "true" || lower == "1" || lower == "yes";
            }
        }

        // IsTextType - Este setare de tip Text?
        [NotMapped]
        public bool IsTextType => SettingType == "Text";

        // IsColorType - Este setare de tip Color?
        [NotMapped]
        public bool IsColorType => SettingType == "Color";

        // IsNumberType - Este setare de tip Number?
        [NotMapped]
        public bool IsNumberType => SettingType == "Number";

        // IsBooleanType - Este setare de tip Boolean?
        [NotMapped]
        public bool IsBooleanType => SettingType == "Boolean";

        // DisplayValue - Valoarea formatata pentru afisare
        //
        // Formateaza valoarea in functie de tip
        // Boolean: "Yes"/"No" in loc de "true"/"false"
        // Number: cu separator de mii
        [NotMapped]
        public string DisplayValue
        {
            get
            {
                if (string.IsNullOrEmpty(SettingValue))
                    return "(not set)";

                if (IsBooleanType)
                    return ValueAsBool ? "Yes" : "No";

                if (IsNumberType)
                    return $"{ValueAsDecimal:N0}";

                return SettingValue;
            }
        }

        // METODE DE SETARE - Pentru tipuri specifice

        // SetValue - Seteaza valoarea (genereaza string din orice tip)
        //
        // Metoda generica pentru setarea valorii
        // Converteste automat la string
        public void SetValue(object value)
        {
            if (value == null)
            {
                SettingValue = null;
            }
            else if (value is bool boolValue)
            {
                SettingValue = boolValue ? "true" : "false";
            }
            else
            {
                SettingValue = value.ToString();
            }

            LastUpdated = DateTime.Now;
        }
    }
}
