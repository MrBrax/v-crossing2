using vcrossing.Code.Data;

namespace vcrossing.Code.Items;

public partial class Wallpaper : WorldItem
{

	[Export( PropertyHint.File, "*.tres" )]
	public string WallpaperDataPath;

	public WallpaperData WallpaperData => Loader.LoadResource<WallpaperData>( WallpaperDataPath );

}
