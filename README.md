# SyncSaberService
Automatically downloads Beat Saber maps

Based on SyncSaber by brian, https://github.com/brian91292/SyncSaber/.

This is currently a standalone application you can run to automatically download maps like the original SyncSaber did.

# Configuration
<p>The app's settings are in the SyncSaberService.ini file in the same folder as the executable</p>
<p>You must add your BeastSaber username if you want to download your bookmarks and following feeds.</p>
<p>You can either manually enter your Beat Saber game's folder or drag and drop the folder onto SyncSaberService.exe to provide your game directory.</p>
<p>SyncSaberService uses the same FavoriteMappers.ini in Beat Saber's UserData folder as the original did. Format is a single mapper's name on each line.</p>

# Frequenty Asked Question(s)
<p><b>Why is SyncSaberService skipping songs and not downloading them?</b> When songs are skipped, it's either because SyncSaberService found the songs in your CustomSongs folder or the songs were listed in the SyncSaberHistory.txt located in your Beat Saber UserData folder. The history exists so that SyncSaberService doesn't redownload a song you've previously deleted.</p>
