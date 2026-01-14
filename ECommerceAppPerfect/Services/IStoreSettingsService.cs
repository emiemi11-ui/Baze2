using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA ISTORESSETTINGSSERVICE - Contract pentru serviciul de configurari
    //
    // CE SUNT STORE SETTINGS?
    // Setarile magazinului - configurari care pot fi modificate fara a schimba codul
    // Stocate intr-un tabel KEY-VALUE pentru flexibilitate maxima
    //
    // STRUCTURA KEY-VALUE:
    // In loc de coloane fixe (StoreName, TaxRate, etc.)
    // Avem perechi cheie-valoare:
    // | SettingKey        | SettingValue | SettingType |
    // |-------------------|--------------|-------------|
    // | StoreName         | TechStore    | Text        |
    // | TaxRate           | 19           | Number      |
    // | PrimaryColor      | #2196F3      | Color       |
    // | AllowGuestCheckout| true         | Boolean     |
    //
    // AVANTAJE KEY-VALUE:
    // 1. FLEXIBILITATE: Adaugi setari noi fara a modifica schema DB
    // 2. DINAMIC: Modifici din UI fara rebuild aplicatie
    // 3. EXTENSIBIL: Noi feature-uri vin cu setari proprii
    // 4. SIMPLU: Un singur tabel pentru toate configurarile
    //
    // DEZAVANTAJE KEY-VALUE:
    // 1. NU AI TIPURI STRICTE: Tot e string, conversie manuala
    // 2. NU AI VALIDARE SCHEMA: Poti pune orice in SettingValue
    // 3. QUERY-URI MAI COMPLEXE: WHERE SettingKey = 'X' in loc de doar SELECT X
    //
    // TIPURI DE SETARI (SettingType):
    // - Text: string simplu (StoreName, StoreEmail)
    // - Number: valori numerice ca string (TaxRate, MinimumOrder)
    // - Color: coduri HEX (#2196F3, #FF5722)
    // - Boolean: true/false ca string (AllowGuestCheckout)
    //
    // EXEMPLU DE FOLOSIRE:
    // var storeName = settingsService.GetSetting("StoreName");
    // var taxRate = settingsService.GetSettingAsInt("TaxRate");
    // var primaryColor = settingsService.GetSetting("PrimaryColor");
    // var allowGuest = settingsService.GetSettingAsBool("AllowGuestCheckout");
    //
    // CONCEPTE DIN CURS DEMONSTRATE:
    // - LINQ to Entities pentru interogari
    // - SaveChanges() pentru persistenta
    // - Pattern repository pentru acces date
    public interface IStoreSettingsService
    {
        // CRUD - READ (CITIRE)

        // GetAllSettings - Returneaza toate setarile
        //
        // FOLOSIRE: Pagina de setari pentru StoreOwner
        //
        // RETURNEAZA: Lista tuturor setarilor, ordonate alfabetic dupa cheie
        List<StoreSetting> GetAllSettings();

        // GetSettingByKey - Returneaza o setare dupa cheie
        //
        // PARAMETRU: key - cheia setarii (ex: "StoreName", "TaxRate")
        //
        // RETURNEAZA:
        // - Obiectul StoreSetting daca exista
        // - null daca nu exista setarea
        //
        // FOLOSIRE:
        // var setting = service.GetSettingByKey("StoreName");
        // string name = setting?.SettingValue;
        StoreSetting GetSettingByKey(string key);

        // GetSetting - Returneaza direct valoarea unei setari
        //
        // PARAMETRU: key - cheia setarii
        //
        // RETURNEAZA:
        // - Valoarea ca string daca exista
        // - null daca nu exista
        //
        // SHORTCUT pentru GetSettingByKey(key)?.SettingValue
        string GetSetting(string key);

        // GetSettingAsInt - Returneaza valoarea ca int
        //
        // PARAMETRU: key - cheia setarii
        //
        // RETURNEAZA:
        // - Valoarea convertita la int
        // - 0 daca nu exista sau nu se poate converti
        //
        // FOLOSIRE: var taxRate = service.GetSettingAsInt("TaxRate");
        int GetSettingAsInt(string key);

        // GetSettingAsDecimal - Returneaza valoarea ca decimal
        //
        // PARAMETRU: key
        //
        // FOLOSIRE: var minOrder = service.GetSettingAsDecimal("MinimumOrderAmount");
        decimal GetSettingAsDecimal(string key);

        // GetSettingAsBool - Returneaza valoarea ca boolean
        //
        // PARAMETRU: key
        //
        // ACCEPTA: "true", "false", "1", "0", "yes", "no"
        //
        // FOLOSIRE: var allowGuest = service.GetSettingAsBool("AllowGuestCheckout");
        bool GetSettingAsBool(string key);

        // GetSettingsByType - Returneaza setarile de un anumit tip
        //
        // PARAMETRU: type - "Text", "Number", "Color", "Boolean"
        //
        // FOLOSIRE: Grupare setari pe tip in UI
        List<StoreSetting> GetSettingsByType(string type);

        // CRUD - CREATE/UPDATE (CREARE/ACTUALIZARE)

        // SetSetting - Seteaza o valoare (creeaza sau actualizeaza)
        //
        // PARAMETRI:
        // - key: cheia setarii
        // - value: noua valoare (ca string)
        //
        // COMPORTAMENT:
        // - Daca setarea EXISTA: actualizeaza valoarea
        // - Daca setarea NU EXISTA: o creeaza cu tip "Text"
        //
        // RETURNEAZA: true daca operatia a reusit
        //
        // FOLOSIRE SIMPLA:
        // service.SetSetting("StoreName", "My New Store");
        bool SetSetting(string key, string value);

        // SetSettingWithType - Seteaza valoarea cu tip specific
        //
        // PARAMETRI:
        // - key: cheia setarii
        // - value: noua valoare
        // - type: tipul setarii ("Text", "Number", "Color", "Boolean")
        //
        // FOLOSIRE:
        // service.SetSettingWithType("TaxRate", "19", "Number");
        // service.SetSettingWithType("PrimaryColor", "#2196F3", "Color");
        bool SetSettingWithType(string key, string value, string type);

        // SetSettingWithDescription - Seteaza cu tip si descriere
        //
        // PARAMETRI:
        // - key: cheia setarii
        // - value: valoarea
        // - type: tipul
        // - description: descrierea (ajutor in UI)
        //
        // FOLOSIRE:
        // service.SetSettingWithDescription(
        //     "FreeShippingThreshold",
        //     "200",
        //     "Number",
        //     "Comenzile peste aceasta suma au transport gratuit"
        // );
        bool SetSettingWithDescription(string key, string value, string type, string description);

        // UpdateSetting - Actualizeaza o setare existenta
        //
        // PARAMETRU: setting - obiectul StoreSetting modificat
        //
        // ATENTIE: Setarea trebuie sa existe deja!
        // NU creeaza setari noi
        //
        // RETURNEAZA: true daca s-a actualizat
        bool UpdateSetting(StoreSetting setting);

        // CreateSetting - Creeaza o setare noua
        //
        // PARAMETRU: setting - obiectul StoreSetting de creat
        //
        // ATENTIE: Cheia trebuie sa fie UNICA!
        // Returneaza false daca exista deja
        //
        // RETURNEAZA: true daca s-a creat
        bool CreateSetting(StoreSetting setting);

        // CRUD - DELETE (STERGERE)

        // DeleteSetting - Sterge o setare
        //
        // PARAMETRU: key - cheia setarii de sters
        //
        // ATENTIE: Stergerea e permanenta!
        // In productie, ar trebui confirmari
        //
        // RETURNEAZA: true daca s-a sters
        bool DeleteSetting(string key);

        // METODE UTILITARE

        // SettingExists - Verifica daca o setare exista
        //
        // PARAMETRU: key
        //
        // RETURNEAZA: true daca exista
        bool SettingExists(string key);

        // GetSettingOrDefault - Returneaza valoarea sau un default
        //
        // PARAMETRI:
        // - key: cheia setarii
        // - defaultValue: valoarea default daca nu exista
        //
        // FOLOSIRE:
        // var currency = service.GetSettingOrDefault("Currency", "RON");
        string GetSettingOrDefault(string key, string defaultValue);

        // GetSettingAsIntOrDefault - Returneaza ca int sau default
        int GetSettingAsIntOrDefault(string key, int defaultValue);

        // GetSettingAsDecimalOrDefault - Returneaza ca decimal sau default
        decimal GetSettingAsDecimalOrDefault(string key, decimal defaultValue);

        // GetSettingAsBoolOrDefault - Returneaza ca bool sau default
        bool GetSettingAsBoolOrDefault(string key, bool defaultValue);

        // SETARI PREDEFINITE (SHORTCUTS)

        // GetStoreName - Numele magazinului
        //
        // Shortcut pentru GetSetting("StoreName")
        string GetStoreName();

        // GetStoreEmail - Email-ul magazinului
        string GetStoreEmail();

        // GetStorePhone - Telefonul magazinului
        string GetStorePhone();

        // GetTaxRate - Procentul TVA
        //
        // RETURNEAZA: Valoarea ca decimal (ex: 19 pentru 19%)
        decimal GetTaxRate();

        // GetMinimumOrderAmount - Valoarea minima a comenzii
        decimal GetMinimumOrderAmount();

        // GetFreeShippingThreshold - Pragul pentru transport gratuit
        decimal GetFreeShippingThreshold();

        // GetPrimaryColor - Culoarea primara a brandului
        string GetPrimaryColor();

        // GetAccentColor - Culoarea de accent
        string GetAccentColor();

        // GetCurrencySymbol - Simbolul monedei
        string GetCurrencySymbol();

        // AllowGuestCheckout - Permite checkout fara cont?
        bool AllowGuestCheckout();

        // INITIALIZARE

        // InitializeDefaultSettings - Creeaza setarile default
        //
        // FOLOSIRE: La prima rulare a aplicatiei
        // Creeaza toate setarile cu valori default daca nu exista
        //
        // RETURNEAZA: Numarul de setari create
        int InitializeDefaultSettings();
    }
}
