namespace Captura.Windows.MediaFoundation
{
    public class MfSettings : PropertyStore
    {
        public string SelectedEncoder
        {
            get => Get("H.264");
            set => Set(value);
        }
    }
}