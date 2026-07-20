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
        string[] cats = ["RPG", "Екшен", "Стратегія", "Пригоди", "Шутер", "Симулятор", "Хорор", "Виживання"];
        foreach (var cat in cats)
        {
            var check = con.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM categories WHERE category_name=$name";
            check.Parameters.AddWithValue("$name", cat);
            if (Convert.ToInt32(check.ExecuteScalar()) > 0) continue;

            var c = con.CreateCommand();
            c.CommandText = "INSERT INTO categories(category_name) VALUES($name)";
            c.Parameters.AddWithValue("$name", cat);
            c.ExecuteNonQuery();
        }

        var games = new (string title, int year, string genre, string cat)[]
        {
            ("The Witcher 3", 2015, "RPG", "RPG"),
            ("Minecraft", 2011, "Пісочниця", "Пригоди"),
            ("Portal 2", 2011, "Головоломка", "Пригоди"),
            ("Half-Life 2", 2004, "Шутер", "Шутер"),
            ("Stardew Valley", 2016, "Симулятор", "Симулятор"),
            ("Civilization VI", 2016, "Стратегія", "Стратегія"),
            ("Hades", 2020, "Roguelike", "Екшен"),
            ("Elden Ring", 2022, "Action RPG", "RPG"),
            ("Terraria", 2011, "Пригоди", "Пригоди"),
            ("Cyberpunk 2077", 2020, "RPG", "RPG"),
            ("Subnautica", 2018, "Виживання", "Виживання"),
            ("Subnautica Below Zero", 2021, "Виживання", "Виживання"),
            ("Counter-Strike 2", 2023, "Тактичний шутер", "Шутер"),
            ("Battlefield 2042", 2021, "Шутер", "Шутер"),
            ("Fortnite", 2017, "Battle Royale", "Шутер"),
            ("GTA V", 2013, "Екшен", "Екшен"),
            ("Red Dead Redemption 2", 2018, "Пригоди", "Пригоди"),
            ("God of War", 2018, "Екшен", "Екшен"),
            ("The Last of Us Part I", 2013, "Пригоди", "Пригоди"),
            ("Doom Eternal", 2020, "Шутер", "Шутер"),
            ("Resident Evil 4", 2005, "Хорор", "Хорор"),
            ("Hogwarts Legacy", 2023, "RPG", "RPG"),
            ("Baldur's Gate 3", 2023, "RPG", "RPG"),
            ("Roblox", 2006, "Платформа ігор", "Пригоди"),
            ("Among Us", 2018, "Соціальна гра", "Пригоди"),
            ("Valorant", 2020, "Тактичний шутер", "Шутер"),
            ("Apex Legends", 2019, "Battle Royale", "Шутер"),
            ("League of Legends", 2009, "MOBA", "Стратегія"),
            ("Dota 2", 2013, "MOBA", "Стратегія"),
            ("Rocket League", 2015, "Спорт", "Екшен"),
            ("Helldivers 2", 2024, "Кооперативний шутер", "Шутер"),
            ("Palworld", 2024, "Виживання", "Виживання"),
            ("Black Myth Wukong", 2024, "Action RPG", "RPG"),
            ("Lethal Company", 2023, "Хорор", "Хорор"),
            ("Phasmophobia", 2020, "Хорор", "Хорор"),
            ("Sea of Thieves", 2020, "Пригоди", "Пригоди"),
            ("No Man's Sky", 2016, "Виживання", "Виживання"),
            ("Euro Truck Simulator 2", 2012, "Симулятор", "Симулятор"),
            ("War Thunder", 2013, "Екшен", "Екшен"),
            ("Rust", 2018, "Виживання", "Виживання")
        };

        int i = 1;
        foreach (var g in games)
        {
            var exists = con.CreateCommand();
            exists.CommandText = "SELECT COUNT(*) FROM items WHERE title=$title";
            exists.Parameters.AddWithValue("$title", g.title);
            if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
            {
                i++;
                continue;
            }

            var cover = MakeCover(g.title, i);
            var html = MakeHtml(g.title, g.year, g.genre);
            int catId = GetCategoryId(con, g.cat);

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
            cmd.Parameters.AddWithValue("$cat", catId);
            cmd.ExecuteNonQuery();
            i++;
        }
    }

    private static int GetCategoryId(SqliteConnection con, string name)
    {
        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT category_id FROM categories WHERE category_name=$name";
        cmd.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static string MakeCover(string title, int number)
    {
        string rel = Path.Combine("Assets", "covers", $"cover_{number}_{MakeFileName(title)}.png");
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
        $$"""
        <!doctype html>
        <html lang="uk">
        <head>
            <meta charset="utf-8">
            <title>{{title}}</title>
        </head>
        <body>
            <h1>{{title}}</h1>
            <p><b>Рік виходу:</b> {{year}}</p>
            <p><b>Жанр:</b> {{genre}}</p>
            <p>Тут можна написати коротку історію гри, опис світу, персонажів або цікаві факти.</p>
            <p>HTML-файл лежить у папці Assets, а в базі даних зберігається тільки шлях до нього.</p>
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
