using System;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceAppPerfect.Helpers
{
    // CLASA PASSWORDHELPER - Utilitare pentru gestionarea parolelor
    //
    // CE FACE ACEASTA CLASA?
    // Gestioneaza SECURITATEA parolelor:
    // 1. Hash-uirea parolelor (pentru stocare in DB)
    // 2. Verificarea parolelor (pentru autentificare)
    // 3. Validarea puterii parolelor (pentru inregistrare)
    //
    // DE CE HASH SI NU ENCRYPTION?
    //
    // ENCRYPTION (criptare):
    // - Bidirectionala: poti cripta si decripta
    // - Daca cineva obtine cheia, poate citi toate parolele
    // - NU e recomandat pentru parole
    //
    // HASHING:
    // - Unidirectional: nu poti reveni la parola originala
    // - Chiar daca DB-ul e compromis, parolele sunt sigure
    // - Standard industry pentru parole
    //
    // CUM FUNCTIONEAZA VERIFICAREA?
    // 1. La INREGISTRARE: hash(parola) -> stocat in DB
    // 2. La LOGIN: hash(parola_introdusa) -> comparam cu hash-ul din DB
    // 3. Daca hash-urile sunt egale, parola e corecta
    //
    // ALGORITMUL FOLOSIT: SHA-256
    //
    // DE CE SHA-256?
    // - Standard, bine testat
    // - Rapid pentru verificare
    // - Rezistent la coliziuni
    //
    // NOTA: Pentru PRODUCTIE, se recomanda:
    // - bcrypt, scrypt sau Argon2 (algoritmi de parole dedicati)
    // - Sunt mai lenti (intentionat!) pentru a preveni brute force
    // - SHA-256 e OK pentru proiecte academice
    public static class PasswordHelper
    {
        // METODA HashPassword - Creeaza hash SHA-256 din parola
        //
        // PARAMETRU: password - Parola in clar
        //
        // RETURNEAZA: Hash-ul ca string HEX (64 caractere)
        //
        // EXEMPLU:
        // "password123" -> "ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f"
        //
        // FLOW:
        // 1. Convertim string-ul in bytes (UTF-8)
        // 2. Calculam hash-ul SHA-256
        // 3. Convertim bytes la string HEX
        public static string HashPassword(string password)
        {
            // Validare input
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            // Cream instanta SHA256
            // "using" garanteaza ca resursa e eliberata dupa utilizare
            using (SHA256 sha256 = SHA256.Create())
            {
                // STEP 1: Convertim parola la bytes
                // UTF-8 e encodingul standard pentru text Unicode
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                // STEP 2: Calculam hash-ul
                // ComputeHash() returneaza un array de 32 bytes (256 biti)
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);

                // STEP 3: Convertim la string HEX
                // Fiecare byte devine 2 caractere HEX (0-9, a-f)
                // 32 bytes -> 64 caractere
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    // x2 = format HEX cu 2 caractere, lowercase
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        // METODA VerifyPassword - Verifica daca parola corespunde hash-ului
        //
        // PARAMETRI:
        // - password: Parola introdusa de utilizator (in clar)
        // - hashedPassword: Hash-ul stocat in baza de date
        //
        // RETURNEAZA:
        // - true: Parola e corecta
        // - false: Parola e gresita
        //
        // FLOW:
        // 1. Hash-uim parola introdusa
        // 2. Comparam cu hash-ul din DB
        // 3. Daca sunt egale, parola e corecta
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // Validare input
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            {
                return false;
            }

            // Hash-uim parola introdusa
            string inputHash = HashPassword(password);

            // Comparam hash-urile (case-insensitive pentru HEX)
            // OrdinalIgnoreCase e mai rapid decat ToLower()
            return string.Equals(inputHash, hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        // METODA ValidatePasswordStrength - Verifica puterea parolei
        //
        // PARAMETRU: password - Parola de verificat
        //
        // RETURNEAZA:
        // Tuple cu (isValid, errorMessage)
        // isValid = true daca parola e suficient de puternica
        // errorMessage = descrierea problemei daca nu e valida
        //
        // REGULI:
        // - Minim 6 caractere
        // - Cel putin o litera
        // - Cel putin o cifra
        //
        // NOTA: Pentru productie, regulile ar fi mai stricte:
        // - Minim 8-12 caractere
        // - Litere mari si mici
        // - Cifre
        // - Caractere speciale
        public static (bool isValid, string errorMessage) ValidatePasswordStrength(string password)
        {
            // Verificare null/empty
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password is required.");
            }

            // Verificare lungime minima
            if (password.Length < 6)
            {
                return (false, "Password must be at least 6 characters long.");
            }

            // Verificare ca are cel putin o litera
            bool hasLetter = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsLetter(c))
                    hasLetter = true;
                if (char.IsDigit(c))
                    hasDigit = true;
            }

            if (!hasLetter)
            {
                return (false, "Password must contain at least one letter.");
            }

            if (!hasDigit)
            {
                return (false, "Password must contain at least one digit.");
            }

            // Toate verificarile au trecut
            return (true, string.Empty);
        }

        // METODA GenerateRandomPassword - Genereaza o parola aleatoare
        //
        // PARAMETRU: length - Lungimea parolei (default: 12)
        //
        // RETURNEAZA: Parola aleatoare
        //
        // FOLOSIRE:
        // - Reset parola (se trimite pe email)
        // - Parola temporara pentru conturi noi
        //
        // CARACTERELE INCLUSE:
        // - Litere mari (A-Z)
        // - Litere mici (a-z)
        // - Cifre (0-9)
        // - Caractere speciale (!@#$%...)
        public static string GenerateRandomPassword(int length = 12)
        {
            // Caracterele disponibile
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            // Toate caracterele combinate
            string allChars = uppercase + lowercase + digits + special;

            // Random cryptographic (mai sigur decat Random())
            using (var rng = new RNGCryptoServiceProvider())
            {
                StringBuilder password = new StringBuilder();

                // Asiguram ca avem cel putin unul din fiecare categorie
                password.Append(GetRandomChar(uppercase, rng));
                password.Append(GetRandomChar(lowercase, rng));
                password.Append(GetRandomChar(digits, rng));
                password.Append(GetRandomChar(special, rng));

                // Completam restul cu caractere aleatorii
                for (int i = 4; i < length; i++)
                {
                    password.Append(GetRandomChar(allChars, rng));
                }

                // Amestecam caracterele (pentru ca primele 4 sunt predictibile)
                return ShuffleString(password.ToString(), rng);
            }
        }

        // METODA HELPER GetRandomChar - Selecteaza un caracter aleatoriu
        private static char GetRandomChar(string chars, RNGCryptoServiceProvider rng)
        {
            byte[] randomByte = new byte[1];
            rng.GetBytes(randomByte);

            // Modulo pentru a fi in range
            int index = randomByte[0] % chars.Length;
            return chars[index];
        }

        // METODA HELPER ShuffleString - Amesteca caracterele unui string
        private static string ShuffleString(string str, RNGCryptoServiceProvider rng)
        {
            char[] array = str.ToCharArray();

            // Fisher-Yates shuffle
            for (int i = array.Length - 1; i > 0; i--)
            {
                byte[] randomByte = new byte[1];
                rng.GetBytes(randomByte);
                int j = randomByte[0] % (i + 1);

                // Swap
                char temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return new string(array);
        }
    }
}
