using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernButton : Button
    {
        private int borderRadius = Theme.ButtonRadius;
        private Color borderColor = Color.FromArgb(50, Theme.Primary);
        private Color hoverColor = Color.FromArgb(255, 100, 115);
        private Color clickColor = Color.FromArgb(200, 50, 65);
        private bool isHovered = false;
        private bool isPressed = false;
        private Timer animationTimer;
        private float animationValue = 0f;

        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = Theme.Primary;
            this.ForeColor = Theme.TextPrimary;
            this.Font = Theme.NormalFont;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(180, 50);
            this.DoubleBuffered = true;

            animationTimer = new Timer { Interval = 20 };
            animationTimer.Tick += (s, e) =>
            {
                if (isPressed && animationValue < 1f)
                    animationValue += 0.1f;
                else if (!isPressed && animationValue > 0f)
                    animationValue -= 0.1f;
                else
                    animationTimer.Stop();

                this.Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(this.Parent.BackColor);

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            // Vẽ nền với gradient
            using (var path = GetRoundedRectangle(rect, borderRadius))
            {
                Color startColor = isPressed ? clickColor : (isHovered ? hoverColor : this.BackColor);
                Color endColor = isPressed ? this.BackColor : (isHovered ? this.BackColor : hoverColor);

                // Thêm hiệu ứng nhấn
                if (animationValue > 0)
                {
                    startColor = ControlPaint.Light(startColor, animationValue * 0.3f);
                    endColor = ControlPaint.Light(endColor, animationValue * 0.3f);
                }

                using (var brush = new LinearGradientBrush(rect, startColor, endColor, LinearGradientMode.Vertical))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Vẽ viền sáng
                using (var pen = new Pen(Color.FromArgb(50, Color.White), 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            // Vẽ icon và text
            string icon = GetIconForText(this.Text);
            string textOnly = this.Text.Replace(icon, "").Trim();

            if (!string.IsNullOrEmpty(icon))
            {
                using (var iconFont = new Font("Segoe UI", this.Font.Size + 4))
                using (var brush = new SolidBrush(Color.FromArgb(200, Color.White)))
                {
                    e.Graphics.DrawString(icon, iconFont, brush, 15, (this.Height - iconFont.Height) / 2 - 2);
                }

                TextRenderer.DrawText(e.Graphics, textOnly, this.Font,
                    new Rectangle(45, 0, this.Width - 45, this.Height),
                    this.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, this.Text, this.Font, this.ClientRectangle,
                    this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private string GetIconForText(string text)
        {
            if (text.Contains("KÍCH HOẠT")) return "🔑";
            if (text.Contains("ĐĂNG NHẬP")) return "🚀";
            if (text.Contains("COPY")) return "📋";
            if (text.Contains("KẾT THÚC")) return "⏹️";
            if (text.Contains("BẮT ĐẦU")) return "▶️";
            if (text.Contains("LÀM MỚI")) return "🔄";
            if (text.Contains("LƯU")) return "💾";
            if (text.Contains("HỦY")) return "✖";
            return "";
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            animationTimer.Start();
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            animationTimer.Start();
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            isPressed = true;
            animationTimer.Start();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            animationTimer.Start();
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