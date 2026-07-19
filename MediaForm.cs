using AxWMPLib;
using Microsoft.Web.WebView2.WinForms;

namespace GameEncyclopedia;

public class MediaForm : Form
{
    private GameItem item;
    private AxWindowsMediaPlayer player = new();
    private WebView2 web = new();

    public MediaForm(GameItem game)
    {
        item = game;
        Text = "Детальний перегляд: " + item.Title;
        Width = 980;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        ((System.ComponentModel.ISupportInitialize)player).BeginInit();
        player.Dock = DockStyle.Top;
        player.Height = 260;
        Controls.Add(player);
        ((System.ComponentModel.ISupportInitialize)player).EndInit();

        web.Dock = DockStyle.Fill;
        Controls.Add(web);

        Load += MediaForm_Load;
        FormClosing += (_, _) => player.close();
    }

    private async void MediaForm_Load(object? sender, EventArgs e)
    {
        string media = Database.FullPath(item.MediaPath);
        if (File.Exists(media))
        {
            player.Visible = true;
            player.Dock = DockStyle.Top;
            player.URL = media;
        }
        else
        {
            player.Visible = false;
            web.Dock = DockStyle.Fill;
        }

        await web.EnsureCoreWebView2Async();
        string html = Database.FullPath(item.HtmlPath);
        if (File.Exists(html)) web.Source = new Uri(html);
        else web.NavigateToString("<h1>Опис не знайдено</h1>");
    }
}
