using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tesseract;
using Rect = Tesseract.Rect;

namespace SudokuSolver1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Does all the work of getting numbers from image of sudoku board.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        static List<string> GetNumberFromPicture(Bitmap img, int offset)
        {
            //List to add numbers
            List<string> numbers = new List<string>();

            GetEdges(img, out List<int> verticalLines, out List<int> horizontalLines);

            if (verticalLines.Count != 10 || horizontalLines.Count != 10)
            {
                MessageBox.Show("Failed to read sudoku");
                return numbers;
            }

            using (var engine = new TesseractEngine(@"./tessdata", "fin", EngineMode.Default))
            {
                engine.SetVariable("tessedit_char_whitelist", "123456789");
                using (Pix image = Pix.LoadFromMemory(ImageToByteArray(img)))
                {
                    //Loop individual sub Rectangles from the SudokuBoard and pass that to engine.
                    for (int i = 0; i < 9; i++)
                    {
                        for (int l = 0; l < 9; l++)
                        {
                            using (var page = engine.Process(image, Rect.FromCoords(verticalLines[l] + offset, horizontalLines[i] + offset, verticalLines[l+1] - offset, horizontalLines[i+1] - offset), PageSegMode.SingleChar))
                            {
                                string text = page.GetText();
                                if (text == "" || page.GetMeanConfidence() < 0.8)
                                {
                                    numbers.Add("0");
                                }
                                else
                                {
                                    //Remove all but ingtegers
                                    string num = new String(text.Where(c => c != '-' && (c > '0' || c < '9')).ToArray());
                                    numbers.Add(num.Remove(1));
                                }
                            }
                        }
                    }
                }
            }

            return numbers;
        }

        /// <summary>
        /// Gets the list of vertical and horizontal axis placements.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="verticalLines"></param>
        /// <param name="horizontalLines"></param>
        static void GetEdges(Bitmap img, out List<int> verticalLines, out List<int> horizontalLines)
        {
            verticalLines = new List<int>();
            horizontalLines = new List<int>();

            //Generate list of 50 numbers. Just a trick to make syntax bit cleaner when checking for next 50 pixels.
            List<int> nums = new List<int>();
            for (int i = 1;i < 50;i++)
            {
                nums.Add(i);
            }

            //Start from the top and loop a pixel at a time, when black pixel (R < 150) is found, check next 50 pixels in hor -direction
            //If all black, add current pixel height in horizontalLines list. When axis is found, skip next 40 pixels.
            //Black threshold and how many pixels to skip should prolly be parameters.
            for (int i = 0; i < img.Height; i++)
            {
                if (img.GetPixel(img.Width / 2,i).R < 150)
                {
                    if (nums.All(num=> img.GetPixel(img.Width / 2 + num, i).R < 150))
                    {
                        horizontalLines.Add(i);
                        i += 40;
                    }
                }
            }

            for (int i = 0; i < img.Width; i++)
            {
                if (img.GetPixel(i, img.Height/2).R < 150)
                {
                    if (nums.All(num => img.GetPixel(i, img.Height / 2 + num).R < 150))
                    {
                        verticalLines.Add(i);
                        i += 40;
                    }
                }
            }
        }

        /// <summary>
        /// Everything happens here pretty much.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Get image from ClipBOard
            Bitmap bitmap = GetBitmap(Clipboard.GetImage());

            //Generate list of buttons to ui. Generate them here rather that in XAML so they are also indexed to a list, which makes it easy to add numbers to correct button.
            List<Button> buttons = new List<Button>();
            for (int i = 0; i < 9; i++)
            {
                for (int l = 0; l < 9; l++)
                {
                    Button btn = new Button();
                    Grid.SetRow(btn, i);
                    Grid.SetColumn(btn, l);
                    MainBoard.Children.Add(btn);
                    buttons.Add(btn);
                }
            }

            //Get list of numbers from the image
            List<string> nums = GetNumberFromPicture(bitmap, 7);

            //Fill number to buttons in sudoku board.
            for (int i = 0; i < nums.Count; i++)
            {
                if (nums[i] != "0")
                {
                    buttons[i].Content = nums[i];
                }
            }
        }

        Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static byte[] ImageToByteArray(System.Drawing.Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
