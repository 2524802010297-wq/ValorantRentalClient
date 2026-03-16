using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient.Controls
{
    public class ModernTabControl : TabControl
    {
        private Color backColor = Theme.SurfaceDark;
        private Color tabBackColor = Theme.Surface;
        private Color selectedTabColor = Theme.Primary;
        private Color hoverColor = Color.FromArgb(40, 50, 60);

        public ModernTabControl()
        {
            this.SizeMode = TabSizeMode.Fixed;
            this.ItemSize = new Size(Theme.SidebarWidth, 70);
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.Font = Theme.LargeFont;
            this.Padding = new Point(20, 15);
            this.Alignment = TabAlignment.Left;
            this.Multiline = true;
            this.DoubleBuffered = true;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            Rectangle tabRect = this.GetTabRect(e.Index);
            bool isSelected = (this.SelectedIndex == e.Index);
            bool isHovered = tabRect.Contains(this.PointToClient(Cursor.Position));

            // Vẽ nền tab
            using (var brush = new SolidBrush(isSelected ? selectedTabColor : (isHovered ? hoverColor : tabBackColor)))
            {
                e.Graphics.FillRectangle(brush, tabRect);
            }

            // Icon và text cho từng tab
            string[][] tabData = new string[][] {
                new string[] { "🏠", "Trang chủ" },
                new string[] { "🔑", "Kích hoạt" },
                new string[] { "🎮", "Chơi game" },
                new string[] { "📊", "Lịch sử" },
                new string[] { "📘", "Hướng dẫn" },
                new string[] { "⚙", "Cài đặt" }
            };

            if (e.Index < tabData.Length)
            {
                // Vẽ icon
                using (var iconFont = new Font("Segoe UI", 24))
                using (var brush = new SolidBrush(isSelected ? Color.White : Theme.TextSecondary))
                {
                    e.Graphics.DrawString(tabData[e.Index][0], iconFont, brush, tabRect.X + 20, tabRect.Y + 18);
                }

                // Vẽ text
                using (var brush = new SolidBrush(isSelected ? Color.White : Theme.TextSecondary))
                {
                    e.Graphics.DrawString(tabData[e.Index][1], this.Font, brush, tabRect.X + 70, tabRect.Y + 22);
                }

                // Vẽ indicator bên cạnh tab được chọn
                if (isSelected)
                {
                    using (var brush = new SolidBrush(Theme.Accent))
                    {
                        e.Graphics.FillRectangle(brush, tabRect.X, tabRect.Y + 15, 4, 40);
                    }
                }
            }
        }
    }
}