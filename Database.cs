using Microsoft.Data.Sqlite;
using System.Data;

namespace GameEncyclopedia;

public static class Database
{
    public static string BaseFolder => AppDomain.CurrentDomain.BaseDirectory;
    public static string AssetsFolder => Path.Combine(BaseFolder, "Assets");
    public static string DbPath => Path.Combine(BaseFolder, "games.db");
    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Prepare()
    {
        Directory.CreateDirectory(AssetsFolder);
        Directory.CreateDirectory(Path.Combine(AssetsFolder, "covers"));
        Directory.CreateDirectory(Path.Combine(AssetsFolder, "html"));

        using var con = new SqliteConnection(ConnectionString);
        con.Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS categories (
            category_id INTEGER PRIMARY KEY AUTOINCREMENT,
            category_name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS items (
            item_id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            release_year INTEGER NOT NULL,
            genre TEXT NOT NULL,
            cover_image_path TEXT NOT NULL,
            media_path TEXT,
            html_desc_path TEXT NOT NULL,
            category_id INTEGER NOT NULL,
            FOREIGN KEY(category_id) REFERENCES categories(category_id)
        );
        """;
        cmd.ExecuteNonQuery();

        FillStartData(con);
    }

    private static void FillStartData(SqliteConnection con)
    {
        var countCmd = con.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM items";
        var count = Convert.ToInt32(countCmd.ExecuteScalar());
        if (count > 0) return;

        string[] cats = ["RPG", "Екшен", "Стратегія", "Пригоди"];
        foreach (var cat in cats)
        {
            var c = con.CreateCommand();
            c.CommandText = "INSERT INTO categories(category_name) VALUES($name)";
            c.Parameters.AddWithValue("$name", cat);
            c.ExecuteNonQuery();
        }

        var games = new (string title, int year, string genre, int cat)[]
        {
            ("The Witcher 3", 2015, "RPG", 1),
            ("Minecraft", 2011, "Пісочниця", 4),
            ("Portal 2", 2011, "Головоломка", 4),
            ("Half-Life 2", 2004, "Шутер", 2),
            ("Stardew Valley", 2016, "Симулятор", 4),
            ("Civilization VI", 2016, "Стратегія", 3),
            ("Hades", 2020, "Roguelike", 2),
            ("Elden Ring", 2022, "Action RPG", 1),
            ("Terraria", 2011, "Пригоди", 4),
            ("Cyberpunk 2077", 2020, "RPG", 1)
        };

        int i = 1;
        foreach (var g in games)
        {
            var cover = MakeCover(g.title, i);
            var html = MakeHtml(g.title, g.year, g.genre);

            var cmd = con.CreateCommand();
            cmd.CommandText =
            """
            INSERT INTO items(title, release_year, genre, cover_image_path, media_path, html_desc_path, category_id)
            VALUES($title, $year, $genre, $cover, '', $html, $cat)
            """;
            cmd.Parameters.AddWithValue("$title", g.title);
            cmd.Parameters.AddWithValue("$year", g.year);
            cmd.Parameters.AddWithValue("$genre", g.genre);
            cmd.Parameters.AddWithValue("$cover", cover);
            cmd.Parameters.AddWithValue("$html", html);
            cmd.Parameters.AddWithValue("$cat", g.cat);
            cmd.ExecuteNonQuery();
            i++;
        }
    }

    private static string MakeCover(string title, int number)
    {
        string rel = Path.Combine("Assets", "covers", $"cover_{number}.png");
        string full = Path.Combine(BaseFolder, rel);
        if (File.Exists(full)) return rel;

        using var bmp = new Bitmap(280, 390);
        using var g = Graphics.FromImage(bmp);
        var colors = new[] { Color.FromArgb(45, 78, 130), Color.FromArgb(95, 45, 120), Color.FromArgb(30, 120, 90), Color.FromArgb(140, 70, 45) };
        g.Clear(colors[number % colors.Length]);
        using var brush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 22, FontStyle.Bold);
        using var small = new Font("Segoe UI", 12);
        g.DrawString(title, font, brush, new RectangleF(18, 95, 240, 150));
        g.DrawString("Енциклопедія ігор", small, brush, 18, 335);
        bmp.Save(full);
        return rel;
    }

    private static string MakeHtml(string title, int year, string genre)
    {
        string rel = Path.Combine("Assets", "html", MakeFileName(title) + ".html");
        string full = Path.Combine(BaseFolder, rel);
        if (File.Exists(full)) return rel;

        File.WriteAllText(full,
        $"""
        <!doctype html>
        <html lang="uk">
        <head>
            <meta charset="utf-8">
            <style>
                body {{ font-family: Segoe UI, Arial; margin: 24px; background: #f7f7f7; color: #222; }}
                h1 {{ color: #20304a; }}
                .box {{ background: white; padding: 16px; border-left: 5px solid #4b75bd; }}
            </style>
        </head>
        <body>
            <h1>{title}</h1>
            <div class="box">
                <p><b>Рік виходу:</b> {year}</p>
                <p><b>Жанр:</b> {genre}</p>
                <p>Ця гра додана як приклад для практичного завдання. Тут можна написати коротку історію гри, опис світу, персонажів або цікаві факти.</p>
                <p>HTML-файл лежить у папці Assets, а в базі даних зберігається тільки шлях до нього.</p>
            </div>
        </body>
        </html>
        """);
        return rel;
    }

    private static string MakeFileName(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(" ", "_").ToLower();
    }

    public static DataTable LoadTable(int categoryId = 0)
    {
        using var con = new SqliteConnection(ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
        """
        SELECT i.item_id, i.title, i.release_year, i.genre, i.cover_image_path,
               i.media_path, i.html_desc_path, c.category_name, i.category_id
        FROM items i
        JOIN categories c ON c.category_id = i.category_id
        WHERE ($cat = 0 OR i.category_id = $cat)
        ORDER BY i.title
        """;
        cmd.Parameters.AddWithValue("$cat", categoryId);

        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public static List<GameItem> LoadItems()
    {
        var table = LoadTable();
        var list = new List<GameItem>();
        foreach (DataRow r in table.Rows) list.Add(RowToItem(r));
        return list;
    }

    public static GameItem RowToItem(DataRow r)
    {
        return new GameItem
        {
            Id = Convert.ToInt32(r["item_id"]),
            Title = r["title"].ToString() ?? "",
            ReleaseYear = Convert.ToInt32(r["release_year"]),
            Genre = r["genre"].ToString() ?? "",
            CoverPath = r["cover_image_path"].ToString() ?? "",
            MediaPath = r["media_path"].ToString() ?? "",
            HtmlPath = r["html_desc_path"].ToString() ?? "",
            CategoryId = Convert.ToInt32(r["category_id"]),
            CategoryName = r["category_name"].ToString() ?? ""
        };
    }

    public static DataTable LoadCategories()
    {
        using var con = new SqliteConnection(ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT category_id, category_name FROM categories ORDER BY category_name";
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public static void SaveItem(GameItem item)
    {
        using var con = new SqliteConnection(ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        if (item.Id == 0)
        {
            cmd.CommandText =
            """
            INSERT INTO items(title, release_year, genre, cover_image_path, media_path, html_desc_path, category_id)
            VALUES($title, $year, $genre, $cover, $media, $html, $cat)
            """;
        }
        else
        {
            cmd.CommandText =
            """
            UPDATE items SET title=$title, release_year=$year, genre=$genre,
            cover_image_path=$cover, media_path=$media, html_desc_path=$html, category_id=$cat
            WHERE item_id=$id
            """;
            cmd.Parameters.AddWithValue("$id", item.Id);
        }

        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$year", item.ReleaseYear);
        cmd.Parameters.AddWithValue("$genre", item.Genre);
        cmd.Parameters.AddWithValue("$cover", item.CoverPath);
        cmd.Parameters.AddWithValue("$media", item.MediaPath);
        cmd.Parameters.AddWithValue("$html", item.HtmlPath);
        cmd.Parameters.AddWithValue("$cat", item.CategoryId);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteItem(int id)
    {
        using var con = new SqliteConnection(ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM items WHERE item_id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public static string FullPath(string rel)
    {
        if (string.IsNullOrWhiteSpace(rel)) return "";
        if (Path.IsPathRooted(rel)) return rel;
        return Path.Combine(BaseFolder, rel);
    }

    public static string CopyToAssets(string file, string folder)
    {
        if (string.IsNullOrWhiteSpace(file)) return "";
        Directory.CreateDirectory(Path.Combine(AssetsFolder, folder));
        string ext = Path.GetExtension(file);
        string name = Guid.NewGuid() + ext;
        string rel = Path.Combine("Assets", folder, name);
        File.Copy(file, Path.Combine(BaseFolder, rel), true);
        return rel;
    }
}
