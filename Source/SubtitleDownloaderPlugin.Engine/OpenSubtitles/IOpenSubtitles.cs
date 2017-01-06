﻿// <auto-generated/>

using CookComputing.XmlRpc;

namespace SubtitleDownloaderPlugin.Engine.OpenSubtitles
{   
    [XmlRpcUrl("http://api.opensubtitles.org/xml-rpc")]
    public interface IOpenSubtitles : IXmlRpcProxy
    {        
        [XmlRpcMethod("LogIn")]
        OpenSubtitlesToken LogIn(string userName, string password, string language, string userAgentId);

        [XmlRpcMethod("LogOut")]
        OpenSubtitlesLogOutResult LogOut(string token);

        [XmlRpcMethod("SearchSubtitles")]
        OpenSubtitlesRecordHeader SearchSubtitles(string token, OpenSubtitlesHashSearchParameters[] search);

        [XmlRpcMethod("SearchSubtitles")]
        OpenSubtitlesRecordHeader SearchSubtitles(string token, OpenSubtitlesSeriesSeasonEpisodeSearchParameters[] search);

        [XmlRpcMethod("SearchSubtitles")]
        OpenSubtitlesRecordHeader SearchSubtitles(string token, OpenSubtitlesImdbParameters[] search);

        [XmlRpcMethod("DownloadSubtitles")]
        OpenSubtitlesSubtitleHeader DownloadSubtitles(string token, object[] IDSubtitleFile);

        [XmlRpcMethod("GetSubLanguages")]
        OpenSubtitlesLanguageHeader GetSubLanguages();
    }    
}