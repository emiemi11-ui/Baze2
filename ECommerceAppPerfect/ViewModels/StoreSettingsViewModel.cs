using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ECommerceAppPerfect.Commands;
using ECommerceAppPerfect.Models;
using ECommerceAppPerfect.Stores;

namespace ECommerceAppPerfect.ViewModels
{
    // CLASA STORESETTINGSVIEWMODEL - Gestionarea setarilor magazinului
    //
    // CE ESTE ACEST VIEWMODEL?
    // Permite StoreOwner-ului sa configureze magazinul prin setari:
    // - Nume magazin
    // - Culori tema
    // - Taxa (VAT)
    // - Optiuni de checkout
    // - etc.
    //
    // PATTERN KEY-VALUE:
    // Setarile sunt stocate ca perechi KEY-VALUE in tabelul StoreSettings
    // Aceasta permite adaugarea de setari noi FARA a modifica schema bazei de date
    //
    // STRUCTURA TABEL:
    // | SettingKey           | SettingValue | SettingType |
    // |----------------------|--------------|-------------|
    // | StoreName            | TechStore    | Text        |
    // | PrimaryColor         | #2196F3      | Color       |
    // | TaxRate              | 19           | Number      |
    // | AllowGuestCheckout   | true         | Boolean     |
    //
    // AVANTAJE PATTERN KEY-VALUE:
    // 1. FLEXIBILITATE - Adaugi setari fara ALTER TABLE
    // 2. DINAMIC - Poti adauga setari din cod sau UI
    // 3. EXTENSIBIL - Module noi pot avea setari proprii
    //
    // DEZAVANTAJE:
    // 1. FARA TIPARE - Toate valorile sunt string
    // 2. FARA VALIDARE SQL - Validarea e in cod
    // 3. FARA INTEGRITATE - Nu poti folosi FK
    //
    // UI TIPIC:
    // +----------------------------------------------------------------+
    // | STORE SETTINGS                                                |
    // +----------------------------------------------------------------+
    // | Store Name:     [TechStore Premium          ]                 |
    // | Primary Color:  [#2196F3] [Color Picker]                      |
    // | Tax Rate (%):   [19      ]                                    |
    // | Guest Checkout: [x] Allow                                     |
    // +----------------------------------------------------------------+
    // | [Save Changes]                              [Reset to Default]|
    // +----------------------------------------------------------------+
    //
    // BINDING:
    // Fiecare setare e afisata diferit in functie de SettingType:
    // - Text: TextBox
    // - Color: TextBox + ColorPicker
    // - Number: TextBox cu validare numerica
    // - Boolean: CheckBox
    public class StoreSettingsViewModel : ViewModelBase
    {
        // STORES

        // _currentUserStore - Informatii despre utilizatorul curent
        //
        // Doar StoreOwner poate modifica setarile
        private readonly CurrentUserStore _currentUserStore;

        // _navigationStore - Pentru navigare
        private readonly NavigationStore _navigationStore;

        // COLECTII

        // _settings - Lista de setari
        //
        // ObservableCollection pentru binding la UI
        private ObservableCollection<StoreSetting> _settings;

        // _originalSettings - Valorile originale pentru detectarea modificarilor
        private Dictionary<string, string> _originalSettings;

        // CAMPURI PROPRIETATI

        // Setarea selectata
        private StoreSetting _selectedSetting;

        // Flag pentru modificari nesalvate
        private bool _hasUnsavedChanges;

        // Flag incarcare
        private bool _isLoading;

        // Flag salvare
        private bool _isSaving;

        // Mesaj eroare
        private string _errorMessage;

        // Mesaj succes
        private string _successMessage;

        // CONSTRUCTOR
        public StoreSettingsViewModel(
            CurrentUserStore currentUserStore,
            NavigationStore navigationStore)
        {
            // Salvare dependinte
            _currentUserStore = currentUserStore ?? throw new ArgumentNullException(nameof(currentUserStore));
            _navigationStore = navigationStore ?? throw new ArgumentNullException(nameof(navigationStore));

            // Initializare colectii
            _settings = new ObservableCollection<StoreSetting>();
            _originalSettings = new Dictionary<string, string>();

            // Initializare comenzi
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            ResetCommand = new RelayCommand(ExecuteReset, CanExecuteReset);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault);
            RefreshCommand = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            AddSettingCommand = new RelayCommand(ExecuteAddSetting);
            DeleteSettingCommand = new RelayCommand(ExecuteDeleteSetting, CanExecuteDeleteSetting);

            // Incarcare date
            LoadData();
        }

        // PROPRIETATI - Colectii

        // Settings - Lista de setari
        //
        // BINDING:
        // <ItemsControl ItemsSource="{Binding Settings}">
        //     <ItemsControl.ItemTemplate>
        //         <DataTemplate>
        //             <Grid>
        //                 <TextBlock Text="{Binding SettingKey}" />
        //                 <!-- Control diferit in functie de tip -->
        //                 <ContentControl>
        //                     <ContentControl.Style>
        //                         <Style TargetType="ContentControl">
        //                             <Style.Triggers>
        //                                 <DataTrigger Binding="{Binding IsTextType}" Value="True">
        //                                     <Setter Property="Content">
        //                                         <Setter.Value>
        //                                             <TextBox Text="{Binding SettingValue}" />
        //                                         </Setter.Value>
        //                                     </Setter>
        //                                 </DataTrigger>
        //                                 <DataTrigger Binding="{Binding IsBooleanType}" Value="True">
        //                                     <Setter Property="Content">
        //                                         <Setter.Value>
        //                                             <CheckBox IsChecked="{Binding ValueAsBool}" />
        //                                         </Setter.Value>
        //                                     </Setter>
        //                                 </DataTrigger>
        //                             </Style.Triggers>
        //                         </Style>
        //                     </ContentControl.Style>
        //                 </ContentControl>
        //             </Grid>
        //         </DataTemplate>
        //     </ItemsControl.ItemTemplate>
        // </ItemsControl>
        public ObservableCollection<StoreSetting> Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        // PROPRIETATI - Selectie

        // SelectedSetting - Setarea selectata
        public StoreSetting SelectedSetting
        {
            get => _selectedSetting;
            set
            {
                if (SetProperty(ref _selectedSetting, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(SelectedSettingKey));
                    OnPropertyChanged(nameof(SelectedSettingValue));
                    OnPropertyChanged(nameof(SelectedSettingType));
                    OnPropertyChanged(nameof(SelectedSettingDescription));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // PROPRIETATI - Stare

        // HasUnsavedChanges - Exista modificari nesalvate?
        //
        // Folosit pentru:
        // - A activa/dezactiva butonul Save
        // - A avertiza la navigare (unsaved changes dialog)
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        // IsLoading - Se incarca?
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // IsSaving - Se salveaza?
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        // ErrorMessage - Mesaj eroare
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // HasError - Exista eroare?
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // SuccessMessage - Mesaj succes
        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        // HasSuccess - Exista mesaj de succes?
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        // PROPRIETATI CALCULATE - Despre selectie

        // HasSelection - Este selectata o setare?
        public bool HasSelection => SelectedSetting != null;

        // SelectedSettingKey - Cheia setarii selectate
        public string SelectedSettingKey => SelectedSetting?.SettingKey ?? "No selection";

        // SelectedSettingValue - Valoarea setarii selectate
        public string SelectedSettingValue => SelectedSetting?.SettingValue ?? "N/A";

        // SelectedSettingType - Tipul setarii selectate
        public string SelectedSettingType => SelectedSetting?.SettingType ?? "N/A";

        // SelectedSettingDescription - Descrierea setarii selectate
        public string SelectedSettingDescription => SelectedSetting?.Description ?? "No description";

        // PROPRIETATI CALCULATE - Statistici

        // TotalSettingsCount - Total setari
        public int TotalSettingsCount => Settings?.Count ?? 0;

        // PROPRIETATI SHORTCUT - Pentru binding direct la setari comune
        // Aceste proprietati permit binding simplu la setari uzuale

        // StoreName - Numele magazinului
        public string StoreName
        {
            get => GetSettingValue("StoreName");
            set => UpdateSetting("StoreName", value);
        }

        // PrimaryColor - Culoarea primara
        public string PrimaryColor
        {
            get => GetSettingValue("PrimaryColor");
            set => UpdateSetting("PrimaryColor", value);
        }

        // TaxRate - Procentul de taxa
        public int TaxRate
        {
            get
            {
                var setting = Settings.FirstOrDefault(s => s.SettingKey == "TaxRate");
                return setting?.ValueAsInt ?? 0;
            }
            set => UpdateSetting("TaxRate", value.ToString());
        }

        // AllowGuestCheckout - Este permis checkout-ul ca guest?
        public bool AllowGuestCheckout
        {
            get
            {
                var setting = Settings.FirstOrDefault(s => s.SettingKey == "AllowGuestCheckout");
                return setting?.ValueAsBool ?? false;
            }
            set => UpdateSetting("AllowGuestCheckout", value ? "true" : "false");
        }

        // FreeShippingThreshold - De la ce suma e transport gratuit
        public decimal FreeShippingThreshold
        {
            get
            {
                var setting = Settings.FirstOrDefault(s => s.SettingKey == "FreeShippingThreshold");
                return setting?.ValueAsDecimal ?? 0;
            }
            set => UpdateSetting("FreeShippingThreshold", value.ToString());
        }

        // COMENZI

        // SaveCommand - Salveaza modificarile
        public ICommand SaveCommand { get; }

        // ResetCommand - Anuleaza modificarile (revert la valorile originale)
        public ICommand ResetCommand { get; }

        // ResetToDefaultCommand - Reseteaza la valorile default
        public ICommand ResetToDefaultCommand { get; }

        // RefreshCommand - Reincarca datele din DB
        public ICommand RefreshCommand { get; }

        // AddSettingCommand - Adauga o setare noua
        public ICommand AddSettingCommand { get; }

        // DeleteSettingCommand - Sterge setarea selectata
        public ICommand DeleteSettingCommand { get; }

        // METODE HELPER - Pentru acces la setari

        // GetSettingValue - Obtine valoarea unei setari dupa cheie
        //
        // PARAMETRU: key - Cheia setarii
        // RETURNEAZA: Valoarea sau string gol daca nu exista
        private string GetSettingValue(string key)
        {
            var setting = Settings.FirstOrDefault(s => s.SettingKey == key);
            return setting?.SettingValue ?? string.Empty;
        }

        // UpdateSetting - Actualizeaza valoarea unei setari
        //
        // PARAMETRI:
        // - key: Cheia setarii
        // - value: Noua valoare
        private void UpdateSetting(string key, string value)
        {
            var setting = Settings.FirstOrDefault(s => s.SettingKey == key);

            if (setting == null)
                return;

            if (setting.SettingValue != value)
            {
                setting.SettingValue = value;
                setting.LastUpdated = DateTime.Now;
                HasUnsavedChanges = true;

                // Notificam proprietatea corespunzatoare
                OnPropertyChanged(key);
            }
        }

        // METODE INCARCARE DATE

        // LoadData - Incarca setarile din baza de date
        private void LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                // In productie: var settings = _settingsService.GetAllSettings();

                // Pentru demonstratie, cream setari default
                Settings.Clear();
                _originalSettings.Clear();

                // Setari demonstrative
                var defaultSettings = CreateDefaultSettings();

                foreach (var setting in defaultSettings)
                {
                    Settings.Add(setting);
                    _originalSettings[setting.SettingKey] = setting.SettingValue;
                }

                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load settings: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(TotalSettingsCount));

                // Notificam proprietatile shortcut
                NotifyShortcutProperties();
            }
        }

        // CreateDefaultSettings - Creeaza setarile default pentru demonstratie
        private List<StoreSetting> CreateDefaultSettings()
        {
            return new List<StoreSetting>
            {
                new StoreSetting
                {
                    SettingID = 1,
                    SettingKey = "StoreName",
                    SettingValue = "TechStore Premium",
                    SettingType = "Text",
                    Description = "The name of your store displayed to customers",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 2,
                    SettingKey = "PrimaryColor",
                    SettingValue = "#2196F3",
                    SettingType = "Color",
                    Description = "Primary theme color for the store",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 3,
                    SettingKey = "TaxRate",
                    SettingValue = "19",
                    SettingType = "Number",
                    Description = "VAT percentage applied to all orders",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 4,
                    SettingKey = "AllowGuestCheckout",
                    SettingValue = "true",
                    SettingType = "Boolean",
                    Description = "Allow customers to checkout without creating an account",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 5,
                    SettingKey = "FreeShippingThreshold",
                    SettingValue = "200",
                    SettingType = "Number",
                    Description = "Order total above which shipping is free",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 6,
                    SettingKey = "ContactEmail",
                    SettingValue = "contact@techstore.com",
                    SettingType = "Text",
                    Description = "Contact email displayed to customers",
                    LastUpdated = DateTime.Now
                },
                new StoreSetting
                {
                    SettingID = 7,
                    SettingKey = "ShowOutOfStock",
                    SettingValue = "false",
                    SettingType = "Boolean",
                    Description = "Show out of stock products in the catalog",
                    LastUpdated = DateTime.Now
                }
            };
        }

        // NotifyShortcutProperties - Notifica proprietatile shortcut
        private void NotifyShortcutProperties()
        {
            OnPropertyChanged(nameof(StoreName));
            OnPropertyChanged(nameof(PrimaryColor));
            OnPropertyChanged(nameof(TaxRate));
            OnPropertyChanged(nameof(AllowGuestCheckout));
            OnPropertyChanged(nameof(FreeShippingThreshold));
        }

        // IMPLEMENTARI COMENZI

        // ExecuteSave - Salveaza modificarile
        private void ExecuteSave(object parameter)
        {
            if (!HasUnsavedChanges)
                return;

            IsSaving = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                // In productie: _settingsService.SaveSettings(Settings);

                // Actualizam valorile originale
                foreach (var setting in Settings)
                {
                    _originalSettings[setting.SettingKey] = setting.SettingValue;
                }

                HasUnsavedChanges = false;
                SuccessMessage = "Settings saved successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to save settings: " + ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

        // CanExecuteSave - Se poate salva?
        private bool CanExecuteSave(object parameter)
        {
            return HasUnsavedChanges && !IsSaving && !IsLoading;
        }

        // ExecuteReset - Anuleaza modificarile
        private void ExecuteReset(object parameter)
        {
            if (!HasUnsavedChanges)
                return;

            // Restauram valorile originale
            foreach (var setting in Settings)
            {
                if (_originalSettings.TryGetValue(setting.SettingKey, out string originalValue))
                {
                    setting.SettingValue = originalValue;
                }
            }

            HasUnsavedChanges = false;
            NotifyShortcutProperties();
        }

        // CanExecuteReset - Se poate reseta?
        private bool CanExecuteReset(object parameter)
        {
            return HasUnsavedChanges;
        }

        // ExecuteResetToDefault - Reseteaza la valorile default
        private void ExecuteResetToDefault(object parameter)
        {
            // In productie, am afisa dialog de confirmare
            // var result = MessageBox.Show("Reset all settings to default?", "Confirm", MessageBoxButton.YesNo);
            // if (result != MessageBoxResult.Yes) return;

            var defaultSettings = CreateDefaultSettings();

            foreach (var defaultSetting in defaultSettings)
            {
                var existingSetting = Settings.FirstOrDefault(s => s.SettingKey == defaultSetting.SettingKey);

                if (existingSetting != null)
                {
                    existingSetting.SettingValue = defaultSetting.SettingValue;
                }
            }

            HasUnsavedChanges = true;
            NotifyShortcutProperties();
        }

        // ExecuteRefresh - Reincarca datele
        private void ExecuteRefresh(object parameter)
        {
            // Avertizam daca sunt modificari nesalvate
            if (HasUnsavedChanges)
            {
                // In productie: dialog de confirmare
                // var result = MessageBox.Show("Unsaved changes will be lost. Continue?", ...);
            }

            LoadData();
        }

        // CanExecuteRefresh - Se poate face refresh?
        private bool CanExecuteRefresh(object parameter)
        {
            return !IsLoading && !IsSaving;
        }

        // ExecuteAddSetting - Adauga o setare noua
        private void ExecuteAddSetting(object parameter)
        {
            // In productie: dialog pentru a cere key, value, type
            // var dialog = new AddSettingDialog();
            // if (dialog.ShowDialog() == true)
            // {
            //     Settings.Add(dialog.Setting);
            //     HasUnsavedChanges = true;
            // }

            var newSetting = new StoreSetting
            {
                SettingID = Settings.Count + 1,
                SettingKey = "NewSetting",
                SettingValue = "",
                SettingType = "Text",
                Description = "New setting",
                LastUpdated = DateTime.Now
            };

            Settings.Add(newSetting);
            SelectedSetting = newSetting;
            HasUnsavedChanges = true;

            OnPropertyChanged(nameof(TotalSettingsCount));
        }

        // ExecuteDeleteSetting - Sterge setarea selectata
        private void ExecuteDeleteSetting(object parameter)
        {
            var setting = parameter as StoreSetting ?? SelectedSetting;

            if (setting == null)
                return;

            Settings.Remove(setting);
            _originalSettings.Remove(setting.SettingKey);
            SelectedSetting = null;
            HasUnsavedChanges = true;

            OnPropertyChanged(nameof(TotalSettingsCount));
        }

        // CanExecuteDeleteSetting - Se poate sterge?
        private bool CanExecuteDeleteSetting(object parameter)
        {
            return (parameter as StoreSetting ?? SelectedSetting) != null;
        }
    }
}
