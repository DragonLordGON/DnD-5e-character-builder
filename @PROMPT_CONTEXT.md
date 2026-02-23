Role: You are an expert C# WPF developer assisting with a D&D 5e Character Builder.

Project Overview:
A desktop application using a TabControl workflow to guide users through character creation. It uses a custom XML-based data loading system for Races and Items.

Current Architecture:

    UI: MainWindow.xaml uses a TabControl (MainModeTab) to switch between the Menu and the Builder. Inside the Builder, another TabControl (BuilderTabControl) handles the steps.

    State Management: Private fields in MainWindow.xaml.cs track gold, selected race, and ability scores.

    Point Buy System: 27-point budget. Scores 8–13 cost 1pt; 14–15 cost 2pts.

    Data Services: XmlLoader (Races) and ItemLoader (Shop items).

What We Have Done:

    Menu & Navigation: Created the landing page and navigation between builder steps.

    Race Selection: Implemented a "Lock" mechanism that filters the list to the selected race and enables the next step.

    Class Selection: Built the UI tab and hard-coded a temporary list of 12 PHB classes with descriptions and icons.

    Ability Scores: Fully functional Point Buy logic including cost calculation and UI updates for modifiers.

    Inventory/Shop: Basic shop system where items can be bought, gold is deducted, and items are added to an ObservableCollection.

Pending Tasks / Next Steps:

    XML Class Loader: Replace hard-coded classes with a ClassLoader and an external Classes.xml.

    Saving/Loading: Implement a system to export the character to a JSON or XML file.

    Visual Polish: Enhance the XAML templates for the ListBoxes.

Feature,Status,Implementation Detail
Data Engine,🟢 Partial,XML loading for Races/Items; Class is currently hard-coded.
Race Step,🔵 Done,"Supports Homebrew filtering and ""Lock/Unlock"" UI logic."
Class Step,🟡 In Progress,UI exists; needs backend data binding to XML.
Stats Step,🔵 Done,5e Point Buy (8-15 range) with cost scaling.
Shop,🔵 Done,"Basic ""Buy"" button with gold validation."
Navigation,🔵 Done,Tab-based flow with overlay confirmation system.