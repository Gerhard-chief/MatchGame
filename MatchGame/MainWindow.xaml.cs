using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MatchGame
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly DispatcherTimer previewTimer = new DispatcherTimer();

        private int tenthsOfSecondsElapsed;
        private int matchesFound;

        private TextBlock? lastTextBlockClicked;
        private bool findingMatch;
        private bool isBusy;

        public MainWindow()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += Timer_Tick;

            previewTimer.Interval = TimeSpan.FromSeconds(2);
            previewTimer.Tick += PreviewTimer_Tick;

            SetUpGame();
        }

        // --- Timer (игровой) ---
        private void Timer_Tick(object? sender, EventArgs e)
        {
            tenthsOfSecondsElapsed++;
            timeTextBlock.Text = (tenthsOfSecondsElapsed / 10F).ToString("0.0s");

            if (matchesFound == 8)
            {
                timer.Stop();
                timeTextBlock.Text += " - Play again?";
            }
        }

        // --- Timer (превью 2 сек) ---
        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            previewTimer.Stop();

            // закрываем все карточки на "?"
            foreach (var tb in GetCardTextBlocks())
            {
                tb.Text = "?";
            }

            // запускаем игру
            tenthsOfSecondsElapsed = 0;
            timeTextBlock.Text = "0.0s";
            isBusy = false;
            timer.Start();
        }

        // --- Инициализация игры ---
        private void SetUpGame()
        {
            // стопаем все таймеры/состояния
            timer.Stop();
            previewTimer.Stop();

            tenthsOfSecondsElapsed = 0;
            matchesFound = 0;

            findingMatch = false;
            lastTextBlockClicked = null;

            // во время превью/перекладок клики блокируем
            isBusy = true;

            timeTextBlock.Text = "Memorize!";

            List<string> animalEmoji = new()
            {
                "🐙","🐙",
                "🐟","🐟",
                "🐎","🐎",
                "🐘","🐘",
                "🐪","🐪",
                "🦕","🦕",
                "🦘","🦘",
                "🦔","🦔",
            };

            // раскидываем эмодзи в Tag, показываем их на 2 секунды
            foreach (TextBlock tb in GetCardTextBlocks())
            {
                tb.Visibility = Visibility.Visible;
                tb.IsHitTestVisible = true;

                int index = Random.Shared.Next(animalEmoji.Count);
                string emoji = animalEmoji[index];
                animalEmoji.RemoveAt(index);

                tb.Tag = emoji;  // "секрет"
                tb.Text = emoji; // превью: показываем
            }

            previewTimer.Start(); // через 2 сек закроем на "?" и начнем таймер
        }

        // --- Клик по карточке ---
        private async void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isBusy) return;

            // после победы: клик по карточкам = рестарт
            if (matchesFound == 8)
            {
                SetUpGame();
                return;
            }

            if (sender is not TextBlock textBlock) return;
            if (textBlock == timeTextBlock) return;

            // если уже открыта (не "?") — не даем кликать
            if (textBlock.Text != "?") return;

            // открываем: берем эмодзи из Tag
            if (textBlock.Tag is not string currentEmoji) return;
            textBlock.Text = currentEmoji;

            // 1-й клик пары
            if (!findingMatch)
            {
                lastTextBlockClicked = textBlock;
                findingMatch = true;
                return;
            }

            // 2-й клик пары
            if (lastTextBlockClicked is null) { findingMatch = false; return; }
            if (textBlock == lastTextBlockClicked) return;

            if (lastTextBlockClicked.Tag is not string lastEmoji)
            {
                findingMatch = false;
                lastTextBlockClicked = null;
                return;
            }

            // совпало
            if (string.Equals(currentEmoji, lastEmoji, StringComparison.Ordinal))
            {
                textBlock.IsHitTestVisible = false;
                lastTextBlockClicked.IsHitTestVisible = false;

                matchesFound++;
                findingMatch = false;
                lastTextBlockClicked = null;
                return;
            }

            // не совпало: показать 0.5 сек и закрыть обе
            isBusy = true;
            try
            {
                await Task.Delay(500);
                textBlock.Text = "?";
                lastTextBlockClicked.Text = "?";
            }
            finally
            {
                isBusy = false;
            }

            findingMatch = false;
            lastTextBlockClicked = null;
        }

        // --- Клик по нижнему тексту ---
        private void TimeTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (matchesFound == 8)
                SetUpGame();
        }

        // --- Хелпер: получить только игровые TextBlock (без timeTextBlock) ---
        private IEnumerable<TextBlock> GetCardTextBlocks()
        {
            return mainGrid.Children
                .OfType<TextBlock>()
                .Where(tb => tb != timeTextBlock);
        }
    }
}
