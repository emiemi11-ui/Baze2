using System.Windows.Controls;

namespace ECommerceAppPerfect.Views
{
    // CLASA LOGINVIEW - Code-behind pentru pagina de login
    //
    // CE ESTE CODE-BEHIND?
    // Fisierul .xaml.cs asociat unui fisier XAML
    // Contine logica C# pentru UI (event handlers, etc.)
    //
    // IN MVVM:
    // Code-behind ar trebui sa fie MINIMAL
    // Toata logica e in ViewModel
    // UI e definit in XAML cu bindings
    //
    // ACEASTA CLASA:
    // Contine doar constructorul cu InitializeComponent()
    // Nu are niciun event handler - totul e prin Commands
    //
    // PARTIAL CLASS:
    // Clasa e "partial" pentru ca e impartita in doua:
    // 1. LoginView.xaml.cs (acest fisier) - cod manual
    // 2. LoginView.g.cs - generat automat din XAML
    public partial class LoginView : UserControl
    {
        // CONSTRUCTORUL
        //
        // InitializeComponent():
        // Incarca si parseaza XAML-ul
        // Creeaza toate controalele definite in XAML
        // Seteaza proprietatile si binding-urile
        //
        // TREBUIE APELAT INTOTDEAUNA!
        public LoginView()
        {
            InitializeComponent();
        }

        // AICI NU AVEM EVENT HANDLERS
        //
        // In MVVM, nu folosim event handlers in code-behind
        // In schimb, folosim:
        // - Commands pentru actiuni (LoginCommand)
        // - Bindings pentru date (Username, Password)
        // - Converters pentru transformari (BoolToVisibility)
        //
        // Daca ai nevoie de logica UI complexa:
        // - Behaviors (pattern pentru a adauga logica la controale)
        // - Attached Properties (ca PasswordHelper)
    }
}
