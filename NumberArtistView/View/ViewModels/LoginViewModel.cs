using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace NumberArtist.View.ViewModels
{
    public partial class LoginViewModel : INotifyPropertyChanged
    {
        string ipAddress;
        string username;
        string password;

       // public event PropertyChangedEventHandler PropertyChanged;

      


  
        public string IpAddress { get => ipAddress; private set => ipAddress = value; }



        //async Task ResolveIpFromUrlAsync(string urlOrHost)
        //{
        //    try
        //    {
        //        string host = urlOrHost;
        //        if (Uri.TryCreate(urlOrHost, UriKind.Absolute, out var uri))
        //            host = uri.Host;

        //        var addresses = await Dns.GetHostAddressesAsync(host);

        //        // prefer IPv4
        //        var ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
        //                 ?? addresses.FirstOrDefault();

        //        IpAddress = ip?.ToString() ?? "Not found";
        //    }
        //    catch (Exception ex)
        //    {
        //        IpAddress = $"Error: {ex.Message}";
        //    }
        //}

        //async Task ResolveIpFromUrlAsync(string urlOrHost)
        //{
        //    try
        //    {
        //        string host = urlOrHost;
        //        if (Uri.TryCreate(urlOrHost, UriKind.Absolute, out var uri))
        //            host = uri.Host;

        //        var addresses = await Dns.GetHostAddressesAsync(host);
        //        var ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();
        //        if (ip != null)
        //        {
        //            IpAddress = ip.ToString();
        //            if (Uri.TryCreate($"http://{IpAddress}", UriKind.Absolute, out var baseUri))
        //            {
        //                _httpClient.BaseAddress = baseUri;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        IpAddress = $"Error: {ex.Message}";
        //    }
        //}

        //void OnPropertyChanged(string name) =>
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}