using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernPanel : Panel
    {
        private int borderRadius = Theme.CornerRadius;
        private float opacity = 0.85f;
        private bool hasGradient = false;
        private Color gradientStart = Theme.Primary;
        private Color gradientEnd = Theme.Accent;
        private bool hasBorder = true;
        private Color borderColor = Color.FromArgb(30, Color.White);

        public ModernPanel()
        {
            this.BackColor = Theme.Surface;
            this.DoubleBuffered = true;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get { return borderRadius; }
            set { borderRadius = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Opacity
        {
            get { return opacity; }
            set { opacity = Math.Max(0, Math.Min(1, value)); this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasGradient
        {
            get { return hasGradient; }
            set { hasGradient = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GradientStart
        {
            get { return gradientStart; }
            set { gradientStart = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GradientEnd
        {
            get { return gradientEnd; }
            set { gradientEnd = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasBorder
        {
            get { return hasBorder; }
            set { hasBorder = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; this.Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(this.Parent?.BackColor ?? Theme.Background);

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (var path = GetRoundedRectangle(rect, borderRadius))
            {
                if (hasGradient)
                {
                    using (var brush = new LinearGradientBrush(rect, gradientStart, gradientEnd, LinearGradientMode.ForwardDiagonal))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(Color.FromArgb((int)(255 * opacity), this.BackColor)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }

                if (hasBorder)
                {
                    using (var pen = new Pen(borderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}