using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using ECommerceAppPerfect.Helpers;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // CLASA USERSERVICE - Implementarea serviciului de utilizatori
    //
    // CE FACE ACEASTA CLASA?
    // Implementeaza toate operatiile legate de utilizatori:
    // - Autentificare (login)
    // - CRUD (creare, citire, actualizare, stergere)
    // - Gestionare parole
    // - Interogari specifice
    //
    // CUM FUNCTIONEAZA?
    // Foloseste Entity Framework pentru acces la baza de date
    // Fiecare metoda creeaza un DbContext NOU pentru operatie
    //
    // DE CE CONTEXT NOU PENTRU FIECARE OPERATIE?
    // 1. DbContext NU e thread-safe
    // 2. Conexiunile se inchid automat cu "using"
    // 3. Evitam probleme de tracking (entitati modificate accidental)
    //
    // ALTERNATIVA: Dependency Injection cu lifetime Scoped
    // In aplicatii web, ai injecta DbContext cu AddScoped
    // Pentru WPF, pattern-ul "context per operation" e mai sigur
    //
    // IMPLEMENTEAZA: IUserService
    // IMPLEMENTEAZA: IDisposable (pentru cleanup)
    public class UserService : IUserService, IDisposable
    {
        // FLAG DISPOSED
        // Pentru a sti daca obiectul a fost deja disposed
        private bool _disposed = false;

        // METODA HELPER GetContext - Creeaza un nou DbContext
        //
        // DE CE METODA SEPARATA?
        // Pentru a nu repeta "new ECommerceEntities()" in fiecare metoda
        // Si pentru a avea un singur loc de configurare
        private ECommerceEntities GetContext()
        {
            return new ECommerceEntities();
        }

        // AUTENTIFICARE

        // Authenticate - Autentifica utilizatorul
        //
        // FLOW:
        // 1. Cauta utilizatorul dupa username
        // 2. Verifica ca exista si e activ
        // 3. Hash-uieste parola introdusa
        // 4. Compara cu hash-ul din DB
        // 5. Returneaza User sau null
        public User Authenticate(string username, string password)
        {
            // Validare input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            using (var context = GetContext())
            {
                // Cautam utilizatorul dupa username
                // FirstOrDefault returneaza primul rezultat sau null
                var user = context.Users
                    .FirstOrDefault(u => u.Username == username && u.IsActive);

                // Nu exista sau e inactiv
                if (user == null)
                    return null;

                // Verificam parola
                // PasswordHelper.VerifyPassword hash-uieste parola si compara
                if (!PasswordHelper.VerifyPassword(password, user.HashedPassword))
                    return null;

                // Autentificare reusita!
                return user;
            }
        }

        // AuthenticateByEmail - Autentificare cu email
        //
        // Acelasi flow ca Authenticate, dar cauta dupa email
        public User AuthenticateByEmail(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            using (var context = GetContext())
            {
                var user = context.Users
                    .FirstOrDefault(u => u.Email == email && u.IsActive);

                if (user == null)
                    return null;

                if (!PasswordHelper.VerifyPassword(password, user.HashedPassword))
                    return null;

                return user;
            }
        }

        // CRUD - READ

        // GetAllUsers - Returneaza toti utilizatorii activi
        //
        // LINQ to Entities:
        // .Where() filtreaza rezultatele
        // .OrderBy() sorteaza
        // .ToList() executa query-ul si returneaza lista
        public List<User> GetAllUsers()
        {
            using (var context = GetContext())
            {
                return context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToList();
            }
        }

        // GetUserById - Returneaza utilizatorul cu ID-ul dat
        //
        // .Find() e optimizat pentru cautare dupa Primary Key
        // Mai rapid decat .FirstOrDefault(u => u.UserID == id)
        public User GetUserById(int userId)
        {
            using (var context = GetContext())
            {
                return context.Users.Find(userId);
            }
        }

        // GetUserByUsername - Returneaza utilizatorul cu username-ul dat
        public User GetUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            using (var context = GetContext())
            {
                return context.Users
                    .FirstOrDefault(u => u.Username == username);
            }
        }

        // GetUserByEmail - Returneaza utilizatorul cu email-ul dat
        public User GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            using (var context = GetContext())
            {
                return context.Users
                    .FirstOrDefault(u => u.Email == email);
            }
        }

        // CRUD - CREATE

        // CreateUser - Creeaza un utilizator nou
        //
        // FLOW:
        // 1. Valideaza unicitatea username-ului
        // 2. Valideaza unicitatea email-ului
        // 3. Hash-uieste parola
        // 4. Seteaza valori default
        // 5. Salveaza in DB
        public bool CreateUser(User user, string password)
        {
            // Validare input
            if (user == null || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Verificam unicitatea username-ului
                    if (context.Users.Any(u => u.Username == user.Username))
                        return false;

                    // Verificam unicitatea email-ului
                    if (context.Users.Any(u => u.Email == user.Email))
                        return false;

                    // Hash-uim parola
                    user.HashedPassword = PasswordHelper.HashPassword(password);

                    // Setam valori default
                    user.CreatedDate = DateTime.Now;
                    user.IsActive = true;

                    // Adaugam in context si salvam
                    context.Users.Add(user);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                // Log exception in productie
                return false;
            }
        }

        // CRUD - UPDATE

        // UpdateUser - Actualizeaza datele unui utilizator
        //
        // NOTA: NU modifica parola! HashedPassword e ignorat
        public bool UpdateUser(User user)
        {
            if (user == null)
                return false;

            try
            {
                using (var context = GetContext())
                {
                    // Gasim utilizatorul existent
                    var existingUser = context.Users.Find(user.UserID);

                    if (existingUser == null)
                        return false;

                    // Actualizam doar campurile editabile
                    // NU modificam: UserID, HashedPassword, CreatedDate
                    existingUser.Email = user.Email;
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Address = user.Address;
                    existingUser.UserRole = user.UserRole;
                    existingUser.IsActive = user.IsActive;

                    // SaveChanges() detecteaza modificarile si le salveaza
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // CRUD - DELETE

        // DeleteUser - Soft delete (dezactiveaza)
        //
        // Nu stergem fizic pentru ca:
        // - Utilizatorul poate avea comenzi asociate
        // - Pastram istoricul pentru audit
        // - Poate fi reactivat
        public bool DeleteUser(int userId)
        {
            try
            {
                using (var context = GetContext())
                {
                    var user = context.Users.Find(userId);

                    if (user == null)
                        return false;

                    // Soft delete - doar dezactivam
                    user.IsActive = false;

                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // PAROLE

        // ChangePassword - Schimba parola cu verificare
        //
        // FLOW:
        // 1. Verifica parola curenta
        // 2. Valideaza noua parola
        // 3. Hash-uieste si salveaza
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var user = context.Users.Find(userId);

                    if (user == null)
                        return false;

                    // Verificam parola curenta
                    if (!PasswordHelper.VerifyPassword(currentPassword, user.HashedPassword))
                        return false;

                    // Setam noua parola (hash-uita)
                    user.HashedPassword = PasswordHelper.HashPassword(newPassword);

                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ResetPassword - Reseteaza parola fara verificare
        //
        // Folosit de admin sau pentru "forgot password"
        public bool ResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;

            try
            {
                using (var context = GetContext())
                {
                    var user = context.Users.Find(userId);

                    if (user == null)
                        return false;

                    user.HashedPassword = PasswordHelper.HashPassword(newPassword);

                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // INTEROGARI SPECIFICE

        // GetUsersByRole - Filtreaza dupa rol
        public List<User> GetUsersByRole(string role)
        {
            using (var context = GetContext())
            {
                return context.Users
                    .Where(u => u.UserRole == role && u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToList();
            }
        }

        // Shortcut-uri pentru roluri specifice
        public List<User> GetCustomers() => GetUsersByRole("Customer");
        public List<User> GetStoreOwners() => GetUsersByRole("StoreOwner");
        public List<User> GetCustomerServiceAgents() => GetUsersByRole("CustomerService");

        // VALIDARI

        // IsUsernameAvailable - Verifica disponibilitatea
        public bool IsUsernameAvailable(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            using (var context = GetContext())
            {
                // Any() returneaza true daca exista cel putin un rezultat
                // Negam pentru a returna true daca NU exista
                return !context.Users.Any(u => u.Username == username);
            }
        }

        // IsEmailAvailable - Verifica disponibilitatea email-ului
        public bool IsEmailAvailable(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            using (var context = GetContext())
            {
                return !context.Users.Any(u => u.Email == email);
            }
        }

        // DISPOSE PATTERN

        // Dispose - Elibereaza resursele
        //
        // In cazul nostru, nu avem resurse de eliberat
        // (context-urile se elibereaza cu "using")
        // Dar implementam pattern-ul pentru consistenta
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
                    // (nu avem in acest caz)
                }

                _disposed = true;
            }
        }
    }
}
