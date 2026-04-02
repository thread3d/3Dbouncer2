namespace TextBouncer;

/// <summary>
/// Defines the control mode for physics simulation.
/// </summary>
public enum ControlMode
{
    /// <summary>
    /// Physics runs without user input - particles bounce automatically.
    /// </summary>
    Automatic,

    /// <summary>
    /// User controls position and rotation directly, physics is paused.
    /// </summary>
    Manual,

    /// <summary>
    /// Physics runs with user input applied as forces (nudge mode).
    /// Full implementation deferred to Phase 6.
    /// </summary>
    Mixture
}
