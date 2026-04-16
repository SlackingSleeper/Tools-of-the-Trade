
using MelonLoader;

namespace ToolsOfTheTrade
{
    public abstract class SlackingBase
    {
        public abstract void RegisterSettings();
        public abstract void TryPatch();
        public abstract void Patch();
    }
}
