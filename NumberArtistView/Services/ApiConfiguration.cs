namespace NumberArtistView.Services
{
    public static class ApiConfiguration
    {
        // ⚠️ IMPORTANT: Your hosts file has "numberartist.officeblox.co.uk" pointing to 127.0.0.1
        // This works on PC but breaks on Android emulator/device!
        // 
        // SOLUTION: Use one of these options:
        // Option 1: Set UseLocalhost = true (uses 10.0.2.2 for Android emulator)
        // Option 2: Set UseFallbackIP = true and update ServerIPAddress to "10.0.2.2"
        
        // Set this to true for local development, false for production
        public const bool UseLocalhost = true;  // ✅ Recommended for development with hosts file
        
        // Set this to true if DNS resolution fails for the domain name
        // This will use the direct IP address instead of the hostname
        public const bool UseFallbackIP = false;  // Not needed if UseLocalhost is true
        
        // Direct IP address as fallback when DNS fails
        // For Android emulator: Use 10.0.2.2 (special IP to reach host PC)
        // For physical device on same WiFi: Use your PC's local IP (e.g., 192.168.1.100)
        public const string ServerIPAddress = "10.0.2.2";
        
        // Local development URLs (for testing on emulator/device)
        public const string LocalhostUrlAndroid = "https://10.0.2.2:5015"; // Android emulator
        public const string LocalhostUrlIOS = "https://localhost:5015"; // iOS simulator
        public const string LocalhostUrlWindows = "https://localhost:5015"; // Windows
        
        // Production URL (domain name)
        // ⚠️ WARNING: This resolves to 127.0.0.1 on your PC due to hosts file!
        public const string ProductionUrl = "https://numberartist.officeblox.co.uk:5015";
        
        // Production URL (IP address fallback)
        public const string ProductionUrlIP = $"https://{ServerIPAddress}:5015";
        
        public static string GetApiUrl()
        {
#if DEBUG
            if (UseLocalhost)
            {
#if ANDROID
                return LocalhostUrlAndroid;
#elif IOS
                return LocalhostUrlIOS;
#else
                return LocalhostUrlWindows;
#endif
            }
#endif
            // Use IP address fallback if DNS resolution is failing
            if (UseFallbackIP)
            {
                return ProductionUrlIP;
            }
            
            return ProductionUrl;
        }
        
        // Get the hostname for SNI (Server Name Indication) when using IP address
        public static string GetHostname()
        {
            return "numberartist.officeblox.co.uk";
        }
    }
}
