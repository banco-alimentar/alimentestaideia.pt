using System.Drawing;

namespace CaptchaMVC
{
    /// <summary>
    /// Interface for implementing image Captcha.
    /// </summary>
    public interface IGenerateImage
    {
        Bitmap Generate(string captchaText);
    }
}
