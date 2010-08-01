#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.UI.Views;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents a view to navigate to.
  /// </summary>
  /// <remarks>
  /// Instances of this class represent view items to be displayed in a GUI view's items list.
  /// View's items lists contain view items (<see cref="NavigationItem"/>s) as well as
  /// playable items (<see cref="PlayableItem"/>).
  /// </remarks>
  public class NavigationItem : ListItem
  {
    #region Public consts

    public const string KEY_NAME = "Name";

    #endregion

    #region Protected fields

    protected View _view;
    protected string _overrideName;

    #endregion

    public NavigationItem(View view, string overrideName)
    {
      _view = view;
      _overrideName = overrideName;
      UpdateData();
    }

    public void UpdateData()
    {
      string name = string.IsNullOrEmpty(_overrideName) ? _view.DisplayName : _overrideName;
      SetLabel(KEY_NAME, name);

      // TODO: Other properties
    }

    public View View
    {
      get { return _view; }
    }
  }
}