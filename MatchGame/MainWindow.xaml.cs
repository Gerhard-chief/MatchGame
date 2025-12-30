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

            if (matchesFound == 8)
            {
                SetUpGame();
                return;
            }

            if (sender is not TextBlock textBlock) return;
            if (textBlock == timeTextBlock) return;

            // кликаем только по закрытым ("?")
            if (!IsClosed(textBlock)) return;

            // открываем карту и получаем её эмодзи
            if (!TryGetEmoji(textBlock, out string currentEmoji)) return;
            OpenCard(textBlock);

            // 1-й клик пары
            if (!findingMatch)
            {
                lastTextBlockClicked = textBlock;
                findingMatch = true;
                return;
            }

            // 2-й клик пары
            if (lastTextBlockClicked is null) { ResetTurn(); return; }

            TextBlock first = lastTextBlockClicked; // локальная переменная, уже не nullable
            if (textBlock == first) return;

            if (!TryGetEmoji(first, out string firstEmoji)) { ResetTurn(); return; }

            // совпало
            if (string.Equals(currentEmoji, firstEmoji, StringComparison.Ordinal))
            {
                textBlock.IsHitTestVisible = false;
                first.IsHitTestVisible = false;

                matchesFound++;
                ResetTurn();
                return;
            }

            // не совпало: показать 0.5 сек и закрыть обе
            isBusy = true;
            try
            {
                await Task.Delay(500);
                CloseCard(textBlock);
                CloseCard(first);
            }
            finally
            {
                isBusy = false;
            }

            ResetTurn();
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
        private static bool IsClosed(TextBlock tb) => tb.Text == "?";

        private static bool TryGetEmoji(TextBlock tb, out string emoji)
        {
            emoji = tb.Tag as string ?? "";
            return emoji.Length > 0;
        }

        private static void CloseCard(TextBlock tb) => tb.Text = "?";

        private static void OpenCard(TextBlock tb)
        {
            // Тут мы уверены, что Tag заполнен в SetUpGame()
            tb.Text = (string)tb.Tag;
        }

        private void ResetTurn()
        {
            findingMatch = false;
            lastTextBlockClicked = null;
        }

    }
}
