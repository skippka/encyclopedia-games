namespace GameEncyclopedia;

public class EditForm : Form
{
    private TextBox titleBox = new();
    private NumericUpDown yearBox = new();
    private TextBox genreBox = new();
    private ComboBox categoryBox = new();
    private TextBox coverBox = new();
    private TextBox mediaBox = new();
    private TextBox htmlBox = new();
    private GameItem item;

    public EditForm(GameItem? oldItem)
    {
        item = oldItem ?? new GameItem();
        Text = oldItem == null ? "Додати гру" : "Редагувати гру";
        Width = 560;
        Height = 390;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        AddLabel("Назва:", 20, 24);
        titleBox.SetBounds(135, 20, 360, 25);
        AddLabel("Рік:", 20, 62);
        yearBox.SetBounds(135, 58, 120, 25);
        yearBox.Minimum = 1980;
        yearBox.Maximum = 2035;
        AddLabel("Жанр:", 20, 100);
        genreBox.SetBounds(135, 96, 230, 25);
        AddLabel("Категорія:", 20, 138);
        categoryBox.SetBounds(135, 134, 230, 25);
        AddFileRow("Обкладинка:", coverBox, 176, "Image files|*.png;*.jpg;*.jpeg;*.bmp");
        AddFileRow("Відео:", mediaBox, 214, "Media files|*.mp4;*.avi;*.wmv;*.mp3|All files|*.*");
        AddFileRow("HTML опис:", htmlBox, 252, "HTML files|*.html;*.htm");

        var save = new Button { Text = "Зберегти", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Скасувати", DialogResult = DialogResult.Cancel };
        save.SetBounds(310, 305, 100, 30);
        cancel.SetBounds(420, 305, 100, 30);
        Controls.AddRange([save, cancel]);
        AcceptButton = save;
        CancelButton = cancel;

        Load += (_, _) => LoadForm();
        save.Click += Save_Click;
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label { Text = text, Left = x, Top = y, Width = 100 });
    }

    private void AddFileRow(string label, TextBox box, int y, string filter)
    {
        AddLabel(label, 20, y + 4);
        box.SetBounds(135, y, 280, 25);
        var btn = new Button { Text = "...", Left = 425, Top = y, Width = 35, Height = 25 };
        btn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = filter };
            if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.FileName;
        };
        Controls.AddRange([box, btn]);
    }

    private void LoadForm()
    {
        var cats = Database.LoadCategories();
        categoryBox.DataSource = cats;
        categoryBox.DisplayMember = "category_name";
        categoryBox.ValueMember = "category_id";

        titleBox.Text = item.Title;
        yearBox.Value = item.ReleaseYear == 0 ? 2020 : item.ReleaseYear;
        genreBox.Text = item.Genre;
        coverBox.Text = item.CoverPath;
        mediaBox.Text = item.MediaPath;
        htmlBox.Text = item.HtmlPath;
        if (item.CategoryId != 0) categoryBox.SelectedValue = item.CategoryId;
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(titleBox.Text) || string.IsNullOrWhiteSpace(genreBox.Text))
        {
            MessageBox.Show("Заповніть назву і жанр.");
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(coverBox.Text) || string.IsNullOrWhiteSpace(htmlBox.Text))
        {
            MessageBox.Show("Потрібна обкладинка і HTML опис.");
            DialogResult = DialogResult.None;
            return;
        }

        item.Title = titleBox.Text.Trim();
        item.ReleaseYear = (int)yearBox.Value;
        item.Genre = genreBox.Text.Trim();
        item.CategoryId = Convert.ToInt32(categoryBox.SelectedValue);
        string cover = PrepareFile(coverBox.Text, item.CoverPath, "covers");
        string media = string.IsNullOrWhiteSpace(mediaBox.Text) ? "" : PrepareFile(mediaBox.Text, item.MediaPath, "media");
        string html = PrepareFile(htmlBox.Text, item.HtmlPath, "html");
        if (DialogResult == DialogResult.None) return;

        item.CoverPath = cover;
        item.MediaPath = media;
        item.HtmlPath = html;
        Database.SaveItem(item);
    }

    private string PrepareFile(string value, string oldValue, string folder)
    {
        if (value == oldValue && !Path.IsPathRooted(value)) return value;
        if (!File.Exists(value))
        {
            MessageBox.Show("Файл не знайдено: " + value);
            DialogResult = DialogResult.None;
            return oldValue;
        }
        return Database.CopyToAssets(value, folder);
    }
}
