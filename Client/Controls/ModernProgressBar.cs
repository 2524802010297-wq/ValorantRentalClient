using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernProgressBar : ProgressBar
    {
        private Color progressColor = Theme.Primary;
        private Color trackColor = Theme.SurfaceLight;
        private int borderRadius = 10;
        private int _value = 0;

        public ModernProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;
            this.Height = 20;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ProgressColor
        {
            get { return progressColor; }
            set { progressColor = value; this.Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TrackColor
        {
            get { return trackColor; }
            set { trackColor = value; this.Invalidate(); }
        }

        // Thêm attribute để tắt warning
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                base.Value = _value; // Sửa lại thành base.Value
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            // Vẽ track (nền)
            using (var path = GetRoundedRectangle(rect, borderRadius))
            using (var brush = new SolidBrush(trackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Vẽ progress (thanh tiến trình)
            if (_value > 0)
            {
                int progressWidth = (int)((float)_value / 100 * rect.Width);
                if (progressWidth > 0)
                {
                    Rectangle progressRect = new Rectangle(rect.X, rect.Y, progressWidth, rect.Height);

                    using (var path = GetRoundedRectangle(progressRect, borderRadius))
                    using (var brush = new LinearGradientBrush(
                        progressRect,
                        progressColor,
                        ControlPaint.Light(progressColor, 0.3f),
                        LinearGradientMode.Horizontal))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    // Vẽ text % ở cuối thanh progress
                    string percent = $"{_value}%";
                    using (var font = new Font("Segoe UI", 8, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        float textX = progressWidth - 35;
                        if (textX > 10)
                        {
                            e.Graphics.DrawString(percent, font, brush, textX, 3);
                        }
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