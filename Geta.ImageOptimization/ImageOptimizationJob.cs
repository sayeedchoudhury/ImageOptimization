using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Geta.ImageOptimization.Configuration;
using Geta.ImageOptimization.Helpers;
using Geta.ImageOptimization.Interfaces;
using Geta.ImageOptimization.Messaging;
using Geta.ImageOptimization.Models;

namespace Geta.ImageOptimization
{
    [ScheduledPlugIn(DisplayName = "Geta Image Optimization")]
    public class ImageOptimizationJob : ScheduledJobBase
    {
        private bool _stop;
        private readonly IImageOptimization _imageOptimization;
        private readonly IImageLogRepository _imageLogRepository;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ImageOptimizationJob));

        public ImageOptimizationJob() : this(ServiceLocator.Current.GetInstance<IImageOptimization>(), ServiceLocator.Current.GetInstance<IImageLogRepository>())
        {
            IsStoppable = true;
        }

        public ImageOptimizationJob(IImageOptimization imageOptimization, IImageLogRepository imageLogRepository)
        {
            this._imageOptimization = imageOptimization;
            this._imageLogRepository = imageLogRepository;
        }

        public override string Execute()
        {
            int count = 0;
            long totalBytesBefore = 0;
            long totalBytesAfter = 0;

            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var blobFactory = ServiceLocator.Current.GetInstance<BlobFactory>();

            IEnumerable<ImageData> allImages = GetFolders(contentRepository).SelectMany(GetImageFiles).Distinct();

            if (_stop)
            {
                return $"Job stopped after optimizing {count} images.";
            }

            if (!ImageOptimizationSettings.Instance.BypassPreviouslyOptimized)
            {
                allImages = FilterPreviouslyOptimizedImages(allImages);
            }

            foreach (ImageData image in allImages)
            {
                if (_stop)
                {
                    return
                        $"Job completed after optimizing: {count} images. Before: {totalBytesBefore / 1024} KB, after: {totalBytesAfter / 1024} KB.";
                }

                if (!PublishedStateAssessor.IsPublished(image) || image.IsDeleted)
                {
                    continue;
                }

                var imageOptimizationRequest = new ImageOptimizationRequest
                {
                    ImageUrl = image.ContentLink.GetFriendlyUrl()
                };

                ImageOptimizationResponse imageOptimizationResponse = this._imageOptimization.ProcessImage(imageOptimizationRequest);
                
                Identity logEntryId = this.AddLogEntry(imageOptimizationResponse, image);

                if (imageOptimizationResponse.Successful)
                {
                    totalBytesBefore += imageOptimizationResponse.OriginalImageSize;

                    if (imageOptimizationResponse.OptimizedImageSize > 0)
                    {
                        totalBytesAfter += imageOptimizationResponse.OptimizedImageSize;
                    }
                    else
                    {
                        totalBytesAfter += imageOptimizationResponse.OriginalImageSize;
                    }

                    if (image.CreateWritableClone() is ImageData file)
                    {
                        byte[] fileContent = imageOptimizationResponse.OptimizedImage;

                        var blob = blobFactory.CreateBlob(file.BinaryDataContainer, MimeTypeHelper.GetDefaultExtension(file.MimeType));

                        blob.Write(new MemoryStream(fileContent));

                        file.BinaryData = blob;

                        contentRepository.Save(file, SaveAction.Publish, AccessLevel.NoAccess);

                        this.UpdateLogEntryToOptimized(logEntryId);

                        count++;
                    }
                }
                else
                {
                    _logger.Error("ErrorMessage from SmushItProxy: " + imageOptimizationResponse.ErrorMessage);
                }
            }

            return
                $"Job completed after optimizing: {count} images. Before: {totalBytesBefore / 1024} KB, after: {totalBytesAfter / 1024} KB.";
        }

        private static List<ContentFolder> GetFolders(IContentRepository contentRepository)
        {
            var folders = new List<ContentFolder>
            {
                contentRepository.Get<ContentFolder>(SiteDefinition.Current.GlobalAssetsRoot)
            };

            if (ImageOptimizationSettings.Instance.IncludeContentAssets)
            {
                folders.Add(contentRepository.Get<ContentFolder>(SiteDefinition.Current.ContentAssetsRoot));
            }
            return folders;
        }

        private IEnumerable<ImageData> FilterPreviouslyOptimizedImages(IEnumerable<ImageData> allImages)
        {
            return allImages.Where(imageData => this._imageLogRepository.GetLogEntry(imageData.ContentGuid) == null);
        }

        private IEnumerable<ImageData> GetImageFiles(ContentFolder contentFolder)
        {
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            var queue = new Queue<ContentFolder>();
            queue.Enqueue(contentFolder);
            while (queue.Count > 0)
            {
                contentFolder = queue.Dequeue();
                try
                {
                    foreach (ContentFolder subDir in contentLoader.GetChildren<ContentFolder>(contentFolder.ContentLink))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message);
                }
                IEnumerable<ImageData> files = null;
                try
                {
                    files = contentLoader.GetChildren<ImageData>(contentFolder.ContentLink);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message);
                }
                if (files != null)
                {
                    foreach (var imageData in files)
                    {
                        yield return imageData;
                    }
                }
            }
        }

        private void UpdateLogEntryToOptimized(Identity logEntryId)
        {
            ImageLogEntry logEntry = this._imageLogRepository.GetLogEntry(logEntryId);

            logEntry.IsOptimized = true;

            this._imageLogRepository.Save(logEntry);
        }

        private Identity AddLogEntry(ImageOptimizationResponse imageOptimizationResponse, ImageData imageData)
        {
            ImageLogEntry logEntry = this._imageLogRepository.GetLogEntry(imageOptimizationResponse.OriginalImageUrl) ?? new ImageLogEntry();

            logEntry.ContentGuid = imageData.ContentGuid;
            logEntry.OriginalSize = imageOptimizationResponse.OriginalImageSize;
            logEntry.OptimizedSize = imageOptimizationResponse.OptimizedImageSize;
            logEntry.PercentSaved = imageOptimizationResponse.PercentSaved;
            logEntry.ImageUrl = imageOptimizationResponse.OriginalImageUrl;

            return this._imageLogRepository.Save(logEntry);
        }

        public override void Stop()
        {
            _stop = true;
        }
    }
}
