using SSMS_AI_Buddy.Models;
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
        private static Action<SqlSuggestion> _onSelected;

        public static bool IsOpen => _popup != null && _popup.IsOpen;

        public static void Show(FrameworkElement parent, Point location, IList<SqlSuggestion> suggestions, Action<SqlSuggestion> onSelected)
        {
            Close(); // remove old popup and handlers

            _onSelected = onSelected;

            _listBox = new ListBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                MinWidth = 420,
                MaxHeight = 240,
                ItemContainerStyle = CreateItemStyle(),
                DisplayMemberPath = "Suggestion" // <-- show the SQL text
            };

            foreach (var s in suggestions)
            {
                _listBox.Items.Add(s);
            }

            // Select first item
            _listBox.SelectedIndex = 0;

            // Mouse double-click handler
            _listBox.MouseDoubleClick += (_, __) => AcceptSelection();

            _popup = new Popup
            {
                Child = _listBox,
                PlacementTarget = parent,
                Placement = PlacementMode.Relative,
                HorizontalOffset = location.X,
                VerticalOffset = location.Y,
                StaysOpen = false, // closes if user clicks elsewhere
                AllowsTransparency = true
            };

            _popup.IsOpen = true;

            // Focus so that keyboard can navigate
            _listBox.Focus();
        }

        private static void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AcceptSelection();
        }

        public static void SelectNext()
        {
            if (_listBox == null || _listBox.Items.Count == 0) return;

            _listBox.SelectedIndex = Math.Min(
                _listBox.SelectedIndex + 1,
                _listBox.Items.Count - 1);

            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        public static void SelectPrevious()
        {
            if (_listBox == null || _listBox.Items.Count == 0) return;

            _listBox.SelectedIndex = Math.Max(
                _listBox.SelectedIndex - 1,
                0);

            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        public static void AcceptSelection()
        {
            if (_listBox?.SelectedItem is SqlSuggestion suggestion)
            {
                _onSelected?.Invoke(suggestion);
            }

            Close();
        }

        public static void Close()
        {
            if (_popup != null)
            {
                if (_listBox != null)
                    _listBox.MouseDoubleClick -= ListBox_MouseDoubleClick;

                _popup.IsOpen = false;
                _popup.Child = null;

                _popup = null;
                _listBox = null;
                _onSelected = null;
            }
        }

        private static Style CreateItemStyle()
        {
            var style = new Style(typeof(ListBoxItem));

            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 3, 6, 3)));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));

            // Hover (light blue)
            style.Triggers.Add(new Trigger
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true,
                Setters =
        {
            new Setter(Control.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(230, 240, 255)))
        }
            });

            // Selected + focused (dark blue, white text)
            var focusedSelectedTrigger = new MultiTrigger();
            focusedSelectedTrigger.Conditions.Add(
                new Condition(ListBoxItem.IsSelectedProperty, true));
            focusedSelectedTrigger.Conditions.Add(
                new Condition(UIElement.IsKeyboardFocusWithinProperty, true));

            focusedSelectedTrigger.Setters.Add(
                new Setter(Control.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(45, 110, 210))));
            focusedSelectedTrigger.Setters.Add(
                new Setter(Control.ForegroundProperty, Brushes.White));

            style.Triggers.Add(focusedSelectedTrigger);

            // Selected but NOT focused (inactive selection)
            var inactiveSelectedTrigger = new MultiTrigger();
            inactiveSelectedTrigger.Conditions.Add(
                new Condition(ListBoxItem.IsSelectedProperty, true));
            inactiveSelectedTrigger.Conditions.Add(
                new Condition(UIElement.IsKeyboardFocusWithinProperty, false));

            inactiveSelectedTrigger.Setters.Add(
                new Setter(Control.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(210, 220, 235))));
            inactiveSelectedTrigger.Setters.Add(
                new Setter(Control.ForegroundProperty, Brushes.Black));

            style.Triggers.Add(inactiveSelectedTrigger);

            return style;
        }

    }
}