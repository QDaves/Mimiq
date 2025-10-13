using Xabbo;
using Xabbo.GEarth;

namespace Mimiq.Core;

public class GEarthExtension : Xabbo.GEarth.GEarthExtension
{
    public MimiqManager? Manager { get; private set; }
    public string HotelDomain { get; private set; } = "com";

    public GEarthExtension() : base(new GEarthOptions
    {
        Name = "Mimiq",
        Description = "Avatar mimiq tool for Habbo",
        Author = "QDave",
        Version = "1.0.0",
        ShowDeleteButton = true,
        ShowLeaveButton = true
    })
    {
    }

    protected override void OnConnected(ConnectedEventArgs e)
    {
        base.OnConnected(e);

        HotelDomain = e.Host switch
        {
            var host when host.Contains("game-br.") => "com.br",
            var host when host.Contains("game-tr.") => "com.tr",
            var host when host.Contains("game-es.") => "es",
            var host when host.Contains("game-fi.") => "fi",
            var host when host.Contains("game-it.") => "it",
            var host when host.Contains("game-nl.") => "nl",
            var host when host.Contains("game-de.") => "de",
            var host when host.Contains("game-fr.") => "fr",
            _ => "com"
        };

        Manager ??= new MimiqManager(this);
    }
}
