﻿using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP;
using MediaPortal.Plugins.SlimTv.Providers.Settings;
using MediaPortal.UI.Presentation.UiNotifications;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Plugins.SlimTv.Providers.UPnP
{
  public class NativeTvProxy : UPnPServiceProxyBase, ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected readonly IChannel[] _channels = new IChannel[2];
    protected readonly object _syncObj = new object();

    #endregion

    public NativeTvProxy(CpService serviceStub)
      : base(serviceStub, "NativeTv")
    {
      ServiceRegistration.Set<ITvProvider>(this);
    }

    public void Dispose()
    {
      ServiceRegistration.Remove<ITvProvider>();
    }


    public string Name
    {
      get { return "NativeTvProxy"; }
    }

    public bool Init()
    {
      return true;
    }

    public bool DeInit()
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_DEINIT);
        IList<object> inParameters = new List<object>();
        IList<object> outParameters = action.InvokeAction(inParameters);
        return (bool) outParameters[0];
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_START_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex,
              channel.ChannelId
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        
        _channels[slotIndex] = channel;

        // Assign a MediaItem, can be null if streamUrl is the same.
        timeshiftMediaItem = CreateMediaItem(slotIndex, (string) outParameters[0], channel);
        return true;
      }
      catch (Exception ex)
      {
        timeshiftMediaItem = null;
        NotifyException(ex);
        return false;
      }
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        //IProgram currentProgram;
        //if (GetCurrentProgram(channel, out currentProgram))
        //  tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;

        //IProgram nextProgram;
        //if (GetNextProgram(channel, out nextProgram))
        //  tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;

        return tvStream;
      }
      return null;
    }

    public bool StopTimeshift(int slotIndex)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_START_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool) outParameters[0];
        if (success)
        {
          _channels[slotIndex] = null;
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public IChannel GetChannel(int slotIndex)
    {
      return _channels[slotIndex];
    }

    public bool GetCurrentProgram (IChannel channel, out IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool GetNextProgram (IChannel channel, out IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool GetPrograms (IChannel channel, DateTime @from, DateTime to, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetProgramsForSchedule (ISchedule schedule, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetScheduledPrograms (IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel (IProgram program, out IChannel channel)
    {
      throw new NotImplementedException();
    }

    public bool GetChannelGroups (out IList<IChannelGroup> groups)
    {
      throw new NotImplementedException();
    }

    public bool GetChannels (IChannelGroup group, out IList<IChannel> channels)
    {
      throw new NotImplementedException();
    }

    public int SelectedChannelId { get; set; }

    public int SelectedChannelGroupId
    {
      get
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        return settings.LastChannelGroupId;
      }
      set
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        settings.LastChannelGroupId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    public bool CreateSchedule (IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool RemoveSchedule (IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool GetRecordingStatus (IProgram program, out RecordingStatus recordingStatus)
    {
      throw new NotImplementedException();
    }

    #region Exeption handling

    private void NotifyException(Exception ex, string localizationMessage = null)
    {
      string notification = string.IsNullOrEmpty(localizationMessage)
                              ? ex.Message
                              : ServiceRegistration.Get<ILocalization>().ToString(localizationMessage, ex.Message);

      ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, "Error", notification, true);
      ServiceRegistration.Get<ILogger>().Error(notification);
    }

    #endregion
  }
}
