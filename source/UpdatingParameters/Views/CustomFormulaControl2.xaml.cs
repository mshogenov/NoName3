using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UpdatingParameters.Models;

namespace UpdatingParameters.Views;

public partial class CustomFormulaControl2 : UserControl
{
    public CustomFormulaControl2()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(CustomFormulaControl2),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }


    public static readonly DependencyProperty SearchParametersViewProperty =
        DependencyProperty.Register(
            nameof(SearchParametersView),
            typeof(string),
            typeof(CustomFormulaControl2),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string SearchParametersView
    {
        get => (string)GetValue(SearchParametersViewProperty);
        set => SetValue(SearchParametersViewProperty, value);
    }

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(ObservableCollection<string>), typeof(CustomFormulaControl2),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ObservableCollection<string>  Parameters
    {
        get => (ObservableCollection<string>)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public static readonly DependencyProperty SelectParameterProperty = DependencyProperty.Register(
        nameof(SelectParameter), typeof(string), typeof(CustomFormulaControl2),
        new PropertyMetadata(default(string)));

    public string SelectParameter
    {
        get => (string)GetValue(SelectParameterProperty);
        set => SetValue(SelectParameterProperty, value);
    }

    public static readonly DependencyProperty AddParameterFormulaCommandProperty = DependencyProperty.Register(
        nameof(AddParameterFormulaCommand), typeof(ICommand), typeof(CustomFormulaControl2),
        new PropertyMetadata(default(ICommand)));

    public ICommand AddParameterFormulaCommand
    {
        get => (ICommand)GetValue(AddParameterFormulaCommandProperty);
        set => SetValue(AddParameterFormulaCommandProperty, value);
    }

    public static readonly DependencyProperty RemoveParameterFormulaCommandProperty = DependencyProperty.Register(
        nameof(RemoveParameterFormulaCommand), typeof(ICommand), typeof(CustomFormulaControl2),
        new PropertyMetadata(default(ICommand)));

    public ICommand RemoveParameterFormulaCommand
    {
        get => (ICommand)GetValue(RemoveParameterFormulaCommandProperty);
        set => SetValue(RemoveParameterFormulaCommandProperty, value);
    }

    public static readonly DependencyProperty AdskFormulasProperty =
        DependencyProperty.Register(
            nameof(AdskFormulas),
            typeof(ObservableCollection<Formula>),
            typeof(CustomFormulaControl2),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ObservableCollection<Formula> AdskFormulas
    {
        get => (ObservableCollection<Formula>)GetValue(AdskFormulasProperty);
        set => SetValue(AdskFormulasProperty, value);
    }


    public static readonly DependencyProperty SelectFormulaProperty = DependencyProperty.Register(
        nameof(SelectFormula), typeof(Formula), typeof(CustomFormulaControl2), new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Formula SelectFormula
    {
        get => (Formula)GetValue(SelectFormulaProperty);
        set => SetValue(SelectFormulaProperty, value);
    }

    public static readonly DependencyProperty MoveUpFormulaCommandProperty = DependencyProperty.Register(
        nameof(MoveUpFormulaCommand), typeof(ICommand), typeof(CustomFormulaControl2),
        new PropertyMetadata(default(ICommand)));

    public ICommand MoveUpFormulaCommand
    {
        get => (ICommand)GetValue(MoveUpFormulaCommandProperty);
        set => SetValue(MoveUpFormulaCommandProperty, value);
    }

    public static readonly DependencyProperty MoveDownFormulaCommandProperty = DependencyProperty.Register(
        nameof(MoveDownFormulaCommand), typeof(ICommand), typeof(CustomFormulaControl2),
        new PropertyMetadata(default(ICommand)));

    public ICommand MoveDownFormulaCommand
    {
        get => (ICommand)GetValue(MoveDownFormulaCommandProperty);
        set => SetValue(MoveDownFormulaCommandProperty, value);
    }

    public static readonly DependencyProperty CombinedValuesProperty = DependencyProperty.Register(
        nameof(CombinedValues), typeof(string), typeof(CustomFormulaControl2), new PropertyMetadata(default(string)));

    public string CombinedValues
    {
        get => (string)GetValue(CombinedValuesProperty);
        set => SetValue(CombinedValuesProperty, value);
    }

    public static readonly DependencyProperty NameParameterProperty = DependencyProperty.Register(
        nameof(NameParameter), typeof(string), typeof(CustomFormulaControl2), new PropertyMetadata(default(string)));

    public string NameParameter
    {
        get => (string)GetValue(NameParameterProperty);
        set => SetValue(NameParameterProperty, value);
    }
   
}