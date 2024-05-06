namespace DevelopersHub.RealtimeNetworking.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Sockets;
    using System.Net;
    using System.Net.NetworkInformation; 
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    public static class Tools
    {
        public static void LogError(string message, string trace, string folder = "")
        {
            Console.WriteLine("Error:" + "\n" + message + "\n" + trace);
            Task task = Task.Run(() =>
            {
                try
                {
                    string folderPath = @"./Logs/";
                    if (!string.IsNullOrEmpty(folder))
                    {
                        folderPath = folderPath + folder + "\\";
                    }
                    string path = folderPath + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss-ffff") + ".txt";
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    File.WriteAllText(path, message + "\n" + trace);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + "\n" + ex.Message + "\n" + ex.StackTrace);
                }
            });
        }
        
        public static string GenerateToken()
        {
            return Path.GetRandomFileName().Remove(8, 1);
        }

        public static string GetIP(AddressFamily type)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == type)
                {
                    return ip.ToString();
                }
            }
            return "0.0.0.0";
        }

        public static int FindFreeTcpPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static T CloneClass<T>(this T target)
        {
            return Deserialize<T>(Serialize<T>(target));
        }

        public static void CopyTo(Stream source, Stream target)
        {
            byte[] bytes = new byte[4096]; int count;
            while ((count = source.Read(bytes, 0, bytes.Length)) != 0)
            {
                target.Write(bytes, 0, count);
            }
        }

        public static List<string> FindCurrentIPs()
        {
            List<string> ipAddresses = new List<string>();
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipAddresses.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return ipAddresses;
        }

        #region Serialization
        public static string Serialize<T>(this T target)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringWriter writer = new StringWriter();
            xml.Serialize(writer, target);
            return writer.ToString();
        }

        public static T Deserialize<T>(this string target)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(target);
            return (T)xml.Deserialize(reader);
        }

        public async static Task<string> SerializeAsync<T>(this T target)
        {
            Task<string> task = Task.Run(() =>
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringWriter writer = new StringWriter();
                xml.Serialize(writer, target);
                return writer.ToString();
            });
            return await task;
        }

        public async static Task<T> DeserializeAsync<T>(this string target)
        {
            Task<T> task = Task.Run(() =>
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringReader reader = new StringReader(target);
                return (T)xml.Deserialize(reader);
            });
            return await task;
        }
        #endregion

        #region Compression
        public async static Task<byte[]> CompressAsync(string target)
        {
            Task<byte[]> task = Task.Run(() =>
            {
                return Compress(target);
            });
            return await task;
        }

        public static byte[] Compress(string target)
        {
            var bytes = Encoding.UTF8.GetBytes(target);
            using (var msi = new MemoryStream(bytes))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        CopyTo(msi, gs);
                    }
                    return mso.ToArray();
                }
            }
        }

        public async static Task<string> CompressStringAsync(string target)
        {
            Task<string> task = Task.Run(() =>
            {
                return CompressString(target);
            });
            return await task;
        }

        public static string CompressString(string target)
        {
            return Convert.ToBase64String(Compress(target));
        }

        public async static Task<string> DecompressAsync(byte[] bytes)
        {
            Task<string> task = Task.Run(() =>
            {
                return Decompress(bytes);
            });
            return await task;
        }

        public static string Decompress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    {
                        CopyTo(gs, mso);
                    }
                    return Encoding.UTF8.GetString(mso.ToArray());
                }
            }
        }

        public async static Task<string> DecompressStringAsync(string target)
        {
            Task<string> task = Task.Run(() =>
            {
                return DecompressString(target);
            });
            return await task;
        }

        public static string DecompressString(string target)
        {
            return Decompress(Convert.FromBase64String(target));
        }
        #endregion

    }
}
