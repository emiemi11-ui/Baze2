using System.Windows;

namespace ECommerceAppPerfect
{
    // CLASA MAINWINDOW - Code-behind pentru fereastra principala
    //
    // CE ESTE CODE-BEHIND?
    // Code-behind este clasa C# asociata unui fisier XAML
    // Contine logica pentru UI (event handlers, animations, etc.)
    //
    // IN MVVM:
    // Code-behind ar trebui sa fie MINIMAL
    // Toata logica e in ViewModel, UI e in XAML
    // Code-behind contine doar ce nu se poate face altfel
    //
    // ACEASTA CLASA:
    // Contine doar constructorul cu InitializeComponent()
    // E exemplul perfect de code-behind MVVM - aproape gol
    //
    // DE CE partial?
    // Clasa e impartita in doua parti:
    // 1. Aceasta (MainWindow.xaml.cs) - scrisa de noi
    // 2. MainWindow.g.cs - generata automat din XAML
    // "partial" permite combinarea lor la compilare
    public partial class MainWindow : Window
    {
        // CONSTRUCTORUL - Initializeaza fereastra
        //
        // CE FACE InitializeComponent()?
        // Incarca si parseaza XAML-ul
        // Creeaza toate controalele definite in XAML
        // Seteaza proprietatile si binding-urile
        //
        // TREBUIE APELAT INTOTDEAUNA!
        // Fara InitializeComponent(), fereastra ar fi goala
        public MainWindow()
        {
            InitializeComponent();
        }

        // AICI AR PUTEA FI:
        // - Event handlers pentru evenimente care nu suporta Commands
        // - Logica de UI complexa (drag & drop, animations custom)
        // - Interactiune cu Win32 API
        //
        // DAR IN MVVM:
        // Incercam sa evitam code-behind
        // Folosim Commands, Behaviors, Triggers
        // Aceasta clasa ramane (aproape) goala
    }
}
