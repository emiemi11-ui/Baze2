using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// FISIERUL ASSEMBLYINFO.CS - Metadata despre Assembly
//
// CE ESTE UN ASSEMBLY?
// Un assembly este unitatea de deployment in .NET
// Practic, e fisierul .exe sau .dll generat dupa compilare
// Contine codul compilat (IL) si metadata despre el
//
// CE ESTE ACEST FISIER?
// Contine ATRIBUTE care descriu assembly-ul
// Aceste informatii sunt embedded in fisierul .exe final
// Pot fi citite cu Reflection sau vazute in Properties la fisier
//
// DE CE E IMPORTANT?
// 1. Identifica aplicatia (titlu, descriere, companie)
// 2. Versionare (versiunea aplicatiei)
// 3. Configurari speciale (COM interop, teme WPF, etc.)
//
// CUM SE FOLOSESTE?
// Se compileaza automat cu restul proiectului
// Nu trebuie sa faci nimic special cu el

// ATRIBUTE DE IDENTIFICARE
// Acestea descriu CE este aplicatia

// AssemblyTitle - Titlul aplicatiei
// Apare in Properties > Details > File description
[assembly: AssemblyTitle("ECommerceAppPerfect")]

// AssemblyDescription - Descrierea aplicatiei
// Scurta descriere a ce face aplicatia
[assembly: AssemblyDescription("E-Commerce Application using Entity Framework DB First approach. Academic project demonstrating WPF, MVVM pattern, and database-first development with Entity Framework 6.")]

// AssemblyConfiguration - Configuratia build-ului
// De obicei gol, sau "Debug"/"Release"
[assembly: AssemblyConfiguration("")]

// AssemblyCompany - Numele companiei/organizatiei
// Pentru proiecte academice, poti pune universitatea sau numele tau
[assembly: AssemblyCompany("Academic Project")]

// AssemblyProduct - Numele produsului
// Poate fi diferit de AssemblyTitle (pentru suite de aplicatii)
[assembly: AssemblyProduct("ECommerceAppPerfect")]

// AssemblyCopyright - Copyright-ul
// Anul si detinatoruul drepturilor de autor
[assembly: AssemblyCopyright("Copyright © 2024")]

// AssemblyTrademark - Trademark (marca inregistrata)
// De obicei gol pentru proiecte academice
[assembly: AssemblyTrademark("")]

// AssemblyCulture - Cultura/limba assembly-ului
// Gol pentru assembly-uri culture-neutral (internationale)
// Sau "en-US", "ro-RO" pentru assembly-uri localizate
[assembly: AssemblyCulture("")]

// ATRIBUT COM INTEROP
// ComVisible controleaza daca tipurile sunt vizibile pentru COM
//
// CE ESTE COM?
// Component Object Model - tehnologie veche Microsoft pentru interoperabilitate
// Permite ca codul .NET sa fie apelat din aplicatii non-.NET (VBA, C++, etc.)
//
// DE CE false?
// Nu avem nevoie de COM interop pentru o aplicatie WPF standalone
// Setam false pentru securitate (nu expunem tipurile)
[assembly: ComVisible(false)]

// ATRIBUTE WPF SPECIFICE
// Acestea sunt necesare pentru aplicatii WPF

// ThemeInfo - Informatii despre teme WPF
//
// CE ESTE O TEMA WPF?
// Tema defineste aspectul vizual al controalelor (butoane, textbox-uri, etc.)
// Windows are teme diferite (Aero, Luna, Classic, etc.)
//
// PARAMETRII:
// ResourceDictionaryLocation.None - NU avem dictationar de resurse pentru teme specifice
// ResourceDictionaryLocation.SourceAssembly - Resursele generice sunt in assembly-ul nostru
//
// Pentru o aplicatie simpla, aceste valori sunt OK
// Pentru aplicatii cu teme custom, ai schimba la ExternalAssembly
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

// ATRIBUTE DE VERSIONARE
// Acestea specifica versiunea assembly-ului
//
// FORMATUL VERSIUNII: Major.Minor.Build.Revision
// Major - schimbari majore, incompatibile
// Minor - functionalitati noi, compatibile
// Build - fix-uri si imbunatatiri
// Revision - hotfix-uri, patch-uri
//
// EXEMPLU: 1.0.0.0
// - Major 1 = prima versiune stabila
// - Minor 0 = fara functionalitati adaugate
// - Build 0 = fara fix-uri
// - Revision 0 = versiunea initiala

// AssemblyVersion - Versiunea assembly-ului
// Folosita de CLR pentru binding-ul assembly-urilor
// IMPORTANT: Daca schimbi asta, referintele existente pot sa nu mai mearga
[assembly: AssemblyVersion("1.0.0.0")]

// AssemblyFileVersion - Versiunea fisierului
// Apare in Properties > Details > File version
// Poate fi diferita de AssemblyVersion
// De obicei se incrementeaza mai frecvent
[assembly: AssemblyFileVersion("1.0.0.0")]

// NeutralResourcesLanguage - Limba default pentru resurse
// Specifica limba pentru string-urile neutre (nelocalizate)
// "en-US" = English (United States) ca limba default
[assembly: NeutralResourcesLanguage("en-US")]
