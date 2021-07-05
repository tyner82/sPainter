using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Spainter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DrawingMode currentMode = DrawingMode.Paint;
        double strokeWidth = 50;
        ImageBrush loadedImg = new ImageBrush();
        bool enteringText = false;
        bool erasing = false;
        List<Canvas> eraseMasks = new  List<Canvas>();
        Canvas currentEraseMask;
        enum DrawingMode
        {
            Paint = 0,
            Text = 1,
            Erase = 2,
            Zoom = 3
        };

        Point previousPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        Canvas NewEraseMask()
        {
            eraseMasks.Add(new Canvas()
            {
                Height = cnvImage.ActualHeight,
                Width = cnvImage.ActualWidth,
                Background=Brushes.Transparent
            });
            return eraseMasks[eraseMasks.Count() - 1];
        }

        private void SubscribeEvents(Canvas cnvImage, DrawingMode currentMode)
        {
            switch (currentMode)
            {
                case DrawingMode.Text:
                    cnvImage.MouseDown += cnvImage_TextMouseDown;
                    break;
                case DrawingMode.Erase:
                    cnvImage.MouseDown += cnvImage_EraseMouseDown;
                    cnvImage.PreviewMouseMove += cnvImage_EraseMouseMove;
                    break;
                case DrawingMode.Zoom:
                    cnvImage.MouseDown += cnvImage_ZoomMouseDown;
                    cnvImage.PreviewMouseRightButtonDown += cnvImage_ZoomPreviewMouseRightButtonDown;
                    cnvImage.PreviewMouseLeftButtonDown += cnvImage_ZoomPreviewMouseLeftButtonDown;
                    cnvImage.PreviewMouseMove += cnvImage_ZoomPreviewMouseMove;
                    break;
                default:
                    cnvImage.MouseDown += cnvImage_MouseDown;
                    cnvImage.PreviewMouseMove += cnvImage_PreviewMouseMove;
                    break;
            }
            
        }

        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawingMode.Paint;
            RemoveCanvasEvents(cnvImage);
            SubscribeEvents(cnvImage, currentMode);
        }

        private void Erase_Click(object sender, RoutedEventArgs e)
        {
            //cnvImage.Children.Clear();
            currentMode = DrawingMode.Erase;
            RemoveCanvasEvents(cnvImage);
            SubscribeEvents(cnvImage, currentMode);
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawingMode.Zoom;
            RemoveCanvasEvents(cnvImage);
            SubscribeEvents(cnvImage, currentMode);
        }

        private void Text_Click(object sender, RoutedEventArgs e)
        {
            currentMode = DrawingMode.Text;
            RemoveCanvasEvents(cnvImage);
            SubscribeEvents(cnvImage, currentMode);
            SetTextBoxesEnabled(true);
        }

        private void SetTextBoxesEnabled(bool v)
        {
            foreach (var tb in FindVisualChildren<TextBox>(this))
            {
                tb.IsHitTestVisible = v;
            }
        }

        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                if (child != null && child is T)
                    yield return (T)child;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        private void cnvImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            previousPoint = e.GetPosition(cnvImage);
            Shape startPoint = new Rectangle()
                                    {
                                        Fill = Brushes.White,
                                        Height = strokeWidth,
                                        Width = strokeWidth,
                                        RadiusX = strokeWidth/2,
                                        RadiusY = strokeWidth/2
                                    };
            Canvas.SetLeft(startPoint, previousPoint.X - strokeWidth/2);
            Canvas.SetTop(startPoint, previousPoint.Y - strokeWidth/2);
            cnvImage.Children.Add(startPoint);
        }

        private void cnvImage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point thisPoint = e.GetPosition(cnvImage);
                Shape line = new Line()
                {
                    X1 = previousPoint.X,
                    X2 = thisPoint.X,
                    Y1 = previousPoint.Y,
                    Y2 = thisPoint.Y,
                    Stroke = System.Windows.Media.Brushes.White,
                    StrokeThickness = strokeWidth,
                    StrokeEndLineCap = PenLineCap.Round
                };
                previousPoint = thisPoint;
                cnvImage.Children.Add(line);
            }
        }

        private void cnvImage_TextMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!enteringText)
            {
                previousPoint = e.GetPosition(cnvImage);
                enteringText = true;
                TextBox textOnImage = new TextBox() {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.White,
                    Foreground = Brushes.White,
                    FontSize = 16
                };
                textOnImage.LostFocus += TextOnImage_LostFocus;
                textOnImage.GotFocus += TextOnImage_GotFocus;
                textOnImage.PreviewKeyDown += TextOnImage_PreviewKeyDown;
                Canvas.SetLeft(textOnImage, previousPoint.X);
                Canvas.SetTop(textOnImage, previousPoint.Y);
                cnvImage.Children.Add(textOnImage);
                textOnImage.Focus();
            }
            else
            {
                IInputElement focusedControl = Keyboard.FocusedElement;
                TextOnImage_LostFocus(focusedControl, new RoutedEventArgs());
            }
        }

        private void TextOnImage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox control = (TextBox)sender;
            if(string.IsNullOrEmpty(control.Text) && (e.Key.Equals(Key.Delete) || e.Key.Equals(Key.Back)))
            {
                control.LostFocus -= TextOnImage_LostFocus;
                control.GotFocus -= TextOnImage_GotFocus;
                control.KeyDown -= TextOnImage_PreviewKeyDown;
                cnvImage.Children.Remove(control);
                enteringText = false;
            }
            else if (e.Key.Equals(Key.Return) || e.Key.Equals(Key.Enter))
            {
                TextOnImage_LostFocus(sender, new RoutedEventArgs());
            }
        }

        private void TextOnImage_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            control.BorderBrush = Brushes.White;
            enteringText = true;
        }

        private void TextOnImage_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            enteringText = false;
            control.BorderBrush = Brushes.Transparent;
            Draw.Focus();
            if (string.IsNullOrEmpty(control.Text))
            {
                control.LostFocus -= TextOnImage_LostFocus;
                control.GotFocus -= TextOnImage_GotFocus;
                control.KeyDown -= TextOnImage_PreviewKeyDown;
                cnvImage.Children.Remove(control);
            }
        }

        private void cnvImage_EraseMouseDown(object sender, MouseButtonEventArgs e)
        {
            Canvas control = (Canvas)sender;
            if (!erasing)
                currentEraseMask = NewEraseMask();

            Point thisPoint = e.GetPosition(cnvImage);
            if (thisPoint.X > cnvImage.Width - strokeWidth / 2 ||
                thisPoint.X < strokeWidth / 2 ||
                thisPoint.Y > cnvImage.Height - strokeWidth / 2 ||
                thisPoint.X < strokeWidth / 2)
                return;

            previousPoint = thisPoint;
            Shape startPoint = new Rectangle()
            {
                Fill = Brushes.Black,
                Height = strokeWidth,
                Width = strokeWidth,
                RadiusX = strokeWidth / 2,
                RadiusY = strokeWidth / 2
            };
            Canvas.SetLeft(startPoint, previousPoint.X - strokeWidth / 2);
            Canvas.SetTop(startPoint, previousPoint.Y - strokeWidth / 2);
            currentEraseMask.Children.Add(startPoint);
            Image mask = new Image()
            {
                Source = loadedImg.ImageSource,
                OpacityMask = new VisualBrush(currentEraseMask),
                Height = cnvImage.Height,
                Width = cnvImage.Width
            };
            //imgContainer.Child = mask;
            if(erasing)
                cnvImage.Children.Remove(cnvImage.Children[cnvImage.Children.Count-1]);
            cnvImage.Children.Add(mask);
            erasing = true;
        }

        private void cnvImage_EraseMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point thisPoint = e.GetPosition(cnvImage);
                if (thisPoint.X > cnvImage.Width-strokeWidth/2 ||
                    thisPoint.X < strokeWidth / 2 ||
                    thisPoint.Y > cnvImage.Height - strokeWidth / 2 ||
                    thisPoint.X < strokeWidth / 2 ) 
                    return;

                Shape line = new Line()
                {
                    X1 = previousPoint.X,
                    X2 = thisPoint.X,
                    Y1 = previousPoint.Y,
                    Y2 = thisPoint.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = strokeWidth,
                    StrokeEndLineCap = PenLineCap.Round
                };
                previousPoint = thisPoint;
                currentEraseMask.Children.Add(line);
                //Image mask = new Image()
                //{
                //    Source = loadedImg.ImageSource,
                //    OpacityMask = new VisualBrush(cnvEraseMask)
                //};
                //cnvImage.Children.Add(mask);
            }
        }

        private void cnvImage_ZoomMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void cnvImage_ZoomPreviewMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void cnvImage_ZoomPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void cnvImage_ZoomPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        void RemoveCanvasEvents(Canvas canvas)
        {
            RemoveRoutedEventHandlers(cnvImage, Canvas.MouseDownEvent);
            RemoveRoutedEventHandlers(cnvImage, Canvas.PreviewMouseLeftButtonDownEvent);
            RemoveRoutedEventHandlers(cnvImage, Canvas.PreviewMouseRightButtonDownEvent);
            RemoveRoutedEventHandlers(cnvImage, Canvas.PreviewMouseMoveEvent);
            SetTextBoxesEnabled(false);
            erasing = false;
        }

        /// <summary>
        /// Credit: https://stackoverflow.com/a/12618521
        /// Removes all event handlers subscribed to the specified routed event from the specified element.
        /// </summary>
        /// <param name="element">The UI element on which the routed event is defined.</param>
        /// <param name="routedEvent">The routed event for which to remove the event handlers.</param>
        public static void RemoveRoutedEventHandlers(UIElement element, RoutedEvent routedEvent)
        {
            // Get the EventHandlersStore instance which holds event handlers for the specified element.
            // The EventHandlersStore class is declared as internal.
            var eventHandlersStoreProperty = typeof(UIElement).GetProperty(
                "EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
            object eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

            // If no event handlers are subscribed, eventHandlersStore will be null.
            // Credit: https://stackoverflow.com/a/16392387/1149773
            if (eventHandlersStore == null)
                return;

            // Invoke the GetRoutedEventHandlers method on the EventHandlersStore instance 
            // for getting an array of the subscribed event handlers.
            var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod(
                "GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var routedEventHandlers = (RoutedEventHandlerInfo[])getRoutedEventHandlers.Invoke(
                eventHandlersStore, new object[] { routedEvent });

            if (routedEventHandlers == null)
            {
                return;
            }
            // Iteratively remove all routed event handlers from the element.
            foreach (var routedEventHandler in routedEventHandlers)
                element.RemoveHandler(routedEvent, routedEventHandler.Handler);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            SetTextBoxesEnabled(false);
            erasing = false;
            OpenFileDialog openFileDialog = new OpenFileDialog() { 
                DefaultExt = "*.jpg,*.png,*.gif,*.bmp"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                loadedImg.ImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                loadedImg.Stretch = Stretch.Uniform;
                cnvImage.Height = loadedImg.ImageSource.Height;
                cnvImage.Width = loadedImg.ImageSource.Width;
                cnvImage.Background = loadedImg;
                cnvImage.Children.Clear();
            }
        }
    }
}
