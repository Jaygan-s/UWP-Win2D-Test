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
        public float maxSpeedPerSecond = 10.0f;
        public float randomnessPower = 0.1f;
        public float randomnessAngle = 0.4f;
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
        public void GenerateRandomness(Vector2 boundary)
        {
            // 랜덤한 힘/각도를 생성
            float randomPower = (float)random.NextDouble() - 0.5f;
            float randomAngle = (float)random.NextDouble() - 0.5f;

            // 현재의 힘/각도에 랜덤인자를 더함
            power += Math.Min(randomPower * randomnessPower, maxSpeedPerSecond);
            angle += randomAngle * randomnessAngle;

            // 현재의 힘/각도로 움직일 X, Y 거리 계산
            float amountX = power * (float)Math.Sin(angle);
            float amountY = power * (float)Math.Cos(angle);

            Vector2 newPosition = position;
            newPosition.X += amountX;
            newPosition.Y += amountY;

            if(newPosition.X < 0.0f || newPosition.X > boundary.X 
                || newPosition.Y < 0.0f || newPosition.Y > boundary.Y)
            {
                angle -= (float)Math.PI;
                amountX = power * (float)Math.Sin(angle);
                amountY = power * (float)Math.Cos(angle);
                position.X += amountX;
                position.Y += amountY;
            }
            else
            {
                // 위치를 변경
                position = newPosition;
            }
        }
    }
    /// 

    public sealed partial class MainPage : Page
    {
        static int count = 10000;
        public Random rara = new Random();
        Almond[] test;
        CanvasCachedGeometry[] canvasCachedGeometry = new CanvasCachedGeometry[3];
        DateTime _lastTime; // marks the beginning the measurement began
        int _framesRendered; // an increasing count
        int _fps; // the FPS calculated from the last measurement
        CanvasTextFormat canvasTextFormat = new CanvasTextFormat();
        Vector2 bobo;
        Color transW;
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

        private void CanvasAnimatedControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            _framesRendered++;
            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                // one second has elapsed 
                _fps = _framesRendered;
                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }

            using (CanvasCommandList ccl = new CanvasCommandList(sender))
            {
                using (CanvasDrawingSession cds = ccl.CreateDrawingSession())
                {
                    cds.Transform = currentMatrix;
                    
                    for (int i = 0; i < count; i++)
                    {
                        cds.DrawCachedGeometry(canvasCachedGeometry[1], test[i].position, test[i].col);
                    }
                }
                args.DrawingSession.DrawImage(ccl);
            }

            if(MainCanvas != null)
            {
                Vector2 topleft = (Vector2.Zero - panning - offset) / targetScale;
                Vector2 bottomright = topleft + (renderSize / targetScale);
                string DebugText =
                    $"FPS: {_fps}\n" +
                    $"PARTICLES: {count}\n" +
                    $"TARGET_SCALE: {targetScale}\n" +
                    $"PANNING_OFFSET: {panning}\n" +
                    $"ZOOMING_OFFSET: {offset}\n" +
                    $"RENDER_SIZE: {renderSize}\n" +
                    $"TOP_LEFT_: {topleft}\n" +
                    $"BOTTOM_RIGHT: {bottomright}\n" +
                    $"VIEWPORT_SIZE: {bottomright - topleft}\n" +
                    $"CURSOR_POSITION: {cursorPos}";
                args.DrawingSession.DrawText(DebugText, 0.0f, 0.0f, Colors.White, canvasTextFormat);
            }
        }

        private void MainCanvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            for (int i = 0; i < count; i++)
            {
                test[i].GenerateRandomness(bobo);
            }
            actualMatrix =
                Matrix3x2.CreateScale(targetScale, targetScale) *
                Matrix3x2.CreateTranslation(panning) *
                Matrix3x2.CreateTranslation(offset);
            currentMatrix = Matrix3x2.Lerp(currentMatrix, actualMatrix, 0.2f);
        }

        private void MainCanvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            CanvasGeometry geometry1 = CanvasGeometry.CreateCircle(sender, 0.0f, 0.0f, 2.0f);
            canvasCachedGeometry[1] = CanvasCachedGeometry.CreateFill(geometry1);

            transW = Color.FromArgb(128, 255, 255, 255);
            test = new Almond[count];
            for (int i = 0; i < count; i++)
            {
                test[i] = new Almond(rara, 900.0f, 450.0f);
                test[i].col = Color.FromArgb(255, (byte)rara.Next(255), (byte)rara.Next(255), (byte)rara.Next(255)); 
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            for (int i = 0; i < count; i++)
            {
                bobo = MainCanvas.RenderSize.ToVector2();
            }
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
                Vector2 bottomRight = topLeft + (renderSize / targetScale);
                cursorPos = (mousePos / targetScale) + topLeft;
                Vector2 viewportSize = bottomRight - topLeft;

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
