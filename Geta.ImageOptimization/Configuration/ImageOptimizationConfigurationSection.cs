using System.Configuration;
using System.Web.Configuration;

namespace Geta.ImageOptimization.Configuration
{
    public class ImageOptimizationConfigurationSection : ConfigurationSection
    {
        private static ImageOptimizationConfigurationSection _instance;
        private static readonly object Lock = new object();

        public static ImageOptimizationConfigurationSection Instance
        {
            get
            {
                lock (Lock)
                {
                    return _instance ?? (_instance = GetSection());
                }
            }
        }

        [ConfigurationProperty("settings", IsRequired = true)]
        public ImageOptimizationSettings Settings => (ImageOptimizationSettings)base["settings"];

        protected static ImageOptimizationConfigurationSection GetSection()
        {
            if (!(WebConfigurationManager.GetSection("geta.imageoptimization") is ImageOptimizationConfigurationSection section))
            {
                throw new ConfigurationErrorsException("The <geta.imageoptimization> configuration section could not be found in web.config.");
            }

            return section;
        }
    }
}