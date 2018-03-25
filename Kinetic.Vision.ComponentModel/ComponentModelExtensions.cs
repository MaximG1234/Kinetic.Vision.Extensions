using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    public static class ComponentModelExtensions
    {
        public static void NotifyPropertyChanged(this INotifyPropertyChanged sender, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = "")
        {
            if (handler != null)
            {
                PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
                handler(sender, args);
            }
        }
    }
}
