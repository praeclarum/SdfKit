namespace Perf.WindowsForms
{
  public static class Extensions
  {
    public static void RunInUIThread(this SynchronizationContext sync, Action a)
    {
      sync.Post(d => a(), null);
    }
  }
}