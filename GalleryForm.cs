namespace GameEncyclopedia;

public class GalleryForm : Form
{
    private PictureBox big = new();
    private FlowLayoutPanel strip = new();
    private Button prev = new();
    private Button next = new();
    private Button slide = new();
    private System.Windows.Forms.Timer timer = new();
    private List<GameItem> items = [];
    private List<string> slideImages = [];
    private int index = 0;
    private int slideIndex = 0;

    public GalleryForm()
    {
        Text = "Галерея обкладинок";
        Width = 850;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        big.Dock = DockStyle.Fill;
        big.SizeMode = PictureBoxSizeMode.Zoom;
        big.BackColor = Color.FromArgb(28, 31, 38);
        big.DoubleClick += (_, _) => OpenCurrent();

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 125 };
        prev.Text = "<";
        next.Text = ">";
        slide.Text = "Слайд-шоу";
        prev.SetBounds(10, 45, 45, 34);
        next.SetBounds(785, 45, 45, 34);
        slide.SetBounds(665, 45, 105, 34);
        strip.SetBounds(65, 10, 590, 105);
        strip.AutoScroll = false;
        bottom.Controls.AddRange([prev, next, slide, strip]);

        Controls.Add(big);
        Controls.Add(bottom);

        Load += (_, _) =>
        {
            items = Database.LoadItems();
            LoadSlideImages();
            ShowItem();
        };
        prev.Click += (_, _) => MoveIndex(-1);
        next.Click += (_, _) => MoveIndex(1);
        slide.Click += (_, _) =>
        {
            timer.Enabled = !timer.Enabled;
            slide.Text = timer.Enabled ? "Стоп" : "Слайд-шоу";
            if (timer.Enabled) ShowSlide();
            else ShowItem();
        };
        timer.Interval = 1800;
        timer.Tick += (_, _) => NextSlide();
    }

    private void MoveIndex(int step)
    {
        if (items.Count == 0) return;
        timer.Enabled = false;
        slide.Text = "Слайд-шоу";
        index += step;
        if (index < 0) index = items.Count - 1;
        if (index >= items.Count) index = 0;
        ShowItem();
    }

    private void LoadSlideImages()
    {
        slideImages.Clear();
        string folder = Path.Combine(Database.BaseFolder, "Assets", "slideshow");
        AddSlideFiles(folder);

        string htmlImages = Path.Combine(Database.BaseFolder, "Assets", "html", "image");
        AddSlideFiles(htmlImages);
    }

    private void AddSlideFiles(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.GetFiles(folder))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                slideImages.Add(file);
            }
        }
    }

    private void NextSlide()
    {
        if (slideImages.Count == 0)
        {
            MoveIndex(1);
            return;
        }
        slideIndex++;
        if (slideIndex >= slideImages.Count) slideIndex = 0;
        ShowSlide();
    }

    private void ShowSlide()
    {
        if (slideImages.Count == 0) return;
        Text = "Галерея: слайд-шоу";
        LoadPictureFromFullPath(big, slideImages[slideIndex]);
    }

    private void ShowItem()
    {
        if (items.Count == 0) return;
        var item = items[index];
        Text = "Галерея: " + item.Title;
        LoadPicture(big, item.CoverPath);
        MakeStrip();
    }

    private void MakeStrip()
    {
        strip.Controls.Clear();
        int start = Math.Max(0, index - 2);
        if (start + 5 > items.Count) start = Math.Max(0, items.Count - 5);

        for (int i = start; i < Math.Min(items.Count, start + 5); i++)
        {
            var pic = new PictureBox();
            pic.Width = 100;
            pic.Height = 100;
            pic.Margin = new Padding(6, 2, 6, 2);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BorderStyle = i == index ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
            pic.Tag = i;
            LoadPicture(pic, items[i].CoverPath);
            pic.Click += (_, _) =>
            {
                timer.Enabled = false;
                slide.Text = "Слайд-шоу";
                index = (int)pic.Tag;
                ShowItem();
            };
            pic.DoubleClick += (_, _) => OpenCurrent();
            strip.Controls.Add(pic);
        }
    }

    private void LoadPicture(PictureBox pic, string rel)
    {
        string path = Database.FullPath(rel);
        if (!File.Exists(path)) return;
        LoadPictureFromFullPath(pic, path);
    }

    private void LoadPictureFromFullPath(PictureBox pic, string path)
    {
        if (pic.Image != null) pic.Image.Dispose();
        try
        {
            using var img = Image.FromFile(path);
            pic.Image = MakeSmallImage(img, Math.Max(pic.Width, 900), Math.Max(pic.Height, 600));
        }
        catch
        {
            pic.Image = null;
        }
    }

    private Image MakeSmallImage(Image img, int maxW, int maxH)
    {
        double k1 = (double)maxW / img.Width;
        double k2 = (double)maxH / img.Height;
        double k = Math.Min(1, Math.Min(k1, k2));
        int w = Math.Max(1, (int)(img.Width * k));
        int h = Math.Max(1, (int)(img.Height * k));

        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(img, 0, 0, w, h);
        return bmp;
    }

    private void OpenCurrent()
    {
        if (items.Count == 0) return;
        new MediaForm(items[index]).ShowDialog();
    }
}
