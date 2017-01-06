using CookComputing.XmlRpc;

namespace SubtitleDownloaderPlugin.Engine.Podnapisi
{
    [XmlRpcUrl("http://ssp.podnapisi.net:8000/RPC2/")]
    public interface IPodnapisi : IXmlRpcProxy
    {
        [XmlRpcMethod("initiate")]
        PodnapisiSession Initiate(string useragent);

        [XmlRpcMethod("search")]
        object Search(string session, string[] hashes);
    }
}
