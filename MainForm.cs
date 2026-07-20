using System.Data;

namespace GameEncyclopedia;

public class MainForm : Form
{
    private DataGridView grid = new();
    private TextBox searchBox = new();
    private ComboBox categoryBox = new();
    private Button addButton = new();
    private Button editButton = new();
    private Button deleteButton = new();
    private Button galleryButton = new();
    private Button mediaButton = new();
    private Button refreshButton = new();
    private DataTable table = new();
    private DataView view = new();

    public MainForm()
    {
        Text = "Енциклопедія відеоігор";
        Width = 980;
        Height = 620;
        StartPosition = FormStartPosition.CenterScreen;

        var top = new Panel { Dock = DockStyle.Top, Height = 58 };
        searchBox.SetBounds(12, 16, 220, 26);
        searchBox.PlaceholderText = "Пошук гри...";
        categoryBox.SetBounds(245, 16, 160, 26);
        addButton.Text = "Додати";
        editButton.Text = "Редагувати";
        deleteButton.Text = "Видалити";
        galleryButton.Text = "Галерея";
        mediaButton.Text = "Деталі";
        refreshButton.Text = "Оновити";
        addButton.SetBounds(420, 14, 90, 30);
        editButton.SetBounds(518, 14, 105, 30);
        deleteButton.SetBounds(631, 14, 95, 30);
        galleryButton.SetBounds(734, 14, 90, 30);
        mediaButton.SetBounds(832, 14, 90, 30);
        refreshButton.SetBounds(12, 44, 90, 25);
        top.Height = 75;
        top.Controls.AddRange([searchBox, categoryBox, addButton, editButton, deleteButton, galleryButton, mediaButton, refreshButton]);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        Controls.Add(grid);
        Controls.Add(top);

        Load += (_, _) => { LoadCategories(); LoadData(); };
        searchBox.TextChanged += (_, _) => ApplySearch();
        categoryBox.SelectedIndexChanged += (_, _) => LoadData();
        addButton.Click += (_, _) => AddGame();
        editButton.Click += (_, _) => EditGame();
        deleteButton.Click += (_, _) => DeleteGame();
        galleryButton.Click += (_, _) => new GalleryForm().ShowDialog();
        mediaButton.Click += (_, _) => OpenMedia();
        refreshButton.Click += (_, _) => LoadData();
        grid.CellDoubleClick += (_, _) => OpenMedia();
        grid.CellFormatting += Grid_CellFormatting;
        FormClosing += MainForm_FormClosing;
    }

    private void LoadCategories()
    {
        var cats = Database.LoadCategories();
        var row = cats.NewRow();
        row["category_id"] = 0;
        row["category_name"] = "Усі жанри";
        cats.Rows.InsertAt(row, 0);
        categoryBox.DataSource = cats;
        categoryBox.DisplayMember = "category_name";
        categoryBox.ValueMember = "category_id";
    }

    private void LoadData()
    {
        int cat = 0;
        if (categoryBox.SelectedValue != null && categoryBox.SelectedValue is not DataRowView)
        {
            cat = Convert.ToInt32(categoryBox.SelectedValue);
        }
        table = Database.LoadTable(cat);
        view = new DataView(table);
        grid.DataSource = view;
        if (grid.Columns.Contains("item_id")) grid.Columns["item_id"].HeaderText = "ID";
        if (grid.Columns.Contains("title")) grid.Columns["title"].HeaderText = "Назва";
        if (grid.Columns.Contains("release_year")) grid.Columns["release_year"].HeaderText = "Рік";
        if (grid.Columns.Contains("genre")) grid.Columns["genre"].HeaderText = "Жанр";
        if (grid.Columns.Contains("category_name")) grid.Columns["category_name"].HeaderText = "Категорія";
        foreach (var name in new[] { "cover_image_path", "media_path", "html_desc_path", "category_id" })
            if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
        ApplySearch();
    }

    private void ApplySearch()
    {
        string s = searchBox.Text.Replace("'", "''");
        view.RowFilter = string.IsNullOrWhiteSpace(s) ? "" : $"title LIKE '%{s}%' OR genre LIKE '%{s}%'";
    }

    private GameItem? SelectedItem()
    {
        if (grid.CurrentRow?.DataBoundItem is not DataRowView rv) return null;
        return Database.RowToItem(rv.Row);
    }

    private void AddGame()
    {
        using var f = new EditForm(null);
        if (f.ShowDialog() == DialogResult.OK) LoadData();
    }

    private void EditGame()
    {
        var item = SelectedItem();
        if (item == null) return;
        using var f = new EditForm(item);
        if (f.ShowDialog() == DialogResult.OK) LoadData();
    }

    private void DeleteGame()
    {
        var item = SelectedItem();
        if (item == null) return;
        if (MessageBox.Show("Видалити гру?", "Підтвердження", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Database.DeleteItem(item.Id);
            LoadData();
        }
    }

    private void OpenMedia()
    {
        var item = SelectedItem();
        if (item == null) return;
        new MediaForm(item).ShowDialog();
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (grid.Rows[e.RowIndex].DataBoundItem is not DataRowView rv) return;
        int year = Convert.ToInt32(rv["release_year"]);
        if (year < 2012) grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
        if (year >= 2020) grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Honeydew;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        var ans = MessageBox.Show("Вийти з програми?", "Вихід", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (ans != DialogResult.Yes) e.Cancel = true;
    }
}
