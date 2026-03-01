using Avalonia;
using Avalonia.Controls;
using System.Collections;

namespace Intune.Commander.Desktop.Views.Controls;

public partial class AssignmentListControl : UserControl
{
    public static readonly StyledProperty<IList?> AssignmentsProperty =
        AvaloniaProperty.Register<AssignmentListControl, IList?>(nameof(Assignments));

    public static readonly StyledProperty<bool> ShowIntentProperty =
        AvaloniaProperty.Register<AssignmentListControl, bool>(nameof(ShowIntent));

    public IList? Assignments
    {
        get => GetValue(AssignmentsProperty);
        set => SetValue(AssignmentsProperty, value);
    }

    public bool ShowIntent
    {
        get => GetValue(ShowIntentProperty);
        set => SetValue(ShowIntentProperty, value);
    }

    public AssignmentListControl()
    {
        InitializeComponent();
    }
}
