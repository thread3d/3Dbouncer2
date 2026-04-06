namespace TextBouncer.FillRules;

/// <summary>
/// Configuration that maps each polyhedron (by name or index) to a fill rule strategy.
/// Allows different polyhedra to use different rendering rules, enabling
/// iterative refinement of each object's rendering independently.
/// </summary>
public class FillRuleConfig
{
    /// <summary>
    /// Maps polyhedron index -> fill rule strategy name.
    /// If a polyhedron is not in the map, the default strategy is used.
    /// </summary>
    private readonly Dictionary<int, string> _polyhedronRules = new();

    /// <summary>
    /// The default fill rule for polyhedra not explicitly configured.
    /// </summary>
    public string DefaultRule { get; set; } = "Fan from Centroid";

    /// <summary>
    /// All available fill rule strategies (registered by name).
    /// </summary>
    public static IReadOnlyDictionary<string, IFillRuleStrategy> Strategies { get; } = new Dictionary<string, IFillRuleStrategy>
    {
        // 2D Triangulation rules
        ["Fan from Centroid"] = new FanTriangulationStrategy(),
        ["Ear Clipping (Even-Odd)"] = new EarClipTriangulationStrategy(),
        ["Convex Hull (Non-Zero)"] = new ConvexHullStrategy(),
        ["Fan from Vertex 0"] = new FanFromVertexStrategy(),
        ["Strip Partition"] = new StripPartitionStrategy(),
        ["Neighbor Fan"] = new NeighborFanStrategy(),
        ["Kirkpatrick Fan"] = new KirkpatrickStrategy(),
        ["Monotone Partition"] = new MonotonePartitionStrategy(),
        ["Minimum Diagonal"] = new MinimumDiagonalStrategy(),
        ["Alternating Fan"] = new AlternatingFanStrategy(),
        ["Dial (Min Angle)"] = new DialStrategy(),
        ["Star Bipartition"] = new HertelMehlhornStrategy(),
        // 3D Rendering rules
        ["Painter Back-to-Front"] = new PainterBackToFrontRule(),
        ["Backface Culling"] = new BackfaceCullingRule(),
        ["Normal Angle Shading"] = new NormalAngleShadingRule(),
        ["Distance Fog"] = new DistanceFogRule(),
        ["Wireframe Overlay"] = new WireframeOverlayRule(),
        ["Spectral Coloring"] = new SpectralColoringRule(),
        ["Emissive Glow"] = new EmissiveGlowRule(),
        ["Alternating Opacity"] = new AlternatingOpacityRule(),
    };

    /// <summary>
    /// Set the fill rule for a specific polyhedron by index.
    /// </summary>
    public void SetRule(int polyhedronIndex, string strategyName)
    {
        if (!Strategies.ContainsKey(strategyName))
            throw new ArgumentException($"Unknown strategy: {strategyName}. Available: {string.Join(", ", Strategies.Keys)}");

        _polyhedronRules[polyhedronIndex] = strategyName;
    }

    /// <summary>
    /// Get the fill rule strategy for a specific polyhedron index.
    /// Returns the default rule if not explicitly configured.
    /// </summary>
    public IFillRuleStrategy GetRule(int polyhedronIndex)
    {
        if (_polyhedronRules.TryGetValue(polyhedronIndex, out string? ruleName))
            return Strategies[ruleName];
        return Strategies[DefaultRule];
    }

    /// <summary>
    /// Get the rule name for a specific polyhedron index.
    /// </summary>
    public string GetRuleName(int polyhedronIndex)
    {
        if (_polyhedronRules.TryGetValue(polyhedronIndex, out string? ruleName))
            return ruleName;
        return DefaultRule;
    }

    /// <summary>
    /// Clear the rule for a specific polyhedron, reverting it to the default.
    /// </summary>
    public void ClearRule(int polyhedronIndex)
    {
        _polyhedronRules.Remove(polyhedronIndex);
    }

    /// <summary>
    /// Clear all per-polyhedron rules, reverting all to the default.
    /// </summary>
    public void ClearAllRules()
    {
        _polyhedronRules.Clear();
    }

    /// <summary>
    /// Preset configurations for common rendering modes.
    /// </summary>
    public void ApplyPreset(string presetName)
    {
        switch (presetName)
        {
            case "All Fan":
                ClearAllRules();
                DefaultRule = "Fan from Centroid";
                break;

            case "All Ear-Clip":
                ClearAllRules();
                DefaultRule = "Ear Clipping (Even-Odd)";
                break;

            case "All Convex Hull":
                ClearAllRules();
                DefaultRule = "Convex Hull (Non-Zero)";
                break;

            case "Conservative (Ear-Clip default)":
                ClearAllRules();
                DefaultRule = "Ear Clipping (Even-Odd)";
                break;

            default:
                throw new ArgumentException($"Unknown preset: {presetName}. Available: All Fan, All Ear-Clip, All Convex Hull, Conservative (Ear-Clip default)");
        }
    }
}
