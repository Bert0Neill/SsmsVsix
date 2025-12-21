using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class SqlSuggestionPopup
    {
        private static Popup _popup;
        private static ListBox _listBox;
        private static Action<string> _onSelected;
        public static bool IsOpen => _popup != null && _popup.IsOpen;

        public static void Show(FrameworkElement parent, Point location, IList<string> suggestions, Action<string> onSelected)
        {
            Close();

            _onSelected = onSelected;

            _listBox = new ListBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                MinWidth = 360,
                MaxHeight = 200
            };

            foreach (var s in suggestions)
                _listBox.Items.Add(s);

            _listBox.SelectedIndex = 0;

            _listBox.MouseDoubleClick += (_, __) =>
            {
                AcceptSelection();
            };

            _popup = new Popup
            {
                Child = _listBox,
                PlacementTarget = parent,
                Placement = PlacementMode.Relative,
                HorizontalOffset = location.X,
                VerticalOffset = location.Y,
                StaysOpen = true,              // 🔑 SSMS requires this
                AllowsTransparency = true
            };

            _popup.IsOpen = true;
        }

        public static void SelectNext()
        {
            if (_listBox == null || _listBox.Items.Count == 0) return;

            int index = _listBox.SelectedIndex + 1;
            if (index >= _listBox.Items.Count)
                index = _listBox.Items.Count - 1;

            _listBox.SelectedIndex = index;
            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        public static void SelectPrevious()
        {
            if (_listBox == null || _listBox.Items.Count == 0) return;

            int index = _listBox.SelectedIndex - 1;
            if (index < 0)
                index = 0;

            _listBox.SelectedIndex = index;
            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        public static void AcceptSelection()
        {
            if (_listBox?.SelectedItem != null)
            {
                _onSelected?.Invoke(_listBox.SelectedItem.ToString());
            }

            Close();
        }

        public static void Close()
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
                _popup = null;
                _listBox = null;
                _onSelected = null;
            }
        }
    }
}
