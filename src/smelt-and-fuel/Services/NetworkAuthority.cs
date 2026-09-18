using UnityEngine;

namespace SmeltAndFuel;

internal static class NetworkAuthority
{
    internal static bool IsOwner(Component component)
    {
        ZNetView? view = component.GetComponent<ZNetView>();
        return view is not null && view.IsValid() && view.IsOwner();
    }
}
