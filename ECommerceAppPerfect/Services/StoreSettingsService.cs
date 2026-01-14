using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA STORESETTINGSSERVICE - Implementarea serviciului de configurari
    //
    // ACEASTA CLASA GESTIONEAZA TABELUL KEY-VALUE PENTRU SETARI
    //
    // PATTERN KEY-VALUE:
    // In loc de un tabel cu coloane fixe:
    // CREATE TABLE Settings (StoreName NVARCHAR, TaxRate INT, ...)
    //
    // Avem un tabel generic:
    // CREATE TABLE StoreSettings (
    //     SettingKey NVARCHAR UNIQUE,
    //     SettingValue NVARCHAR,
    //     SettingType NVARCHAR
    // )
    //
    // AVANTAJ: Putem adauga setari noi fara ALTER TABLE
    // DEZAVANTAJ: Tot e stocat ca string, trebuie conversie
    //
    // CONCEPTE DEMONSTRATE:
    // 1. LINQ cu FirstOrDefault pe coloane UNIQUE
    //    Cautam dupa SettingKey care e UNIQUE, deci maxim 1 rezultat
    //
    // 2. CONVERSII DE TIP
    //    Stocam tot ca string, dar oferim metode GetSettingAsInt(), etc.
    //
    // 3. PATTERN "GET OR CREATE"
    //    SetSetting() fie actualizeaza, fie creeaza
    //
    // 4. PATTERN "GET OR DEFAULT"
    //    GetSettingOrDefault() returneaza o valoare default daca nu exista
    //
    // 5. SAVECHANGES() pentru persistenta
    //    Toate modificarile se salveaza doar la SaveChanges()
    public class StoreSettingsService : IStoreSettingsService, IDisposable
    {
        private bool _disposed = false;

        // HELPER - Creeaza un nou DbContext
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // CRUD - READ (CITIRE)

        // GetAllSettings - Toate setarile ordonate alfabetic
        //
        // QUERY SQL:
        // SELECT * FROM StoreSettings ORDER BY SettingKey
        public List<StoreSetting> GetAllSettings()
        {
            using (var context = GetContext())
            {
                return context.StoreSettings
                    .OrderBy(s => s.SettingKey)
                    .ToList();
            }
        }

        // GetSettingByKey - Returneaza setarea completa
        //
        // DEMONSTREAZA: Cautare dupa coloana UNIQUE
        // SettingKey e UNIQUE, deci FirstOrDefault e potrivit
        // Stim ca nu pot exista 2 setari cu aceeasi cheie
        //
        // QUERY SQL:
        // SELECT TOP 1 * FROM StoreSettings WHERE SettingKey = @key
        public StoreSetting GetSettingByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            using (var context = GetContext())
            {
                return context.StoreSettings
                    .FirstOrDefault(s => s.SettingKey == key);
            }
        }

        // GetSetting - Returneaza doar valoarea
        //
        // SHORTCUT pentru GetSettingByKey(key)?.SettingValue
        // Mai convenabil cand vrei doar valoarea, nu tot obiectul
        public string GetSetting(string key)
        {
            return GetSettingByKey(key)?.SettingValue;
        }

        // GetSettingAsInt - Conversie la int
        //
        // DEMONSTREAZA: Conversie sigura cu TryParse
        // Nu arunca exceptie daca valoarea nu e numar
        public int GetSettingAsInt(string key)
        {
            var value = GetSetting(key);
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        // GetSettingAsDecimal - Conversie la decimal
        public decimal GetSettingAsDecimal(string key)
        {
            var value = GetSetting(key);
            if (decimal.TryParse(value, out decimal result))
                return result;
            return 0;
        }

        // GetSettingAsBool - Conversie la boolean
        //
        // ACCEPTA multiple formate: "true", "false", "1", "0", "yes", "no"
        public bool GetSettingAsBool(string key)
        {
            var value = GetSetting(key);
            if (string.IsNullOrEmpty(value))
                return false;

            var lower = value.ToLower().Trim();
            return lower == "true" || lower == "1" || lower == "yes";
        }

        // GetSettingsByType - Filtreaza dupa tip
        //
        // FOLOSIRE: UI poate grupa setarile pe tip
        // - Toate setarile de tip Color pentru color picker
        // - Toate setarile Boolean pentru toggle switches
        public List<StoreSetting> GetSettingsByType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new List<StoreSetting>();

            using (var context = GetContext())
            {
                return context.StoreSettings
                    .Where(s => s.SettingType == type)
                    .OrderBy(s => s.SettingKey)
                    .ToList();
            }
        }

        // CRUD - CREATE/UPDATE (CREARE/ACTUALIZARE)

        // SetSetting - Seteaza valoarea (creeaza sau actualizeaza)
        //
        // DEMONSTREAZA: Pattern "Upsert" (UPDATE or INSERT)
        // 1. Cauta setarea dupa cheie
        // 2. Daca exista: UPDATE
        // 3. Daca nu exista: INSERT
        //
        // ATOMICITATE: Operatia e atomica - ori reuseste, ori nu
        public bool SetSetting(string key, string value)
        {
            return SetSettingWithType(key, value, "Text");
        }

        // SetSettingWithType - Cu specificare de tip
        public bool SetSettingWithType(string key, string value, string type)
        {
            return SetSettingWithDescription(key, value, type, null);
        }

        // SetSettingWithDescription - Implementarea completa
        //
        // DEMONSTREAZA: Pattern Upsert detaliat
        public bool SetSettingWithDescription(string key, string value, string type, string description)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // VALIDARE TIP
            var validTypes = new[] { "Text", "Number", "Color", "Boolean" };
            if (!validTypes.Contains(type))
                type = "Text";

            try
            {
                using (var context = GetContext())
                {
                    // CAUTAM SETAREA EXISTENTA
                    var setting = context.StoreSettings
                        .FirstOrDefault(s => s.SettingKey == key);

                    if (setting != null)
                    {
                        // UPDATE - Setarea exista
                        setting.SettingValue = value;
                        setting.SettingType = type;
                        if (description != null)
                            setting.Description = description;
                        setting.LastUpdated = DateTime.Now;
                    }
                    else
                    {
                        // INSERT - Setarea nu exista
                        setting = new StoreSetting
                        {
                            SettingKey = key,
                            SettingValue = value,
                            SettingType = type,
                            Description = description,
                            LastUpdated = DateTime.Now
                        };
                        context.StoreSettings.Add(setting);
                    }

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UpdateSetting - Actualizeaza o setare existenta
        //
        // DIFERENTA fata de SetSetting:
        // UpdateSetting ESUEAZA daca setarea nu exista
        // SetSetting CREEAZA setarea daca nu exista
        public bool UpdateSetting(StoreSetting setting)
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.SettingKey))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // CAUTAM SETAREA EXISTENTA
                    var existing = context.StoreSettings
                        .FirstOrDefault(s => s.SettingKey == setting.SettingKey);

                    if (existing == null)
                        return false;  // NU creeaza, doar actualizeaza

                    // UPDATE
                    existing.SettingValue = setting.SettingValue;
                    existing.SettingType = setting.SettingType;
                    existing.Description = setting.Description;
                    existing.LastUpdated = DateTime.Now;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CreateSetting - Creeaza o setare noua
        //
        // DIFERENTA fata de SetSetting:
        // CreateSetting ESUEAZA daca setarea exista deja
        // SetSetting ACTUALIZEAZA setarea daca exista
        public bool CreateSetting(StoreSetting setting)
        {
            if (setting == null || string.IsNullOrWhiteSpace(setting.SettingKey))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // VERIFICAM SA NU EXISTE
                    bool exists = context.StoreSettings
                        .Any(s => s.SettingKey == setting.SettingKey);

                    if (exists)
                        return false;  // Deja exista

                    // CREARE
                    setting.LastUpdated = DateTime.Now;
                    context.StoreSettings.Add(setting);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CRUD - DELETE (STERGERE)

        // DeleteSetting - Sterge o setare
        //
        // ATENTIE: Stergerea e permanenta!
        // Remove() marcheaza entitatea pentru stergere
        // SaveChanges() executa DELETE
        public bool DeleteSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var setting = context.StoreSettings
                        .FirstOrDefault(s => s.SettingKey == key);

                    if (setting == null)
                        return false;

                    context.StoreSettings.Remove(setting);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // METODE UTILITARE

        // SettingExists - Verifica existenta
        //
        // DEMONSTREAZA: Any() pentru verificare existenta
        // Mai eficient decat FirstOrDefault() != null
        // SQL: SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
        public bool SettingExists(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            using (var context = GetContext())
            {
                return context.StoreSettings
                    .Any(s => s.SettingKey == key);
            }
        }

        // GetSettingOrDefault - Cu valoare default
        //
        // PATTERN NULL COALESCING
        // Returneaza valoarea sau default-ul daca e null
        public string GetSettingOrDefault(string key, string defaultValue)
        {
            var value = GetSetting(key);
            return value ?? defaultValue;
        }

        // GetSettingAsIntOrDefault - Int cu default
        public int GetSettingAsIntOrDefault(string key, int defaultValue)
        {
            var value = GetSetting(key);
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        // GetSettingAsDecimalOrDefault - Decimal cu default
        public decimal GetSettingAsDecimalOrDefault(string key, decimal defaultValue)
        {
            var value = GetSetting(key);
            if (decimal.TryParse(value, out decimal result))
                return result;
            return defaultValue;
        }

        // GetSettingAsBoolOrDefault - Bool cu default
        public bool GetSettingAsBoolOrDefault(string key, bool defaultValue)
        {
            var value = GetSetting(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            var lower = value.ToLower().Trim();
            if (lower == "true" || lower == "1" || lower == "yes")
                return true;
            if (lower == "false" || lower == "0" || lower == "no")
                return false;

            return defaultValue;
        }

        // SETARI PREDEFINITE (SHORTCUTS)

        // Aceste metode ofera acces rapid la setarile comune
        // In loc de GetSetting("StoreName"), apelezi GetStoreName()

        public string GetStoreName()
        {
            return GetSettingOrDefault("StoreName", "E-Commerce Store");
        }

        public string GetStoreEmail()
        {
            return GetSettingOrDefault("StoreEmail", "contact@store.com");
        }

        public string GetStorePhone()
        {
            return GetSettingOrDefault("StorePhone", "");
        }

        public decimal GetTaxRate()
        {
            return GetSettingAsDecimalOrDefault("TaxRate", 19);
        }

        public decimal GetMinimumOrderAmount()
        {
            return GetSettingAsDecimalOrDefault("MinimumOrderAmount", 0);
        }

        public decimal GetFreeShippingThreshold()
        {
            return GetSettingAsDecimalOrDefault("FreeShippingThreshold", 0);
        }

        public string GetPrimaryColor()
        {
            return GetSettingOrDefault("PrimaryColor", "#2196F3");
        }

        public string GetAccentColor()
        {
            return GetSettingOrDefault("AccentColor", "#FF5722");
        }

        public string GetCurrencySymbol()
        {
            return GetSettingOrDefault("CurrencySymbol", "RON");
        }

        public bool AllowGuestCheckout()
        {
            return GetSettingAsBoolOrDefault("AllowGuestCheckout", false);
        }

        // INITIALIZARE

        // InitializeDefaultSettings - Creeaza setarile default
        //
        // FOLOSIRE: La prima rulare a aplicatiei sau pentru reset
        // Verifica fiecare setare si o creeaza doar daca nu exista
        //
        // DEMONSTREAZA: Batch insert cu verificare individuala
        public int InitializeDefaultSettings()
        {
            // DEFINIM SETARILE DEFAULT
            var defaultSettings = new List<StoreSetting>
            {
                new StoreSetting
                {
                    SettingKey = "StoreName",
                    SettingValue = "TechStore Premium",
                    SettingType = "Text",
                    Description = "Name of the store displayed to customers"
                },
                new StoreSetting
                {
                    SettingKey = "StoreEmail",
                    SettingValue = "contact@techstore.com",
                    SettingType = "Text",
                    Description = "Contact email address"
                },
                new StoreSetting
                {
                    SettingKey = "StorePhone",
                    SettingValue = "1-800-TECH-STORE",
                    SettingType = "Text",
                    Description = "Contact phone number"
                },
                new StoreSetting
                {
                    SettingKey = "PrimaryColor",
                    SettingValue = "#2196F3",
                    SettingType = "Color",
                    Description = "Primary brand color"
                },
                new StoreSetting
                {
                    SettingKey = "AccentColor",
                    SettingValue = "#FF5722",
                    SettingType = "Color",
                    Description = "Accent color for highlights"
                },
                new StoreSetting
                {
                    SettingKey = "CurrencySymbol",
                    SettingValue = "RON",
                    SettingType = "Text",
                    Description = "Currency symbol displayed"
                },
                new StoreSetting
                {
                    SettingKey = "TaxRate",
                    SettingValue = "19",
                    SettingType = "Number",
                    Description = "VAT percentage applied to orders"
                },
                new StoreSetting
                {
                    SettingKey = "MinimumOrderAmount",
                    SettingValue = "50",
                    SettingType = "Number",
                    Description = "Minimum order amount required"
                },
                new StoreSetting
                {
                    SettingKey = "FreeShippingThreshold",
                    SettingValue = "200",
                    SettingType = "Number",
                    Description = "Orders above this amount get free shipping"
                },
                new StoreSetting
                {
                    SettingKey = "AllowGuestCheckout",
                    SettingValue = "true",
                    SettingType = "Boolean",
                    Description = "Allow checkout without creating an account"
                }
            };

            int createdCount = 0;

            try
            {
                using (var context = GetContext())
                {
                    // INCARCAM CHEILE EXISTENTE PENTRU VERIFICARE
                    var existingKeys = context.StoreSettings
                        .Select(s => s.SettingKey)
                        .ToList();

                    foreach (var setting in defaultSettings)
                    {
                        // CREAM DOAR DACA NU EXISTA
                        if (!existingKeys.Contains(setting.SettingKey))
                        {
                            setting.LastUpdated = DateTime.Now;
                            context.StoreSettings.Add(setting);
                            createdCount++;
                        }
                    }

                    // SALVAM TOATE SETARILE NOI ODATA
                    if (createdCount > 0)
                        context.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Log exception in productie
            }

            return createdCount;
        }

        // DISPOSE PATTERN

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Elibereaza resurse managed
                }

                _disposed = true;
            }
        }
    }
}
