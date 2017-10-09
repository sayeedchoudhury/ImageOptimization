using System.Configuration;

namespace Geta.ImageOptimization.Configuration
{
    public class ImageOptimizationSettings : ConfigurationElement
    {
        private static ImageOptimizationSettings _instance;
        private static readonly object Lock = new object();

        public static ImageOptimizationSettings Instance
        {
            get
            {
                lock (Lock)
                {
                    return _instance ?? (_instance = ImageOptimizationConfigurationSection.Instance.Settings);
                }
            }
        }

        /// <summary>
        /// Url prefix used for the images (needs to be public)
        /// </summary>
        [ConfigurationProperty("siteUrl")]
        public string SiteUrl
        {
            get => base["siteUrl"] as string;
            set => base["siteUrl"] = value;
        }
        
        [ConfigurationProperty("imageOptimizationApi")]
        public string ImageOptimizationApi
        {
            get => base["imageOptimizationApi"] as string;
            set => base["imageOptimizationApi"] = value;
        }

        [ConfigurationProperty("bypassPreviouslyOptimized")]
        public bool BypassPreviouslyOptimized
        {
            get
            {
                if (base["bypassPreviouslyOptimized"] == null)
                {
                    return false;
                }

                return (bool)base["bypassPreviouslyOptimized"];
            }
            set => base["bypassPreviouslyOptimized"] = value;
        }

        [ConfigurationProperty("includeContentAssets")]
        public bool IncludeContentAssets
        {
            get
            {
                if (base["includeContentAssets"] == null)
                {
                    return false;
                }

                return (bool)base["includeContentAssets"];
            }
            set => base["includeContentAssets"] = value;
        }
    }
}