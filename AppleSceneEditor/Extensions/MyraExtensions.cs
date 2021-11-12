using System.Diagnostics;
using Myra.Graphics2D.UI;
using Myra.MML;

namespace AppleSceneEditor.Extensions
{
    public static class MyraExtensions
    {
        public static T? TryFindWidgetById<T>(this Container container, string id) where T : Widget
        {
#if DEBUG
            const string methodName = nameof(MyraExtensions) + "." + nameof(TryFindWidgetById);
#endif
            T? output;
            
            try
            {
                output = container.FindWidgetById(id) as T;

                if (output is null)
                {
                    Debug.WriteLine($"{methodName}: {id} cannot be casted into an instance of {typeof(T)}");
                    return null;
                }
            }
            catch
            {
                Debug.WriteLine($"{methodName}: {typeof(T)} of ID {id} cannot be found.");
                return null;
            }

            return output;
        }
    }
}