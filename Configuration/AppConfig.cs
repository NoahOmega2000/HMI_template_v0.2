using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace HMI_template_v0._2.Configuration
{
    // Classe statica per un facile accesso alla configurazione da qualsiasi punto del programma
    public static class AppConfig
    {
        public static IConfiguration Configuration { get; }
        public static OpcUaSettings OpcUa { get; }
        public static List<OpcTagConfig> OpcTags { get; }

        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            OpcUa = new OpcUaSettings();
            Configuration.GetSection("OpcUaSettings").Bind(OpcUa);

            OpcTags = new List<OpcTagConfig>();
            Configuration.GetSection("OpcTags").Bind(OpcTags);
        }
    }

    // Classi che mappano le sezioni del file JSON
    public class OpcUaSettings
    {
        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string NodeIdPrefix { get; set; }
    }

    public class OpcTagConfig
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Access { get; set; } // "ReadWrite" o "ReadOnly"
    }
}