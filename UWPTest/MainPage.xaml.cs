using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;

namespace UWPTest
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>

    public class Almond
    {
        private Random random;
        public Vector2 position;
        public float maxSpeed = 0.1f;
        public float randomnessPower = 0.2f;
        public float randomnessAngle = 0.1f;
        private float power = 0;
        private float angle;
        public Color col;

        public Almond(Random ra, float x, float y)
        {
            position.X = x;
            position.Y = y;
            angle = (float)(ra.NextDouble() * (float)(Math.PI * 2.0));
            random = ra;
        }
        public void GenerateRandomness(Vector2 boundary1, Vector2 boundary2, Vector2 externalPowerVector)
        {
            // generate random power and angle amount
            float randomPower = (float)random.NextDouble() - 0.5f;
            float randomAngle = (float)random.NextDouble() - 0.5f;

            // add generated random values to current power and angle
            power += Math.Min(randomPower * randomnessPower, maxSpeed);
            angle += randomAngle * randomnessAngle;

            // calculate amount of x/y movement with current power and angle
            float amountX = power * (float)Math.Cos(angle) + externalPowerVector.X;
            float amountY = power * (float)Math.Sin(angle) + externalPowerVector.Y;

            // calculate next position, and check for boundary violation
            Vector2 newPosition = position;
            newPosition.X += amountX;
            newPosition.Y += amountY;
            if(newPosition.X < boundary1.X || newPosition.X > boundary2.X 
                || newPosition.Y < boundary1.Y || newPosition.Y > boundary2.Y)
            {
                // out of boundary, flip angle and kick back the almond
                angle -= (float)Math.PI;

                amountX = power * (float)Math.Cos(angle);
                amountY = power * (float)Math.Sin(angle);
                position.X += amountX;
                position.Y += amountY;

                // check if the almond is stuck(mostly due to panning or zoom) outside of the boundary, and summon back to inside
                if (position.X < boundary1.X)
                    position.X = boundary1.X + amountX;
                else if (position.X > boundary2.X)
                    position.X = boundary2.X + amountX;
                if (position.Y < boundary1.Y)
                    position.Y = boundary1.Y + amountY;
                else if (position.Y > boundary2.Y)
                    position.Y = boundary2.Y + amountY;
            }
            else
            {
                // update position if there is no violation
                position = newPosition;
            }
        }
    }
    /// 

    public sealed partial class MainPage : Page
    {
        Almond[] test;
        static int count = 20000; // number of almonds(particles)
        public Random randomMachine = new Random();
        CanvasCachedGeometry canvasCachedGeometry;


        CanvasTextFormat canvasTextFormat = new CanvasTextFormat();
        //Color transW;
        float minScale = 0.005f;
        float targetScale = 1.0f;
        float maxScale = 5.0f;
        Vector2 offset = Vector2.Zero;
        Vector2 renderSize;
        Vector2 cursorPos;
        bool pressed = false;
        Vector2 prev = Vector2.Zero;
        Vector2 panning = Vector2.Zero;
        Matrix3x2 currentMatrix;
        Matrix3x2 actualMatrix;


        public MainPage()
        {
            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            double height = this.Height * 0.5;
            double width = this.Width * 0.5;
            canvasTextFormat.FontSize = 11.0f;
        }

        // variables of fps counter, copy&pasted from stackoverflow; thanks
        DateTime _lastTime; // marks the beginning the measurement began
        int _framesRendered; // an increasing count
        int _fps; // the FPS calculated from the last measurement
        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            // fps counter, copy&pasted from stackoverflow;
            _framesRendered++;
            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                // one second has elapsed 
                _fps = _framesRendered;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }

            // draw almonds
            using (CanvasCommandList ccl = new CanvasCommandList(sender))
            {
                using (CanvasDrawingSession cds = ccl.CreateDrawingSession())
                {
                    cds.Transform = currentMatrix;
                    
                    for (int i = 0; i < count; i++)
                    {
                        cds.DrawCachedGeometry(canvasCachedGeometry, test[i].position, test[i].col);
                    }
                }
                args.DrawingSession.DrawImage(ccl);
            }

            // debug print session
            bool debug = true;
            if (debug && MainCanvas != null)
            {
                Vector2 topleft = (Vector2.Zero - panning - offset) / targetScale;
                Vector2 bottomright = topleft + (renderSize / targetScale);
                Vector2 off;
                Vector2 sca;
                off.X = currentMatrix.M31;
                off.Y = currentMatrix.M32;
                sca.X = currentMatrix.M11;
                sca.Y = currentMatrix.M22;
                Vector2 topleft2 = (Vector2.Zero - off) / sca;
                Vector2 bottomright2 = topleft + (renderSize / sca);
                string DebugText =
                    $"FPS: {_fps}\n" +
                    $"PARTICLES: {count}\n" +
                    $"TARGET_SCALE: {targetScale}\n" +
                    $"PANNING_OFFSET: {panning}\n" +
                    $"ZOOMING_OFFSET: {offset}\n" +
                    $"RENDER_SIZE: {renderSize}\n" +
                    $"TARGET_TOP_LEFT: {topleft}\n" +
                    $"TARGET_BOTTOM_RIGHT: {bottomright}\n" +
                    $"VIEWPORT_SIZE: {bottomright - topleft}\n" +
                    $"CURSOR_POSITION: {cursorPos}\n" +
                    $"CURRENT_MATRIX: {currentMatrix}\n" +
                    $"OFF: {off}\n" +
                    $"SCA: {sca}\n" +
                    $"CURRENT_TOP_LEFT: {topleft2}\n" +
                    $"CURRENT_BOTTOM_RIGHT: {bottomright2}";
                args.DrawingSession.DrawText(DebugText, 0.0f, 0.0f, Colors.White, canvasTextFormat);
            }
        }

        // some random sigmoid function, copy&pasted from stackoverflow
        public static float Sigmoid(float value)
        {
            return 1.0f / (1.0f + (float)Math.Exp(-value));
        }
        private void MainCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            // lerp current render matrix to target matrix
            actualMatrix =
                Matrix3x2.CreateScale(targetScale, targetScale) *
                Matrix3x2.CreateTranslation(panning) *
                Matrix3x2.CreateTranslation(offset);
            currentMatrix = Matrix3x2.Lerp(currentMatrix, actualMatrix, 0.2f);

            for (int i = 0; i < count; i++)
            {
                // boundary calculation of current(with lerp) render transform matrix
                Vector2 off; // current offset vector with lerp
                off.X = currentMatrix.M31;
                off.Y = currentMatrix.M32;

                Vector2 sca; // current scale vector with lerp
                sca.X = currentMatrix.M11;
                sca.Y = currentMatrix.M22;

                // calculate current boundary with zoom, panning and lerp of matrix
                Vector2 topleft = (Vector2.Zero - off) / sca;
                Vector2 bottomright = topleft + (renderSize / sca);

                // decide almond's next speed vector with randomness
                Vector2 cursorDiff = cursorPos - test[i].position;
                Vector2 cursorPower;
                float diffR = cursorDiff.Length();
                float diffRpow2 = diffR * diffR;
                float powerConst = 500.0f;
                cursorPower.X = cursorDiff.X * (powerConst / diffRpow2);
                cursorPower.Y = cursorDiff.Y * (powerConst / diffRpow2);
                test[i].GenerateRandomness(topleft, bottomright, -cursorPower);
            }
        }

        private void MainCanvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            // creation of circle geometry
            CanvasGeometry geometry1 = CanvasGeometry.CreateCircle(sender, 0.0f, 0.0f, 2.0f);
            canvasCachedGeometry = CanvasCachedGeometry.CreateFill(geometry1);

            // creation of line color(for the test purpose)
            //transW = Color.FromArgb(128, 255, 255, 255);
            
            // create array of almond, number of almond refers to static int value named 'count'
            test = new Almond[count];
            for (int i = 0; i < count; i++)
            {
                // create new almond, 
                test[i] = new Almond(randomMachine, 900.0f, 450.0f);

                // give random color to almond
                test[i].col = Color.FromArgb(255, (byte)randomMachine.Next(255), (byte)randomMachine.Next(255), (byte)randomMachine.Next(255)); 
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            renderSize = MainCanvas.RenderSize.ToVector2();
        }

        private void MainCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (MainCanvas.IsEnabled)
            {
                renderSize = MainCanvas.RenderSize.ToVector2();

                var wheelDelta = (float)e.GetCurrentPoint(MainGrid).Properties.MouseWheelDelta;
                Vector2 mousePos = e.GetCurrentPoint(MainCanvas).Position.ToVector2();
                float delta = (wheelDelta < 0.0f ? 0.91f : 1.1f);
                float newScale = targetScale * delta;
                float oldScale = targetScale;
                targetScale = Math.Max(minScale, Math.Min(maxScale, newScale));

                Vector2 topLeft = (Vector2.Zero - panning - offset) / targetScale;
                Vector2 bottomRight = topLeft + (renderSize / targetScale);
                cursorPos = (mousePos / targetScale) + topLeft;
                Vector2 viewportSize = bottomRight - topLeft;
                Vector2 cursorRatio = Vector2.Zero;
                cursorRatio = cursorPos / viewportSize;

                Vector2 amount;
                amount.X = (viewportSize.X * oldScale) - (viewportSize.X * targetScale);
                amount.Y = (viewportSize.Y * oldScale) - (viewportSize.Y * targetScale);

                offset += amount * cursorRatio;
            }
        }

        private void MainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            prev = e.GetCurrentPoint(MainCanvas).Position.ToVector2();
            pressed = true;
        }

        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (MainCanvas.IsEnabled)
            {
                Vector2 mousePos = e.GetCurrentPoint(MainCanvas).Position.ToVector2();
                Vector2 topLeft = (Vector2.Zero - panning - offset) / targetScale;
                cursorPos = (mousePos / targetScale) + topLeft;

            }
            if (pressed)
            {
                Vector2 now = e.GetCurrentPoint(MainCanvas).Position.ToVector2();
                Vector2 gap = now - prev;
                panning += gap;
                prev = now;
            }
        }

        private void MainCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            prev = Vector2.Zero;
            pressed = false;
        }
    }
}
