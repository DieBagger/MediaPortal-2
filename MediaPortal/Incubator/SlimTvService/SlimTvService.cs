﻿using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using Mediaportal.TV.Server.TVControl;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVDatabase.Entities.Factories;
using Mediaportal.TV.Server.TVLibrary;
using Mediaportal.TV.Server.TVService.Interfaces;
using Mediaportal.TV.Server.TVService.Interfaces.Enums;
using Mediaportal.TV.Server.TVService.Interfaces.Services;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    const int MAX_WAIT_MS = 2000;
    private TvServiceThread _tvServiceThread;
    protected readonly Dictionary<string, IUser> _tvUsers = new Dictionary<string, IUser>();


    public string Name
    {
      get { return "NativeTv Service"; }
    }

    public bool Init()
    {
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
      _tvServiceThread.Start();
      return true;
    }

    public bool DeInit()
    {
      if (_tvServiceThread != null)
      {
        _tvServiceThread.Stop(MAX_WAIT_MS);
        _tvServiceThread = null;
      }
      return true;
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      // TODO: how to get the calling client name or Guid?
      string timeshiftFile = SwitchTVServerToChannel(GetUserName(slotIndex), channel.ChannelId);
      timeshiftMediaItem = CreateMediaItem(slotIndex, timeshiftFile, channel);
      return true;
    }

    public bool StopTimeshift(int slotIndex)
    {
      IUser user;
      IInternalControllerService control = GlobalServiceProvider.Get<IInternalControllerService>();
      return control.StopTimeShifting(GetUserName(slotIndex), out user);
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        IProgram currentProgram;
        IProgram nextProgram;
        if (GetNowNextProgram(channel, out currentProgram, out nextProgram))
        {
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;
        }
        return tvStream;
      }
      return null;
    }

    public IChannel GetChannel(int slotIndex)
    {
      // We do not manage all client channels here in server, this feature applies only to client side management!
      return null;
    }

    public bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext)
    {
      programNow = null;
      programNext = null;
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      var programs = programService.GetNowAndNextProgramsForChannel(channel.ChannelId);
      var count = programs.Count;
      if (count >= 1)
        programNow = programs[0].ToProgram();
      if (count >= 2)
        programNext = programs[1].ToProgram();
      
      return programNow != null || programNext != null;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      IProgramService programService = GlobalServiceProvider.Get<IProgramService>();
      programs = programService.GetProgramsByChannelAndStartEndTimes(channel.ChannelId, from, to)
        .Select(tvProgram => tvProgram.ToProgram())
        .ToList();
      return programs.Count > 0;
    }

    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
      IChannelService channelService = GlobalServiceProvider.Get<IChannelService>();
      channel = channelService.GetChannel(program.ChannelId).ToChannel();
      return true;
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Get<IChannelGroupService>();
      groups = channelGroupService.ListAllChannelGroups()
        .Select(tvGroup => tvGroup.ToChannelGroup())
        .ToList();
      return true;
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      IChannelGroupService channelGroupService = GlobalServiceProvider.Get<IChannelGroupService>();
      channels = channelGroupService.GetChannelGroup(group.ChannelGroupId).GroupMaps
        .Select(groupMap => groupMap.Channel.ToChannel())
        .ToList();
      return true;
    }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelId { get; set; }

    // This property applies only to client side management and is not used in server!
    public int SelectedChannelGroupId { get; set; }

    public bool CreateSchedule(IProgram program)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      Schedule schedule = ScheduleFactory.CreateSchedule(program.ChannelId, program.Title, program.StartTime, program.EndTime);
      scheduleService.SaveSchedule(schedule);
      return true;
    }

    public bool RemoveSchedule(IProgram program)
    {
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      ICanceledScheduleService canceledScheduleService = GlobalServiceProvider.Get<ICanceledScheduleService>();
      var allSchedules = scheduleService.ListAllSchedules()
        .Where(schedule =>
          schedule.idChannel == program.ChannelId &&
          schedule.startTime == program.StartTime &&
          schedule.endTime == program.EndTime);
      foreach (Schedule schedule in allSchedules)
      {
        switch (schedule.scheduleType)
        {
          case (int) ScheduleRecordingType.Once:
            scheduleService.DeleteSchedule(schedule.id_Schedule);
            break;
          default:
            CanceledSchedule canceledSchedule = CanceledScheduleFactory.CreateCanceledSchedule(schedule.id_Schedule, schedule.idChannel, schedule.startTime);
            canceledScheduleService.SaveCanceledSchedule(canceledSchedule);
            break;
        }
      }
      return true;
    }

    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      throw new NotImplementedException();
    }

    private string SwitchTVServerToChannel(string userName, int channelId)
    {
      if (String.IsNullOrEmpty(userName))
      {
        ServiceRegistration.Get<ILogger>().Error("Called SwitchTVServerToChannel with empty userName");
        throw new ArgumentNullException("userName");
      }

      IUser currentUser = UserFactory.CreateBasicUser(userName, -1);
      ServiceRegistration.Get<ILogger>().Debug("Starting timeshifiting with username {0} on channel id {1}", userName, channelId);

      IInternalControllerService control = GlobalServiceProvider.Get<IInternalControllerService>();

      IVirtualCard card;
      IUser user;
      TvResult result = control.StartTimeShifting(currentUser.Name, channelId, out card, out user);
      ServiceRegistration.Get<ILogger>().Debug("Tried to start timeshifting, result {0}", result);

      if (result != TvResult.Succeeded)
      {
        // TODO: should we retry?
        ServiceRegistration.Get<ILogger>().Error("Starting timeshifting failed with result {0}", result);
        throw new Exception("Failed to start tv stream: " + result);
      }
      return card.RTSPUrl;
    }

    protected IUser GetUserByUserName(string userName, bool create = false)
    {
      if (userName == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("Used user with null name");
        return null;
      }

      if (!_tvUsers.ContainsKey(userName) && !create)
        return null;

      if (!_tvUsers.ContainsKey(userName) && create)
        _tvUsers.Add(userName, new User(userName, UserType.Normal));

      return _tvUsers[userName];
    }

    private static string GetUserName(int slotIndex)
    {
      return "NativeTvClient-" + slotIndex;
    }
  }
}
