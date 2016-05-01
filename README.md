# Image Optimization for EPiServer

![](http://tc.geta.no/app/rest/builds/buildType:(id:TeamFrederik_ImageOptimization_ImageOptimizationDebug)/statusIcon)

Scheduled job that will use Yahoo! SmushIt to optimize all images in EPiServer's assets catalog without any change to look or visual quality. Smart enough to only optimize new images that have not been optimized before.

## Getting started

### Install the NuGet package

    Install-Package Geta.ImageOptimization

### Configuration

    <section name="geta.imageoptimization" type="Geta.ImageOptimization.Configuration.ImageOptimizationConfigurationSection, Geta.ImageOptimization" />
    <geta.imageoptimization>
      <settings bypassPreviouslyOptimized="false" siteUrl="[public url here]" />
    </geta.imageoptimization>

The bypassPreviouslyOptimized attribute (default is false) will tell the scheduled job to optimize all images, regardless if they have been optimized before or not.

The siteUrl attribute is used for a public domain name that we use when building the external public URL of the image, for SmushIt.
