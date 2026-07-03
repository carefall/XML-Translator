using System.Windows;

namespace XML_Translator
{
    public partial class App : Application {
        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
    }

}
