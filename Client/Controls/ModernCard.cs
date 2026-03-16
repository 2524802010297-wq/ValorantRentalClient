using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernCard : Panel
    {
        private Label titleLabel;
        private Label valueLabel;
        private PictureBox iconBox;
        private Color accentColor = Theme.Primary;
        private string title = "";
        private string cardValue = "";
        private Image icon = null;

        public ModernCard()
        {
            this.Size = new Size(250, 150);
            this.BackColor = Color.Transparent;
            this.DoubleBuffered = true;
            this.Padding = new Padding(20);

            titleLabel = new Label
            {
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(20, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            valueLabel = new Label
            {
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(20, 45),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            iconBox = new PictureBox
            {
                Size = new Size(50, 50),
                Location = new Point(this.Width - 70, 20),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            this.Controls.Add(titleLabel);
            this.Controls.Add(valueLabel);
            this.Controls.Add(iconBox);

            this.Paint += ModernCard_Paint;
            this.Resize += (s, e) =>
            {
                iconBox.Location = new Point(this.Width - 70, 20);
            };
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                titleLabel.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value
        {
            get { return cardValue; }
            set
            {
                cardValue = value;
                valueLabel.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                iconBox.Image = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor
        {
            get { return accentColor; }
            set { accentColor = value; this.Invalidate(); }
        }

        private void ModernCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            // Vẽ nền mờ
            using (var path = GetRoundedRectangle(rect, Theme.CardRadius))
            using (var brush = new SolidBrush(Color.FromArgb(200, Theme.Surface)))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Vẽ accent bar bên trái
            using (var brush = new SolidBrush(accentColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, 5, this.Height);
            }

            // Vẽ viền sáng
            using (var pen = new Pen(Color.FromArgb(30, Color.White), 1))
            using (var path = GetRoundedRectangle(rect, Theme.CardRadius))
            {
                e.Graphics.DrawPath(pen, path);
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