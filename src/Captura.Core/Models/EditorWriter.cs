using System.IO;
using System.Threading.Tasks;

namespace Captura.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EditorWriter : NotifyPropertyChanged, IImageWriterItem
    {
        readonly Settings _settings;

        public EditorWriter(Settings Settings)
        {
            _settings = Settings;
        }

        public Task Save(IBitmapImage Image, ImageFormats Format, string FileName)
        {
            var extension = Format.ToString().ToLower();
            var fileName = _settings.GetFileName(extension, FileName);

            if (!File.Exists(fileName))
            {
                Image.Save(fileName, Format);
            }

            var winserv = ServiceProvider.Get<IMainWindow>();
            winserv.EditImage(fileName);

            return Task.CompletedTask;
        }

        public string Display => "Editor";

        bool _active;

        public bool Active
        {
            get => _active;
            set => Set(ref _active, value);
        }

        public override string ToString() => Display;
    }
}