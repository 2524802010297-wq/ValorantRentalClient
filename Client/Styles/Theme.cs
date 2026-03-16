using System.Drawing;

namespace ValorantRentalClient.Styles
{
    public static class Theme
    {
        // Màu sắc chủ đạo
        public static Color Primary = Color.FromArgb(255, 70, 85);    // Đỏ Valorant
        public static Color Secondary = Color.FromArgb(15, 25, 35);   // Xám đen
        public static Color Accent = Color.FromArgb(0, 255, 255);     // Cyan
        public static Color Success = Color.FromArgb(0, 200, 100);    // Xanh lá
        public static Color Warning = Color.FromArgb(255, 180, 0);    // Vàng cam
        public static Color Danger = Color.FromArgb(255, 50, 50);     // Đỏ tươi
        public static Color Info = Color.FromArgb(50, 150, 255);      // Xanh dương

        // Màu nền
        public static Color Background = Color.FromArgb(10, 15, 25);  // Đen xanh đậm
        public static Color Surface = Color.FromArgb(20, 30, 40);     // Xám đen
        public static Color SurfaceLight = Color.FromArgb(30, 40, 50); // Xám sáng
        public static Color SurfaceDark = Color.FromArgb(5, 10, 15);  // Đen tuyền

        // Màu chữ
        public static Color TextPrimary = Color.White;
        public static Color TextSecondary = Color.FromArgb(180, 190, 200);
        public static Color TextMuted = Color.FromArgb(120, 130, 140);

        // Font chữ
        public static Font TitleFont = new Font("Segoe UI", 32, FontStyle.Bold);
        public static Font HeaderFont = new Font("Segoe UI", 24, FontStyle.Bold);
        public static Font SubHeaderFont = new Font("Segoe UI", 18, FontStyle.Bold);
        public static Font LargeFont = new Font("Segoe UI", 14, FontStyle.Bold);
        public static Font NormalFont = new Font("Segoe UI", 11);
        public static Font SmallFont = new Font("Segoe UI", 9);

        // Bo góc - THÊM CÁC ĐỊNH NGHĨA NÀY
        public static int CornerRadius = 20;        // Bo góc chung
        public static int CardRadius = 15;          // Bo góc cho card
        public static int ButtonRadius = 10;        // Bo góc cho button
        public static int InputRadius = 8;           // Bo góc cho input (THÊM DÒNG NÀY)

        // Kích thước
        public static int SidebarWidth = 280;
        public static int TitleBarHeight = 60;
    }
}