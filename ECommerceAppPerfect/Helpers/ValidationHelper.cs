using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ECommerceAppPerfect.Helpers
{
    // CLASA VALIDATIONHELPER - Utilitare pentru validarea datelor
    //
    // CE FACE ACEASTA CLASA?
    // Contine metode STATICE pentru validarea diferitelor tipuri de date:
    // - Email
    // - Telefon
    // - Pret
    // - String-uri (lungime, format)
    // - etc.
    //
    // DE CE CENTRALIZAT?
    // 1. CONSISTENTA: Aceleasi reguli de validare peste tot
    // 2. REUTILIZARE: O metoda, folosita in mai multe locuri
    // 3. MENTINERE: Schimbi regula intr-un singur loc
    // 4. TESTARE: Usor de testat izolat
    //
    // CUM SE FOLOSESTE?
    // string error = ValidationHelper.ValidateEmail(email);
    // if (!string.IsNullOrEmpty(error))
    // {
    //     // Afiseaza eroarea
    // }
    //
    // SAU cu tuple:
    // var (isValid, error) = ValidationHelper.ValidateProduct(product);
    public static class ValidationHelper
    {
        // METODA ValidateEmail - Verifica validitatea unui email
        //
        // PARAMETRU: email - Adresa de email
        //
        // RETURNEAZA:
        // - null/empty daca e valid
        // - Mesaj de eroare daca nu e valid
        //
        // REGULI:
        // - Nu poate fi gol
        // - Trebuie sa aiba format: ceva@ceva.ceva
        // - Domeniu valid (nu @.com)
        public static string ValidateEmail(string email)
        {
            // Verificare empty
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Email is required.";
            }

            // Verificare format cu Regex
            // Pattern explicat:
            // ^[^@\s]+ = incepe cu unul sau mai multe caractere care NU sunt @ sau spatiu
            // @        = trebuie sa aiba @
            // [^@\s]+  = dupa @ trebuie caractere (nu alt @, nu spatii)
            // \.       = trebuie sa aiba punct
            // [^@\s]+$ = dupa punct, mai multe caractere pana la sfarsit
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (!Regex.IsMatch(email, emailPattern))
            {
                return "Please enter a valid email address.";
            }

            // Verificare lungime maxima (conform RFC)
            if (email.Length > 254)
            {
                return "Email address is too long.";
            }

            return null; // Valid
        }

        // METODA ValidatePhone - Verifica validitatea unui numar de telefon
        //
        // PARAMETRU: phone - Numarul de telefon
        //
        // RETURNEAZA:
        // - null/empty daca e valid
        // - Mesaj de eroare daca nu e valid
        //
        // ACCEPTA:
        // - Format romanesc: 07xxxxxxxx, +407xxxxxxxx
        // - Format international: +1234567890
        // - Cu sau fara spatii/dash-uri
        public static string ValidatePhone(string phone)
        {
            // Telefonul e optional, deci gol e OK
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            // Scoatem caracterele de formatare
            string cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");

            // Verificam ca are doar cifre (si eventual + la inceput)
            if (!Regex.IsMatch(cleanPhone, @"^\+?[0-9]+$"))
            {
                return "Phone number can only contain digits, spaces, and dashes.";
            }

            // Verificare lungime (fara +)
            string digitsOnly = cleanPhone.TrimStart('+');

            if (digitsOnly.Length < 6)
            {
                return "Phone number is too short.";
            }

            if (digitsOnly.Length > 15)
            {
                return "Phone number is too long.";
            }

            return null; // Valid
        }

        // METODA ValidatePrice - Verifica validitatea unui pret
        //
        // PARAMETRU: price - Pretul ca decimal
        //
        // RETURNEAZA: Mesaj de eroare sau null daca e valid
        //
        // REGULI:
        // - Trebuie sa fie >= 0
        // - Maxim 2 zecimale
        // - Maxim 1,000,000 (limita rezonabila)
        public static string ValidatePrice(decimal price)
        {
            if (price < 0)
            {
                return "Price cannot be negative.";
            }

            if (price > 1000000)
            {
                return "Price seems unreasonably high. Please verify.";
            }

            // Verificare zecimale (maxim 2)
            // Inmultim cu 100 si verificam ca e intreg
            decimal multiplied = price * 100;
            if (multiplied != Math.Floor(multiplied))
            {
                return "Price can have at most 2 decimal places.";
            }

            return null; // Valid
        }

        // METODA ValidateQuantity - Verifica validitatea unei cantitati
        //
        // PARAMETRU: quantity - Cantitatea ca intreg
        //
        // RETURNEAZA: Mesaj de eroare sau null daca e valid
        public static string ValidateQuantity(int quantity)
        {
            if (quantity < 0)
            {
                return "Quantity cannot be negative.";
            }

            if (quantity > 10000)
            {
                return "Quantity seems unreasonably high. Please verify.";
            }

            return null; // Valid
        }

        // METODA ValidateRequired - Verifica ca un string nu e gol
        //
        // PARAMETRI:
        // - value: Valoarea de verificat
        // - fieldName: Numele campului (pentru mesaj)
        //
        // RETURNEAZA: Mesaj de eroare sau null
        public static string ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return $"{fieldName} is required.";
            }

            return null;
        }

        // METODA ValidateStringLength - Verifica lungimea unui string
        //
        // PARAMETRI:
        // - value: String-ul de verificat
        // - fieldName: Numele campului
        // - minLength: Lungime minima (0 pentru fara minim)
        // - maxLength: Lungime maxima
        //
        // RETURNEAZA: Mesaj de eroare sau null
        public static string ValidateStringLength(string value, string fieldName,
            int minLength = 0, int maxLength = int.MaxValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (minLength > 0)
                    return $"{fieldName} is required.";
                return null;
            }

            if (value.Length < minLength)
            {
                return $"{fieldName} must be at least {minLength} characters.";
            }

            if (value.Length > maxLength)
            {
                return $"{fieldName} cannot exceed {maxLength} characters.";
            }

            return null;
        }

        // METODA ValidateUsername - Verifica validitatea unui username
        //
        // REGULI:
        // - 3-50 caractere
        // - Doar litere, cifre, underscore
        // - Nu poate incepe cu cifra
        public static string ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Username is required.";
            }

            if (username.Length < 3)
            {
                return "Username must be at least 3 characters.";
            }

            if (username.Length > 50)
            {
                return "Username cannot exceed 50 characters.";
            }

            // Verificare format
            if (!Regex.IsMatch(username, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
            {
                return "Username can only contain letters, numbers, and underscores, and must start with a letter.";
            }

            return null;
        }

        // METODA ValidateProduct - Valideaza un obiect Product complet
        //
        // RETURNEAZA: Lista de erori (goala daca totul e valid)
        //
        // VALIDARI:
        // - Nume produs: obligatoriu, 2-200 caractere
        // - Pret: >= 0
        // - Categorie: obligatorie (CategoryID > 0)
        public static List<string> ValidateProduct(string productName, decimal price, int categoryId)
        {
            var errors = new List<string>();

            // Validare nume
            var nameError = ValidateStringLength(productName, "Product name", 2, 200);
            if (nameError != null)
                errors.Add(nameError);

            // Validare pret
            var priceError = ValidatePrice(price);
            if (priceError != null)
                errors.Add(priceError);

            // Validare categorie
            if (categoryId <= 0)
                errors.Add("Please select a category.");

            return errors;
        }

        // METODA ValidateUser - Valideaza datele de inregistrare user
        //
        // RETURNEAZA: Lista de erori
        public static List<string> ValidateUserRegistration(
            string username, string email, string password, string confirmPassword)
        {
            var errors = new List<string>();

            // Username
            var usernameError = ValidateUsername(username);
            if (usernameError != null)
                errors.Add(usernameError);

            // Email
            var emailError = ValidateEmail(email);
            if (emailError != null)
                errors.Add(emailError);

            // Password
            var (isValid, passwordError) = PasswordHelper.ValidatePasswordStrength(password);
            if (!isValid)
                errors.Add(passwordError);

            // Confirm password
            if (password != confirmPassword)
                errors.Add("Passwords do not match.");

            return errors;
        }

        // METODA HasErrors - Verifica rapid daca o lista de erori are continut
        public static bool HasErrors(List<string> errors)
        {
            return errors != null && errors.Count > 0;
        }

        // METODA JoinErrors - Combina erorile intr-un singur string
        public static string JoinErrors(List<string> errors, string separator = "\n")
        {
            if (errors == null || errors.Count == 0)
                return string.Empty;

            return string.Join(separator, errors);
        }
    }
}
