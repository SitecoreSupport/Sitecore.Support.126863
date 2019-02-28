using Sitecore.Diagnostics;
using Sitecore.Events.Hooks;
using Sitecore.Resources.Media;

namespace Sitecore.Support.Resources.Media
{
  public class MediaCreatorReplacerHook : IHook
  {
    public void Initialize()
    {
      MediaManager.Creator = new Sitecore.Support.Resources.Media.MediaCreator();
    }
  }
}