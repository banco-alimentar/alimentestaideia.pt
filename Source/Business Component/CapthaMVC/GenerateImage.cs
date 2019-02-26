using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CaptchaMVC.HtmlHelpers;

namespace CaptchaMVC
{
    /// <summary>
    /// The base implementation of the generation of images.
    /// </summary>
    internal class GenerateImage :IGenerateImage
    {

        private const int Width = 200;
        private const int Height = 70;

        private const double WarpFactor = 1.6;
        private const double xAmp = WarpFactor * Width / 100;
        private const double yAmp = WarpFactor * Height / 85;
        private const double xFreq = 2 * Math.PI / Width;
        private const double yFreq = 2 * Math.PI / Height;


        private readonly FontFamily[] _fonts = {
                                        new FontFamily("Times New Roman"),
                                        new FontFamily("Georgia"),
                                        new FontFamily("Arial"),
                                        new FontFamily("Comic Sans MS"), 
                                      };
        /// <summary>
        /// Creating an image for a Captcha.
        /// </summary>
        /// <param name="captchaText">Text Captcha.</param>
        /// <returns></returns>
        public Bitmap Generate(string captchaText)
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                var rect = new Rectangle(0, 0, Width, Height);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (var solidBrush = new SolidBrush(Color.White))
                {
                    graphics.FillRectangle(solidBrush, rect);
                }

                //Randomly choose the font name.
                var family = _fonts[RandomNumber.Next(_fonts.Length - 1)];
                var size = (int)(Width * 2 / captchaText.Length);
                var font = new Font(family, size);

                //Select the font size.
                var meas = new SizeF(0, 0);
                while (size > 2 && (meas = graphics.MeasureString(captchaText, font)).Width > Width || meas.Height > Height)
                {
                    font.Dispose();
                    size -= 2;
                    font = new Font(family, size);
                }

                using (var fontFormat = new StringFormat())
                {
                    //Format the font in the center.
                    fontFormat.Alignment = StringAlignment.Center;
                    fontFormat.LineAlignment = StringAlignment.Center;

                    var path = new GraphicsPath();
                    path.AddString(captchaText, font.FontFamily, (int)font.Style, font.Size, rect, fontFormat);
                    using (var solidBrush = new SolidBrush(Color.Blue))
                    {
                        graphics.FillPath(solidBrush, DeformPath(path));
                    }

                }
                font.Dispose();

            }
            return bmp;
        }


        private GraphicsPath DeformPath(GraphicsPath graphicsPath)
        {
            var deformed = new PointF[graphicsPath.PathPoints.Length];
            var rng = new Random();
            var xSeed = rng.NextDouble()*2*Math.PI;
            var ySeed = rng.NextDouble()*2*Math.PI;
            for (int i = 0; i < graphicsPath.PathPoints.Length; i++)
            {
                var original = graphicsPath.PathPoints[i];
                var val = xFreq*original.X*yFreq*original.Y;
                var xOffset = (int) (xAmp*Math.Sin(val + xSeed));
                var yOffset = (int) (yAmp*Math.Sin(val + ySeed));
                deformed[i] = new PointF(original.X + xOffset, original.Y + yOffset);
            }
            return new GraphicsPath(deformed, graphicsPath.PathTypes);
        }
    }
}
