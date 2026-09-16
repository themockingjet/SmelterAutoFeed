using System;
using System.Collections.Generic;

namespace SmeltAndFuel;

internal sealed class FuelRules
{
    private readonly AutoFeedSettings _settings;
    private string? _lastPriorityValue;
    private string? _lastDisallowValue;
    private IReadOnlyList<string> _priorities = Array.Empty<string>();
    private HashSet<string> _disallowed = new(StringComparer.OrdinalIgnoreCase);

    internal FuelRules(AutoFeedSettings settings)
    {
        _settings = settings;
    }

    internal IReadOnlyList<string> Priorities
    {
        get
        {
            Refresh();
            return _priorities;
        }
    }

    internal bool IsDisallowed(string prefabName)
    {
        Refresh();
        return _disallowed.Contains(prefabName);
    }

    private void Refresh()
    {
        string priorityValue = _settings.FuelPriority.Value;
        string disallowValue = _settings.FuelDisallowTypes.Value;
        if (string.Equals(priorityValue, _lastPriorityValue, StringComparison.Ordinal) &&
            string.Equals(disallowValue, _lastDisallowValue, StringComparison.Ordinal))
        {
            return;
        }

        HashSet<string> seenPriorities = new(StringComparer.OrdinalIgnoreCase);
        List<string> priorities = new();
        foreach (string fuelType in SplitPrefabNames(priorityValue))
        {
            if (seenPriorities.Add(fuelType))
            {
                priorities.Add(fuelType);
            }
        }

        _priorities = priorities;
        _disallowed = new HashSet<string>(SplitPrefabNames(disallowValue), StringComparer.OrdinalIgnoreCase);
        _lastPriorityValue = priorityValue;
        _lastDisallowValue = disallowValue;
    }

    private static IEnumerable<string> SplitPrefabNames(string configuredTypes)
    {
        foreach (string configuredType in configuredTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string prefabName = configuredType.Trim();
            if (prefabName.Length > 0)
            {
                yield return prefabName;
            }
        }
    }
}
