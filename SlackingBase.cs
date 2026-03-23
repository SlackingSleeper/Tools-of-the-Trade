
using MelonLoader;

namespace ToolsOfTheTrade
{
    internal abstract class SlackingBase
    {
        public abstract void RegisterSettings();
        public abstract void Patch();
        public virtual void OnPreferencesSaved()
        {
        }
    }
}
