using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernTextBox : TextBox
    {
        private int borderRadius = Theme.InputRadius;  // Giờ đã có định nghĩa
        private Color borderColor = Color.FromArgb(50, Theme.Primary);
        private Color focusColor = Theme.Primary;
        private bool isFocused = false;

        public ModernTextBox()
        {
            this.BorderStyle = BorderStyle.None;
            this.BackColor = Theme.SurfaceLight;
            this.ForeColor = Theme.TextPrimary;
            this.Font = Theme.NormalFont;
            this.Padding = new Padding(15, 12, 15, 12);
            this.Size = new Size(300, 45);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var path = GetRoundedRectangle(this.ClientRectangle, borderRadius))
            using (var pen = new Pen(isFocused ? focusColor : borderColor, 2))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            isFocused = true;
            this.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            isFocused = false;
            this.Invalidate();
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