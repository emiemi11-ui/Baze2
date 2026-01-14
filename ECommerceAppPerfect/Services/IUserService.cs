using System.Collections.Generic;
using ECommerceAppPerfect.Models;

namespace ECommerceAppPerfect.Services
{
    // INTERFATA IUSERSERVICE - Contract pentru serviciul de utilizatori
    //
    // CE ESTE O INTERFATA?
    // O interfata defineste CE poate face o clasa, nu CUM
    // Este un CONTRACT pe care implementarea trebuie sa il respecte
    //
    // DE CE INTERFETE PENTRU SERVICII?
    //
    // 1. DEPENDENCY INVERSION (Principiul D din SOLID)
    // ViewModels depind de INTERFETE, nu de implementari concrete
    // Asta permite schimbarea implementarii fara a modifica ViewModels
    //
    // 2. TESTABILITATE
    // In teste, poti crea MOCK-uri care implementeaza interfata
    // Astfel testezi ViewModel-ul fara baza de date reala
    //
    // 3. FLEXIBILITATE
    // Poti avea mai multe implementari:
    // - UserService - foloseste Entity Framework (productie)
    // - UserServiceMock - returneaza date hardcodate (teste)
    // - UserServiceApi - foloseste REST API (microservices)
    //
    // EXEMPLU FOLOSIRE:
    // // In ViewModel:
    // public LoginViewModel(IUserService userService)
    // {
    //     _userService = userService;
    // }
    //
    // // In productie:
    // new LoginViewModel(new UserService())
    //
    // // In teste:
    // new LoginViewModel(new MockUserService())
    public interface IUserService
    {
        // AUTENTIFICARE

        // Authenticate - Autentifica utilizatorul cu username si parola
        //
        // PARAMETRI:
        // - username: Numele de utilizator
        // - password: Parola IN CLAR (va fi hash-uita intern)
        //
        // RETURNEAZA:
        // - User daca autentificarea a reusit
        // - null daca username/parola gresita sau user inactiv
        //
        // NOTA: Metoda TREBUIE sa:
        // 1. Hash-uiasca parola primita
        // 2. Compare cu hash-ul din DB
        // 3. Verifice ca IsActive = true
        User Authenticate(string username, string password);

        // AuthenticateByEmail - Autentificare cu email in loc de username
        //
        // Unii utilizatori prefera sa foloseasca email-ul
        User AuthenticateByEmail(string email, string password);

        // CRUD OPERATIONS

        // GetAllUsers - Returneaza toti utilizatorii
        //
        // RETURNEAZA: Lista tuturor utilizatorilor
        // INCLUDE: Doar utilizatorii activi sau toti? (vezi implementare)
        List<User> GetAllUsers();

        // GetUserById - Returneaza un utilizator dupa ID
        //
        // PARAMETRU: userId - ID-ul utilizatorului
        // RETURNEAZA: User sau null daca nu exista
        User GetUserById(int userId);

        // GetUserByUsername - Returneaza utilizatorul cu username-ul dat
        //
        // PARAMETRU: username - Numele de utilizator
        // RETURNEAZA: User sau null daca nu exista
        User GetUserByUsername(string username);

        // GetUserByEmail - Returneaza utilizatorul cu email-ul dat
        //
        // PARAMETRU: email - Adresa de email
        // RETURNEAZA: User sau null daca nu exista
        User GetUserByEmail(string email);

        // CreateUser - Creeaza un utilizator nou
        //
        // PARAMETRI:
        // - user: Obiectul User cu datele
        // - password: Parola IN CLAR (va fi hash-uita)
        //
        // RETURNEAZA:
        // - true daca s-a creat cu succes
        // - false daca a esuat (username/email duplicat, etc.)
        //
        // OPERATII:
        // 1. Valideaza datele (username unic, email unic)
        // 2. Hash-uieste parola
        // 3. Seteaza CreatedDate
        // 4. Salveaza in DB
        bool CreateUser(User user, string password);

        // UpdateUser - Actualizeaza datele unui utilizator
        //
        // PARAMETRU: user - User-ul cu datele modificate
        //
        // RETURNEAZA: true daca a reusit
        //
        // NOTA: NU modifica parola! Pentru parola, foloseste ChangePassword
        bool UpdateUser(User user);

        // DeleteUser - Sterge (dezactiveaza) un utilizator
        //
        // PARAMETRU: userId - ID-ul utilizatorului
        //
        // RETURNEAZA: true daca a reusit
        //
        // NOTA: Soft delete - seteaza IsActive = false
        bool DeleteUser(int userId);

        // PAROLE

        // ChangePassword - Schimba parola utilizatorului
        //
        // PARAMETRI:
        // - userId: ID-ul utilizatorului
        // - currentPassword: Parola curenta (pentru verificare)
        // - newPassword: Noua parola
        //
        // RETURNEAZA: true daca a reusit
        bool ChangePassword(int userId, string currentPassword, string newPassword);

        // ResetPassword - Reseteaza parola (fara verificare)
        //
        // PARAMETRI:
        // - userId: ID-ul utilizatorului
        // - newPassword: Noua parola
        //
        // RETURNEAZA: true daca a reusit
        //
        // NOTA: Folosit de admin sau pentru "forgot password"
        bool ResetPassword(int userId, string newPassword);

        // INTEROGARI SPECIFICE

        // GetUsersByRole - Returneaza utilizatorii cu un anumit rol
        //
        // PARAMETRU: role - "StoreOwner", "Customer", sau "CustomerService"
        //
        // RETURNEAZA: Lista utilizatorilor cu acel rol
        List<User> GetUsersByRole(string role);

        // GetCustomers - Returneaza toti clientii
        //
        // Shortcut pentru GetUsersByRole("Customer")
        List<User> GetCustomers();

        // GetStoreOwners - Returneaza toti proprietarii
        List<User> GetStoreOwners();

        // GetCustomerServiceAgents - Returneaza toti agentii
        List<User> GetCustomerServiceAgents();

        // VALIDARI

        // IsUsernameAvailable - Verifica daca username-ul e disponibil
        //
        // PARAMETRU: username - Username-ul de verificat
        //
        // RETURNEAZA: true daca e disponibil (nu exista)
        bool IsUsernameAvailable(string username);

        // IsEmailAvailable - Verifica daca email-ul e disponibil
        //
        // PARAMETRU: email - Email-ul de verificat
        //
        // RETURNEAZA: true daca e disponibil
        bool IsEmailAvailable(string email);
    }
}
