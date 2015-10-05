﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  // This is a work around -> wait for MIA rework
  // Add more details
  class GetTVShowsBasic : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      // we can't select only for shows, so we take all episodes and filter the shows.
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Tv Episodes found");

      var output = new List<WebTVShowBasic>();

      foreach (var item in items)
      {
        var seriesAspect = item.Aspects[SeriesAspect.ASPECT_ID];
        int index = output.FindIndex(x => x.Title == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
        if (index == -1)
        {
          var episodesInThisShow = items.ToList().FindAll(x => (string)x.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_SERIESNAME] == (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME]);
          var episodesInThisShowUnwatched = episodesInThisShow.FindAll(x => x.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] == null || (int)x.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_PLAYCOUNT] == 0);
          necessaryMIATypes = new HashSet<Guid>();
          necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
          MediaItem show = GetMediaItems.GetMediaItemByName((string)seriesAspect[SeriesAspect.ATTR_SERIESNAME], necessaryMIATypes);

          WebTVShowBasic webTVShowBasic = new WebTVShowBasic();
          webTVShowBasic.Id = show.MediaItemId.ToString();
          webTVShowBasic.Title = (string)seriesAspect[SeriesAspect.ATTR_SERIESNAME];
          webTVShowBasic.EpisodeCount = episodesInThisShow.Count;
          webTVShowBasic.UnwatchedEpisodeCount = episodesInThisShowUnwatched.Count;

          output.Add(webTVShowBasic);
        }
      }

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      string filter = httpParam["filter"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.Filter(filter).SortWebTVShowBasic(webSortField, webSortOrder).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
