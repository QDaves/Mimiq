using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.IO;
using System.Net.Http;
using Mimiq.Core;
using Path = Avalonia.Controls.Shapes.Path;

namespace Mimiq;

public partial class MainWindow : Window
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static MainWindow()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    private readonly GEarthExtension? extension;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(GEarthExtension extension) : this()
    {
        this.extension = extension;
        InitializeControls();
    }

    private void InitializeControls()
    {
        var pinButton = this.FindControl<Button>("pinbtn");
        var pinIcon = this.FindControl<Path>("pinicon");

        if (pinButton != null && pinIcon != null)
        {
            pinButton.Click += (s, e) =>
            {
                Topmost = !Topmost;
                pinIcon.Fill = Topmost ? Brushes.White : Brushes.Gray;
            };
        }

        if (extension?.Manager == null)
            return;

        var manager = extension.Manager;

        var controls = new
        {
            Figure = this.FindControl<CheckBox>("figchk"),
            Motto = this.FindControl<CheckBox>("mottochk"),
            Action = this.FindControl<CheckBox>("actchk"),
            Dance = this.FindControl<CheckBox>("dancechk"),
            Sign = this.FindControl<CheckBox>("signchk"),
            Effect = this.FindControl<CheckBox>("effchk"),
            Sit = this.FindControl<CheckBox>("sitchk"),
            Follow = this.FindControl<CheckBox>("followchk"),
            Typing = this.FindControl<CheckBox>("typechk"),
            Talk = this.FindControl<CheckBox>("talkchk"),
            Shout = this.FindControl<CheckBox>("shoutchk"),
            Whisper = this.FindControl<CheckBox>("whisperchk"),
            Button = this.FindControl<Button>("mainbtn"),
            AvatarImage = this.FindControl<Image>("avatarimg"),
            NoAvatar = this.FindControl<TextBlock>("noavatar")
        };

        if (controls.Figure != null) controls.Figure.IsChecked = manager.Figure;
        if (controls.Motto != null) controls.Motto.IsChecked = manager.Motto;
        if (controls.Action != null) controls.Action.IsChecked = manager.Action;
        if (controls.Dance != null) controls.Dance.IsChecked = manager.Dance;
        if (controls.Sign != null) controls.Sign.IsChecked = manager.Sign;
        if (controls.Effect != null) controls.Effect.IsChecked = manager.Effect;
        if (controls.Sit != null) controls.Sit.IsChecked = manager.Sit;
        if (controls.Follow != null) controls.Follow.IsChecked = manager.Follow;
        if (controls.Typing != null) controls.Typing.IsChecked = manager.Typing;
        if (controls.Talk != null) controls.Talk.IsChecked = manager.Talk;
        if (controls.Shout != null) controls.Shout.IsChecked = manager.Shout;
        if (controls.Whisper != null) controls.Whisper.IsChecked = manager.Whisper;

        manager.PropertyChanged += (s, e) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (e.PropertyName == nameof(manager.ButtonText) && controls.Button != null)
                    controls.Button.Content = manager.ButtonText;

                if (e.PropertyName == nameof(manager.TargetAvatarUrl))
                {
                    if (!string.IsNullOrEmpty(manager.TargetAvatarUrl))
                        LoadAvatar(manager.TargetAvatarUrl, controls.AvatarImage, controls.NoAvatar);
                    else
                    {
                        if (controls.AvatarImage != null) controls.AvatarImage.IsVisible = false;
                        if (controls.NoAvatar != null) controls.NoAvatar.IsVisible = true;
                    }
                }
            });
        };

        if (controls.Figure != null)
            controls.Figure.IsCheckedChanged += (s, e) => manager.Figure = controls.Figure.IsChecked ?? false;
        if (controls.Motto != null)
            controls.Motto.IsCheckedChanged += (s, e) => manager.Motto = controls.Motto.IsChecked ?? false;
        if (controls.Action != null)
            controls.Action.IsCheckedChanged += (s, e) => manager.Action = controls.Action.IsChecked ?? false;
        if (controls.Dance != null)
            controls.Dance.IsCheckedChanged += (s, e) => manager.Dance = controls.Dance.IsChecked ?? false;
        if (controls.Sign != null)
            controls.Sign.IsCheckedChanged += (s, e) => manager.Sign = controls.Sign.IsChecked ?? false;
        if (controls.Effect != null)
            controls.Effect.IsCheckedChanged += (s, e) => manager.Effect = controls.Effect.IsChecked ?? false;
        if (controls.Sit != null)
            controls.Sit.IsCheckedChanged += (s, e) => manager.Sit = controls.Sit.IsChecked ?? false;
        if (controls.Follow != null)
            controls.Follow.IsCheckedChanged += (s, e) => manager.Follow = controls.Follow.IsChecked ?? false;
        if (controls.Typing != null)
            controls.Typing.IsCheckedChanged += (s, e) => manager.Typing = controls.Typing.IsChecked ?? false;
        if (controls.Talk != null)
            controls.Talk.IsCheckedChanged += (s, e) => manager.Talk = controls.Talk.IsChecked ?? false;
        if (controls.Shout != null)
            controls.Shout.IsCheckedChanged += (s, e) => manager.Shout = controls.Shout.IsChecked ?? false;
        if (controls.Whisper != null)
            controls.Whisper.IsCheckedChanged += (s, e) => manager.Whisper = controls.Whisper.IsChecked ?? false;
        if (controls.Button != null)
            controls.Button.Click += (s, e) => manager.Toggle();
    }

    private async void LoadAvatar(string url, Image? image, TextBlock? placeholder)
    {
        if (image == null || placeholder == null)
            return;

        try
        {
            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);

            Dispatcher.UIThread.Post(() =>
            {
                image.Source = bitmap;
                image.IsVisible = true;
                placeholder.IsVisible = false;
            });
        }
        catch
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (placeholder != null)
                {
                    placeholder.Text = "Error";
                    placeholder.IsVisible = true;
                }
            });
        }
    }
}
