using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace MatchGame
{
	public partial class MainWindow : Window
	{
		private readonly DispatcherTimer timer = new DispatcherTimer();
		private int tenthsOfSecondsElapsed;
		private int matchesFound;

		private TextBlock? lastTextBlockClicked;
		private bool findingMatch = false;
        private bool isBusy;


        public MainWindow()
		{
			InitializeComponent();

			timer.Interval = TimeSpan.FromSeconds(0.1);
			timer.Tick += Timer_Tick;

			SetUpGame();
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			tenthsOfSecondsElapsed++;
			timeTextBlock.Text = (tenthsOfSecondsElapsed / 10F).ToString("0.0s");

			if (matchesFound == 8)
			{
				timer.Stop();
				timeTextBlock.Text = timeTextBlock.Text + " - Play again?";
			}
		}

		private void SetUpGame()
		{
			timer.Stop();
			tenthsOfSecondsElapsed = 0;
			matchesFound = 0;
			timeTextBlock.Text = "0.0s";

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

			foreach (TextBlock textBlock in mainGrid.Children.OfType<TextBlock>())
			{
				if (textBlock == timeTextBlock) continue; // нижний TextBlock не трогаем

				textBlock.Visibility = Visibility.Visible;

				int index = Random.Shared.Next(animalEmoji.Count);
				textBlock.Text = animalEmoji[index];
				animalEmoji.RemoveAt(index);
			}

			findingMatch = false;
			lastTextBlockClicked = null;

			timer.Start();
		}

        private async void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)

        {
            // если игра уже выиграна — любой клик по карточке перезапускает
            if (matchesFound == 8)
			{
				SetUpGame();
				return;
			}
            if (isBusy) return;
            if (sender is not TextBlock textBlock) return;
			if (textBlock == timeTextBlock) return; // страховка

			// 1-й клик
			if (!findingMatch)
			{
				textBlock.Visibility = Visibility.Hidden;
				lastTextBlockClicked = textBlock;
				findingMatch = true;
				return;
			}

            // 2-й клик
            if (lastTextBlockClicked is null) { findingMatch = false; return; }
            if (textBlock == lastTextBlockClicked) return;

            // показываем второй (на всякий)
            textBlock.Visibility = Visibility.Hidden;

            if (textBlock.Text == lastTextBlockClicked.Text)
            {
                matchesFound++;
                // оба уже Hidden — оставляем
            }
            else
            {
                isBusy = true;
                try
                {
                    await Task.Delay(500);
                    textBlock.Visibility = Visibility.Visible;
                    lastTextBlockClicked.Visibility = Visibility.Visible;
                }
                finally
                {
                    isBusy = false;
                }

            }

            findingMatch = false;
            lastTextBlockClicked = null;
        }

		private void TimeTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
		{
			// по книге: перезапуск после победы
			if (matchesFound == 8)
				SetUpGame();
		}
	}
}
