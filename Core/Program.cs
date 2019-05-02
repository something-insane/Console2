using StreamDeckSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenMacroBoard.NetCore.SDK;
using StreamDeckSharp.NetCore;

namespace ConsoleApp2
{
    class Program
    {
        static int targetFramerate = 0;
        static int stabilizationTimeInMilliseconds = 0;
        static int keyLimit = 20;
        static int howManyColors = 20;

        static Color[] colors = new[]
        {
            Color.Black,
            Color.White
        };

        static KeyBitmap black = KeyBitmap.Black;
        static KeyBitmap white = KeyBitmap.Create.FromRgb(255, 255, 255);
        static Font drawFont = new Font("Arial", 16);
        static SolidBrush drawBrush = new SolidBrush(Color.Black);

        static long frames = 0;
        static Stopwatch sw = new Stopwatch();

        static void Main(string[] args)
        {
            MakeColors();

            using (var deck = StreamDeck.OpenDevice())
            {
                deck.ClearKeys();

                sw.Start();
                var keepWaiting = true;

                deck.KeyStateChanged += (sender, e) =>
                {
                    if (e.Key == 0)
                    {
                        sw.Restart();
                        frames = 0;
                        DoFrame(deck);
                        return;
                    }

                    if (e.Key == 3 && e.IsDown)
                    {
                        targetFramerate++;
                        stabilizationTimeInMilliseconds =
                            targetFramerate > 0
                                ? 1000 / targetFramerate
                                : 0;
                        DoFrame(deck);
                        return;
                    }

                    if (e.Key == (3 + 10) && e.IsDown)
                    {
                        targetFramerate = Math.Max(0, targetFramerate - 1);
                        stabilizationTimeInMilliseconds =
                            targetFramerate > 0
                                ? 1000 / targetFramerate
                                : 0;
                        DoFrame(deck);
                        return;
                    }

                    if (e.Key == 2 && e.IsDown)
                    {
                        howManyColors = Math.Max(2, howManyColors + 1);
                        MakeColors();
                        DoFrame(deck);
                        return;
                    }

                    if (e.Key == (2 + 10) && e.IsDown)
                    {
                        howManyColors = Math.Max(2, howManyColors - 1);
                        MakeColors();
                        DoFrame(deck);
                        return;
                    }

                    if ((e.Key == 4 || e.Key == (4 + 5)) && e.IsDown)
                    {
                        DoFrame(deck);
                        return;
                    }

                    if (e.Key == 4 + 10)
                    {
                        keepWaiting = !e.IsDown;
                        return;
                    }
                };

                while (true)
                {
                    DoFrame(deck);

                    while (keepWaiting) Thread.Sleep(10);
                }
            }
        }

        static void DoFrame(IStreamDeckBoard deck)
        {
            lock (deck)
            {
                var clearColor = colors[frames % colors.Length];
                ////var brightness = MakeBrightness(sw);
                ////deck.SetBrightness(brightness);
                var swSnapshot = sw.ElapsedMilliseconds;

                var frame = $"{frames}f";

                var fps = targetFramerate > 0
                    ? $"{targetFramerate}"
                    : "max";
                var limit = Math.Min(keyLimit, deck.Keys.Count);

                var column = 0;
                DrawOne(
                    deck,
                    column,
                    clearColor,
                    "reset",
                    "frames",
                    frame);
                DrawOne(
                    deck,
                    column + 5,
                    clearColor,
                    string.Empty,
                    string.Empty,
                    frame);
                DrawOne(
                    deck,
                    column + 10,
                    clearColor,
                    string.Empty,
                    string.Empty,
                    frame);

                column = 1;
                DrawOne(
                    deck,
                    column,
                    clearColor,
                    string.Empty,
                    string.Empty,
                    frame);
                DrawOne(
                    deck,
                    column + 5,
                    clearColor,
                    string.Empty,
                    string.Empty,
                    frame);
                DrawOne(
                    deck,
                    column + 10,
                    clearColor,
                    string.Empty,
                    string.Empty,
                    frame);

                column = 2;
                DrawOne(
                    deck,
                    column,
                    clearColor,
                    "",
                    "color++",
                    frame);
                DrawOne(
                    deck,
                    column + 5,
                    clearColor,
                    "colors",
                    $"{howManyColors}",
                    frame);
                DrawOne(
                    deck,
                    column + 10,
                    clearColor,
                    "",
                    "color--",
                    frame);

                column = 3;
                DrawOne(
                    deck,
                    column,
                    clearColor,
                    "",
                    "fps++",
                    frame);
                DrawOne(
                    deck,
                    column + 5,
                    clearColor,
                    "fps",
                    $"{fps}",
                    frame);
                DrawOne(
                    deck,
                    column + 10,
                    clearColor,
                    "",
                    "fps--",
                    frame);

                column = 4;
                DrawOne(
                    deck,
                    column,
                    clearColor,
                    "draw",
                    "frame",
                    frame);
                DrawOne(
                    deck,
                    column + 5,
                    clearColor,
                    "draw",
                    "frame",
                    frame);
                DrawOne(
                    deck,
                    column + 10,
                    clearColor,
                    "conti-",
                    "nuous",
                    frame);
            }

            frames++;
            Console.WriteLine("-----------");
            Console.WriteLine($"frame {frames}");
            Console.WriteLine($" time {sw.Elapsed}");
            Console.WriteLine($"  fps {frames / (sw.ElapsedMilliseconds / 1000f)}");

            if (stabilizationTimeInMilliseconds > 0)
                Thread.Sleep(stabilizationTimeInMilliseconds);
        }

        private static Point top = new Point();
        private static Point middle = new Point(0, drawFont.Height);
        private static Point bottom = new Point(0, drawFont.Height * 2);

        static void DrawOne(
            IStreamDeckBoard deck,
            int key,
            Color color,
            string lineOne,
            string lineTwo,
            string lineThree)
        {
            deck.SetKeyBitmap(
                key,
                KeyBitmap.Create.FromGraphics(
                    72,
                    72,
                    gr => {
                        gr.Clear(color);
                        gr.DrawString(lineOne, drawFont, drawBrush, top);
                        gr.DrawString(lineTwo, drawFont, drawBrush, middle);
                        gr.DrawString(lineThree, drawFont, drawBrush, bottom);
                    }));
        }

        static void DrawEnd()
        {
            Console.WriteLine();
        }

        static byte MakeBrightness(Stopwatch stopwatch)
        {
            var ms = stopwatch.ElapsedMilliseconds;
            var radiansCyclingPerSecond = (ms / 1000f) * 2 * Math.PI;
            var radiansTargetCycling = radiansCyclingPerSecond * 0.1;
            var normalized = (Math.Sin(radiansTargetCycling) + 1) / 2;
            return (byte)Math.Round((normalized * 50) + 10);
        }

        static void ColorToHsv(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        static Color ColorFromHsv(double hue, double saturation, double value)
        {
            hue = hue % 360;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        static void MakeColors()
        {
            var newColors = new List<Color>();
            for (var step = 0d; step < 1; step += (1d / howManyColors))
            {
                var hue = 360 * step;
                newColors.Add(ColorFromHsv(hue, 1, 1));
            }

            colors = newColors.ToArray();
        }
    }
}
