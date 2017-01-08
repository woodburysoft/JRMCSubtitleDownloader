# JRMCSubtitleDownloader #

<a href="http://pahunt.no-ip.org:8081/viewType.html?buildTypeId=buildType:JRMCSubtitleDownloader_BuildPlugIn&guest=1">
	<img src="http://pahunt.no-ip.org:8081/app/rest/builds/buildType:(id:buildType:JRMCSubtitleDownloader_BuildPlugIn)/statusIcon"/>
</a>

This is a plug-in for JRiver Media Center will attempt to download subtitles for a movie or a TV show as soon as playback begins. I wrote this purely for myself to begin with primarily because I got bored of manually downloading subtitles, naming the file correctly and putting it in the correct folder but I realised other people might find this useful too. Here are some important disclaimers:

1. You need to have Microsoft .NET Framework version 4 installed for this plug-in to work.
2. It will attempt to download subtitles for the following types of video file:

   Movies: Any video file with a Media Sub Type of "Movie" that has a value in the IMDB ID field.
   TV Shows: Any video file with a Media Sub Type of "TV Show" that has values in the Series, Season and Episode fields.

3. It will not attempt to download subtitles for DVD or Blu-ray discs that have been imported.
4. It will not attempt to download subtitles for any files that have not been imported.
5. In the plug-in options you can select from a list of languages.
