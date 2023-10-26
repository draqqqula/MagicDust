﻿using MagicDustLibrary.Logic;
using Microsoft.Xna.Framework;


namespace MagicDustLibrary.Display
{
    public record struct CameraSettings(Vector2 defaultPosition, Rectangle innerWindow, Rectangle? outerWindow);
    public class GameCamera
    {
        public Vector2 Position { get; private set; }

        public Vector2 LeftTopCorner { get { return new Vector2(Position.X - ViewPort.Width / 2, Position.Y - ViewPort.Height / 2); } }
        public Vector2 RightTopCorner { get { return new Vector2(Position.X + ViewPort.Width / 2, Position.Y - ViewPort.Height / 2); } }
        public Vector2 LeftBottomCorner { get { return new Vector2(Position.X - ViewPort.Width / 2, Position.Y + ViewPort.Height / 2); } }
        public Vector2 RightBottomCorner { get { return new Vector2(Position.X + ViewPort.Width / 2, Position.Y + ViewPort.Height / 2); } }

        private const float CatchDistance = 0.4f;


        /// <summary>
        /// применяет паралакс к абсолютной позиции объекта
        /// паралакс представляет собой коэффициент изменения абсолютной позиции объекта относительно наблюдателя
        /// </summary>
        /// <param newPriority="position"></param>
        /// <param newPriority="dx"></param>
        /// <param newPriority="dy"></param>
        /// <returns></returns>
        public DrawingParameters ApplyParalax(DrawingParameters arguments, float dx, float dy)
        {
            return arguments with { Position = arguments.Position - new Vector2((int)(LeftTopCorner.X * dx), (int)(LeftTopCorner.Y * dy)) };
        }

        public GameObject TargetObject { get; private set; }
        /// <summary>
        /// внутренние границы, закреплены на цели
        /// положение камеры не может выйти за их пределы
        /// </summary>
        private Rectangle InnerBorders { get; set; }
        /// <summary>
        /// внешние границы за которыми камера не может видеть
        /// </summary>
        private Rectangle? OuterBorders { get; set; }
        /// <summary>
        /// область пространства, попадающая в поле зрения камеры
        /// </summary>
        public Rectangle ViewPort
        {
            get
            {
                return new Rectangle
                {
                    Location = new Point((int)Position.X - ClientBounds.Width / 2, (int)Position.Y - ClientBounds.Height / 2),
                    Height = ClientBounds.Height,
                    Width = ClientBounds.Width
                };
            }
        }
        /// <summary>
        /// границы экрана игрока
        /// </summary>
        public Rectangle ClientBounds { get; private set; }

        public Action<GameClient> OnClientUpdated = delegate { };
        private void UpdateWindow(GameClient client)
        {
            ClientBounds = client.Window;
        }

        private float InterpolationFactor(float dt)
        {
            return 1f - MathF.Pow(1 - 0.95f, dt / 0.4f);
        }

        public GameCamera(CameraSettings settings, Rectangle clientBorders) :
            this(settings.defaultPosition, settings.innerWindow, settings.outerWindow, clientBorders)
        { }

        private GameCamera(Vector2 position, Rectangle innerBorders, Rectangle? outerBorders, Rectangle clientBorders)
        {
            Position = position;
            InnerBorders = innerBorders;
            OnClientUpdated += UpdateWindow;
        }
        /// <summary>
        /// устанавливает внешние границы пространства, в котором камера может видеть
        /// </summary>
        /// <param newPriority="innerBorders"></param>
        public void SetOuterBorders(Rectangle borders)
        {
            OuterBorders = borders;
        }

        /// <summary>
        /// true если объект в поле зрения камеры
        /// </summary>
        /// <param newPriority="obj"></param>
        /// <returns></returns>
        public bool Sees(GameObject obj)
        {
            return ViewPort.Intersects(obj.Layout);
        }

        private Vector2 Lerp(Vector2 a, Vector2 b, float k)
        {
            return b + (a - b) * k;
        }

        private Vector2 FitInBorders(Vector2 a, Vector2 b, Rectangle R)
        {
            return new Vector2(
                Math.Min(Math.Max(a.X, b.X - R.Width / 2), b.X + R.Width / 2),
                Math.Min(Math.Max(a.Y, b.Y - R.Height / 2), b.Y + R.Height / 2)
                );
        }

        /// <summary>
        /// заставляет камеру следовать за объектом
        /// </summary>
        /// <param newPriority="targetObject"></param>
        public void LinkTo(GameObject targetObject)
        {
            this.TargetObject = targetObject;
        }

        /// <summary>
        /// обновляет позицию камеры, учитывая интерполяцию, внешние и внутренние границы
        /// </summary>
        public void Update(TimeSpan deltaTime)
        {
            if (TargetObject != null)
            {
                if ((TargetObject.Position - Position).Length() < CatchDistance)
                {
                    Position = TargetObject.Position;
                    return;
                }
                var rawPos = FitInBorders(
                    Lerp(TargetObject.Position, Position, InterpolationFactor((float)deltaTime.TotalSeconds))
                    , TargetObject.Position, InnerBorders);
                if (OuterBorders.HasValue)
                {
                    var outer = OuterBorders.Value;
                    Position = FitInBorders(rawPos, outer.Center.ToVector2(), new Rectangle(new Point(0, 0), new Point(outer.Width - ViewPort.Width, outer.Height - ViewPort.Height)));
                }
                else
                    Position = rawPos;
            }
        }
    }
}
