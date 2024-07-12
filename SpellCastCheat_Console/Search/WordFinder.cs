namespace SpellCastCheat_Console
{
    public class WordFinder
    {
        private readonly BoardModel _board;
        private readonly WordList _wordList;
        private readonly int[,] _directions = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };

        public WordFinder(BoardModel board, WordList wordList)
        {
            _board = board;
            _wordList = wordList;
        }

        public List<WordResultDTO> Search(AppConfig config)
        {
            var result = new List<WordResultDTO>();
            var availableLetters = GetAvailableLetters();

            if (config.WordSearchMode == WordSearchMode.Swap)
            {
                result = FindBestWordsWithSingleSwap(config, availableLetters, config.WordFilteringMode);
            }
            else if (config.WordSearchMode == WordSearchMode.DoubleSwap)
            {
                result = FindBestWordsWithDoubleSwap(config, availableLetters, config.WordFilteringMode);
            }
            else if (config.WordSearchMode == WordSearchMode.Simple)
            {
                result = FindBestWords(config.ResultsCount, availableLetters);
            }

            return result;
        }

        private List<WordResultDTO> FindBestWords(int count, HashSet<char> availableLetters)
        {
            var bestWords = new List<WordResultDTO>();
            var filteredWords = FilterWords(_wordList.Words, availableLetters);

            foreach (var word in filteredWords)
            {
                var scoreResult = CalculateWordScore(word);
                if (scoreResult.IsValid)
                {
                    bestWords.Add(new WordResultDTO
                    {
                        Word = word,
                        Score = scoreResult.Score,
                        Path = scoreResult.Path.Select(p => new PathDTO { X = p.X, Y = p.Y }).ToList(),
                        Swaps = new List<SwapDTO>()
                    });
                }
            }

            return bestWords.GroupBy(w => w.Word)
                .Select(g => g.OrderByDescending(w => w.Score).First())
                .OrderByDescending(w => w.Score)
                .Take(count)
                .ToList();
        }

        private List<WordResultDTO> FindBestWordsWithSingleSwap(AppConfig config, HashSet<char> availableLetters, WordFilteringMode filteringMode = WordFilteringMode.Simple)
        {
            var bestWords = new List<WordResultDTO>();
            var originalGrid = (LetterModel[,])_board.Grid.Clone();

            var filteredWords = FilterWords(_wordList.Words.Where(word => word.Length >= config.SwapWordsMinLength && word.Length <= config.SwapWordsMaxLength), availableLetters, filteringMode);

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            for (int i = 0; i < _board.Grid.GetLength(0); i++)
            {
                for (int j = 0; j < _board.Grid.GetLength(1); j++)
                {
                    char originalLetter = _board.Grid[i, j].Letter;

                    foreach (char newLetter in alphabet)
                    {
                        if (originalLetter == newLetter) continue;

                        _board.Grid[i, j].Letter = newLetter;

                        foreach (var word in filteredWords)
                        {
                            var scoreResult = CalculateWordScore(word);
                            if (scoreResult.IsValid)
                            {
                                var swaps = new List<SwapDTO>
                                {
                                    new SwapDTO { X = i, Y = j, OldLetter = originalLetter, NewLetter = newLetter }
                                };

                                bestWords.Add(new WordResultDTO
                                {
                                    Word = word,
                                    Score = scoreResult.Score,
                                    Path = scoreResult.Path.Select(p => new PathDTO { X = p.X, Y = p.Y }).ToList(),
                                    Swaps = swaps
                                });
                            }
                        }

                        _board.Grid[i, j].Letter = originalLetter;
                    }
                }
            }

            _board.Grid = originalGrid;

            return bestWords.GroupBy(w => w.Word)
                .Select(g => g.OrderByDescending(w => w.Score).First())
                .OrderByDescending(w => w.Score)
                .Take(config.ResultsCount)
                .ToList();
        }

        private List<WordResultDTO> FindBestWordsWithDoubleSwap(AppConfig config, HashSet<char> availableLetters, WordFilteringMode filteringMode = WordFilteringMode.Simple)
        {
            var bestWords = new List<WordResultDTO>();
            var originalGrid = (LetterModel[,])_board.Grid.Clone();

            var filteredWords = FilterWords(_wordList.Words.Where(word => word.Length >= config.DoubleSwapWordsMinLength && word.Length <= config.DoubleSwapWordsMaxLength), availableLetters, filteringMode);

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            for (int i = 0; i < _board.Grid.GetLength(0); i++)
            {
                for (int j = 0; j < _board.Grid.GetLength(1); j++)
                {
                    char originalLetter = _board.Grid[i, j].Letter;

                    foreach (char newLetter in alphabet)
                    {
                        if (originalLetter == newLetter) continue;

                        _board.Grid[i, j].Letter = newLetter;

                        for (int m = 0; m < _board.Grid.GetLength(0); m++)
                        {
                            for (int n = 0; n < _board.Grid.GetLength(1); n++)
                            {
                                if (i == m && j == n) continue;

                                char originalLetter2 = _board.Grid[m, n].Letter;

                                foreach (char swapLetter in availableLetters)
                                {
                                    if (originalLetter2 == swapLetter) continue;

                                    _board.Grid[m, n].Letter = swapLetter;

                                    foreach (var word in filteredWords)
                                    {
                                        var scoreResult = CalculateWordScore(word);
                                        if (scoreResult.IsValid)
                                        {
                                            var swaps = new List<SwapDTO>
                                            {
                                                new SwapDTO { X = i, Y = j, OldLetter = originalLetter, NewLetter = newLetter },
                                                new SwapDTO { X = m, Y = n, OldLetter = originalLetter2, NewLetter = swapLetter }
                                            };

                                            bestWords.Add(new WordResultDTO
                                            {
                                                Word = word,
                                                Score = scoreResult.Score,
                                                Path = scoreResult.Path.Select(p => new PathDTO { X = p.X, Y = p.Y }).ToList(),
                                                Swaps = swaps
                                            });
                                        }
                                    }

                                    _board.Grid[m, n].Letter = originalLetter2;
                                }
                            }
                        }

                        _board.Grid[i, j].Letter = originalLetter;
                    }
                }
            }

            _board.Grid = originalGrid;

            return bestWords.GroupBy(w => w.Word)
                .Select(g => g.OrderByDescending(w => w.Score).First())
                .OrderByDescending(w => w.Score)
                .Take(config.ResultsCount)
                .ToList();
        }

        #region Filters
        private static IEnumerable<string> FilterWords(IEnumerable<string> words, HashSet<char> availableLetters, WordFilteringMode filteringMode = WordFilteringMode.Simple)
        {
            if (filteringMode == WordFilteringMode.Simple)
            {
                return FilterWordsByAvailableLetters(words, availableLetters);
            }
            else
            {
                return FilterWordsConsideringSwap(words, availableLetters);
            }
        }

        private static IEnumerable<string> FilterWordsByAvailableLetters(IEnumerable<string> words, HashSet<char> availableLetters)
        {
            return words.Where(word => word.All(letter => availableLetters.Contains(char.ToUpper(letter))));
        }

        private static IEnumerable<string> FilterWordsConsideringSwap(IEnumerable<string> words, HashSet<char> availableLetters)
        {
            return words.Where(word =>
            {
                var wordLetters = new HashSet<char>(word.ToUpper().ToCharArray());
                var missingLetters = wordLetters.Except(availableLetters).ToList();
                return missingLetters.Count <= 1;
            });
        }
        #endregion

        #region Score Calculations
        private bool DFS(string word, int index, int x, int y, bool[,] visited, List<PathDTO> path)
        {
            if (index == word.Length)
            {
                return true;
            }

            if (x < 0 || x >= _board.Grid.GetLength(0) || y < 0 || y >= _board.Grid.GetLength(1) || visited[x, y] || char.ToUpper(_board.Grid[x, y].Letter) != char.ToUpper(word[index]))
            {
                return false;
            }

            visited[x, y] = true;
            path.Add(new PathDTO { X = x, Y = y });

            for (int dir = 0; dir < 8; dir++)
            {
                int newX = x + _directions[dir, 0];
                int newY = y + _directions[dir, 1];
                if (DFS(word, index + 1, newX, newY, visited, path))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            visited[x, y] = false;
            return false;
        }

        private (int Score, List<PathDTO> Path, bool IsValid) CalculateWordScore(string word)
        {
            int n = _board.Grid.GetLength(0);
            int m = _board.Grid.GetLength(1);
            var visited = new bool[n, m];
            var path = new List<PathDTO>();
            bool isValid = false;
            int bestScore = 0;
            List<PathDTO> bestPath = new List<PathDTO>();

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (DFS(word, 0, i, j, visited, path))
                    {
                        isValid = true;

                        if (path.Count == word.Length)
                        {
                            int score = CalculateScore(path);

                            if (word.Length >= 6)
                            {
                                score += 10;
                            }

                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestPath = new List<PathDTO>(path);
                            }
                        }
                    }
                }
            }

            return (bestScore, bestPath, isValid);
        }

        private int CalculateScore(List<PathDTO> path)
        {
            int wordMultiplier = 1;
            int score = 0;

            foreach (var p in path)
            {
                var letter = _board.Grid[p.X, p.Y];
                int letterScore = letter.Points;

                score += letterScore;

                if (letter.IsDoubleWord)
                {
                    wordMultiplier *= 2;
                }
            }

            return score * wordMultiplier;
        }
        #endregion

        #region Other
        private HashSet<char> GetAvailableLetters()
        {
            var availableLetters = new HashSet<char>();
            int rows = _board.Grid.GetLength(0);
            int cols = _board.Grid.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    availableLetters.Add(char.ToUpper(_board.Grid[i, j].Letter));
                }
            }

            return availableLetters;
        }
        #endregion
    }
}
