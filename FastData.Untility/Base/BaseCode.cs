using System;
using System.IO;

#if NETFRAMEWORK
using System.Drawing;
using System.Drawing.Imaging;
#endif

namespace FastUntility.Base
{
    /// <summary>
    /// 验证码工具类
    /// 提供图形验证码生成功能（.NET Framework 下生成图片，跨平台下生成纯文本）
    /// </summary>
    public static class BaseCode
    {
#if NETFRAMEWORK
        #region 生成验证代码
        /// <summary>
        /// 生成验证代码
        /// </summary>
        /// <param name="Code">验证码（输出参数）</param>
        /// <param name="CodeLength">验证码长度</param>
        /// <param name="Width">图片宽度</param>
        /// <param name="Height">图片高度</param>
        /// <param name="FontSize">字体大小</param>
        /// <returns>图片字节数组</returns>
        public static byte[] CreateValidateGraphic(out String Code, int CodeLength, int Width, int Height, int FontSize)
        {            
            var sCode = String.Empty;

            //颜色
            Color[] oColors = { Color.Black, Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Brown, Color.Brown, Color.DarkBlue };

            //字体
            string[] oFontNames = { "Times New Roman", "MS Mincho", "Book Antiqua", "Gungsuh", "PMingLiU", "Impact" };

            //字符
            char[] oCharacter = { '1', '2', '3', '4', '5', '6', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'W', 'X', 'Y' };
            Random oRnd = new Random();
            Bitmap oBmp = null;
            Graphics oGraphics = null;
            int N1 = 0;
            Point oPoint1 = default(Point);
            Point oPoint2 = default(Point);
            string sFontName = null;
            Font oFont = null;
            Color oColor = default(Color);

            for (N1 = 0; N1 <= CodeLength - 1; N1++)
            {
                sCode += oCharacter[oRnd.Next(oCharacter.Length)];
            }

            oBmp = new Bitmap(Width, Height);
            oGraphics = Graphics.FromImage(oBmp);
            oGraphics.Clear(Color.White);
            try
            {
                for (N1 = 0; N1 <= 4; N1++)
                {
                    oPoint1.X = oRnd.Next(Width);
                    oPoint1.Y = oRnd.Next(Height);
                    oPoint2.X = oRnd.Next(Width);
                    oPoint2.Y = oRnd.Next(Height);
                    oColor = oColors[oRnd.Next(oColors.Length)];
                    oGraphics.DrawLine(new Pen(oColor), oPoint1, oPoint2);
                }

                float spaceWith = 0, dotX = 0, dotY = 0;
                if (CodeLength != 0)
                {
                    spaceWith = (Width - FontSize * CodeLength - 10) / CodeLength;
                }

                for (N1 = 0; N1 <= sCode.Length - 1; N1++)
                {
                    sFontName = oFontNames[oRnd.Next(oFontNames.Length)];
                    oFont = new Font(sFontName, FontSize, FontStyle.Italic);
                    oColor = oColors[oRnd.Next(oColors.Length)];

                    dotY = (Height - oFont.Height) / 2 + 2;
                    dotX = Convert.ToSingle(N1) * FontSize + (N1 + 1) * spaceWith;

                    oGraphics.DrawString(sCode[N1].ToString(), oFont, new SolidBrush(oColor), dotX, dotY);
                }

                for (int i = 0; i <= 30; i++)
                {
                    int x = oRnd.Next(oBmp.Width);
                    int y = oRnd.Next(oBmp.Height);
                    Color clr = oColors[oRnd.Next(oColors.Length)];
                    oBmp.SetPixel(x, y, clr);
                }

                Code = sCode;

                using (MemoryStream stream = new MemoryStream())
                {
                    oBmp.Save(stream, ImageFormat.Jpeg);
                    oBmp.Dispose();
                    oGraphics.Dispose();
                    return stream.ToArray();
                }
            }
            finally
            {
                oGraphics.Dispose();
            }
        }
        #endregion
#else
        #region 生成验证代码（跨平台版本，返回纯文本）
        /// <summary>
        /// 生成验证代码（跨平台版本，返回纯文本字节）
        /// </summary>
        /// <param name="Code">验证码（输出参数）</param>
        /// <param name="CodeLength">验证码长度</param>
        /// <param name="Width">图片宽度</param>
        /// <param name="Height">图片高度</param>
        /// <param name="FontSize">字体大小</param>
        /// <returns>图片字节数组</returns>
        public static byte[] CreateValidateGraphic(out String Code, int CodeLength, int Width, int Height, int FontSize)
        {
            var sCode = String.Empty;
            char[] oCharacter = { '1', '2', '3', '4', '5', '6', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'W', 'X', 'Y' };
            Random oRnd = new Random();

            for (int N1 = 0; N1 <= CodeLength - 1; N1++)
            {
                sCode += oCharacter[oRnd.Next(oCharacter.Length)];
            }

            Code = sCode;
            return System.Text.Encoding.UTF8.GetBytes(sCode);
        }
        #endregion
#endif
    }
}
