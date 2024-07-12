using System.Diagnostics;
using OpenCvSharp;

namespace SpellCastCheat_Console
{
    public class ImageProcessor
    {
        private readonly Mat _image;
        private readonly Dictionary<char, Mat> _letterTemplates;
        private Mat _dlTemplate;
        private Mat _tlTemplate;
        private Mat _doubleWordTemplate;
        private Rect _boardRect;

        // Define the scale ratios - template size to board size for resizing templates depends on board size
        private const double ScaleRatioX = 12.73;
        private const double ScaleRatioY = 12.82;

        public ImageProcessor(string imagePath)
        {
            _image = new Mat(imagePath, ImreadModes.Color);

            // Load Tesseract
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/tessdata");
            _ = new Tesseract.TesseractEngine(tessDataPath, "eng", Tesseract.EngineMode.Default);

            _letterTemplates = new Dictionary<char, Mat>();
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/templates");

            foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                string letterFilePath = Path.Combine(templatePath, $"{letter}.png");
                if (File.Exists(letterFilePath))
                {
                    _letterTemplates[letter] = new Mat(letterFilePath, ImreadModes.Grayscale); // Use grayscale for letters
                }
            }

            _dlTemplate = new Mat(Path.Combine(templatePath, "DL.png"), ImreadModes.Color);
            _tlTemplate = new Mat(Path.Combine(templatePath, "TL.png"), ImreadModes.Color);
            _doubleWordTemplate = new Mat(Path.Combine(templatePath, "2X.png"), ImreadModes.Color);
        }

        public LetterModel[,] ParseBoard()
        {
            Mat preprocessedImage = PreprocessImage(_image);
            _boardRect = DetectBoard(preprocessedImage);

            Console.WriteLine($"Detected board size: {_boardRect.Width}x{_boardRect.Height}");

            Mat board = new Mat(_image, _boardRect);
            ResizeTemplates(_boardRect.Width, _boardRect.Height);
            return SegmentAndParseGrid(board);
        }

        private void ResizeTemplates(int boardWidth, int boardHeight)
        {
            int targetWidth = (int)(boardWidth / ScaleRatioX);
            int targetHeight = (int)(boardHeight / ScaleRatioY);

            Console.WriteLine($"Resizing templates to: {targetWidth}x{targetHeight}");

            foreach (var key in _letterTemplates.Keys.ToList())
            {
                _letterTemplates[key] = _letterTemplates[key].Resize(new Size(targetWidth, targetHeight));
            }

            _dlTemplate = _dlTemplate.Resize(new Size(targetWidth, targetHeight));
            _tlTemplate = _tlTemplate.Resize(new Size(targetWidth, targetHeight));
            _doubleWordTemplate = _doubleWordTemplate.Resize(new Size(targetWidth, targetHeight));
        }

        public void SaveResultsImages(string outputImagePath, LetterModel[,] grid, List<WordResultDTO> wordResults)
        {
            List<Mat> resultImages = new List<Mat>();

            foreach (var wordResult in wordResults)
            {
                Mat resultImage = _image.Clone();

                // Draw arrows between letters
                for (int i = 0; i < wordResult.Path.Count - 1; i++)
                {
                    var start = wordResult.Path[i];
                    var end = wordResult.Path[i + 1];

                    var startPoint = new Point(_boardRect.X + grid[start.X, start.Y].LetterCenterX, _boardRect.Y + grid[start.X, start.Y].LetterCenterY);
                    var endPoint = new Point(_boardRect.X + grid[end.X, end.Y].LetterCenterX, _boardRect.Y + grid[end.X, end.Y].LetterCenterY);

                    // Add padding to start and end points
                    var direction = endPoint - startPoint;
                    var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
                    var padding = 18; // Adjust padding as needed
                    var paddingVector = direction * (padding / length);

                    var paddedStartPoint = startPoint + paddingVector;
                    var paddedEndPoint = endPoint - paddingVector;

                    Cv2.ArrowedLine(resultImage, paddedStartPoint, paddedEndPoint, Scalar.Red, 4, LineTypes.AntiAlias, 0, 0.2);
                }

                // Draw circles for swaps
                foreach (var swap in wordResult.Swaps)
                {
                    var center = new Point(_boardRect.X + grid[swap.X, swap.Y].LetterCenterX, _boardRect.Y + grid[swap.X, swap.Y].LetterCenterY);
                    Cv2.Circle(resultImage, center, 25, Scalar.White, -1); // White background
                    Cv2.Circle(resultImage, center, 25, Scalar.Blue, 2); // Blue border
                    Cv2.PutText(resultImage, swap.NewLetter.ToString(), new Point(center.X - 10, center.Y + 10), HersheyFonts.HersheySimplex, 0.8, Scalar.Blue, 2);
                }

                // Add word with score text
                var textPoint = new Point(_boardRect.X, _boardRect.Y);
                Cv2.Rectangle(resultImage, new Rect(textPoint.X - 40, textPoint.Y - 20, 350, 40), Scalar.White, -1);
                Cv2.PutText(resultImage, $"{wordResult.Word} - {wordResult.Score}", textPoint, HersheyFonts.HersheySimplex, 0.8, Scalar.Black, 2);

                resultImages.Add(resultImage);
            }

            // Concatenate result images vertically
            Mat concatenatedResultImage = ConcatenateImagesVertically(resultImages);

            // Ensure the output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputImagePath));

            // Save the concatenated image
            concatenatedResultImage.SaveImage(outputImagePath);

            // Open the image using the default photo viewer
            string fullPath = Path.GetFullPath(outputImagePath);
            string url = "file:///" + fullPath.Replace("\\", "/");
            Process.Start(new ProcessStartInfo("explorer.exe", url) { UseShellExecute = true });
        }

        private Mat ConcatenateImagesVertically(List<Mat> images)
        {
            int totalHeight = images.Sum(img => img.Height);
            int maxWidth = images.Max(img => img.Width);

            Mat result = new Mat(new Size(maxWidth, totalHeight), MatType.CV_8UC3, Scalar.White);

            int currentY = 0;
            foreach (var img in images)
            {
                img.CopyTo(result[new Rect(0, currentY, img.Width, img.Height)]);
                currentY += img.Height;
            }

            return result;
        }

        #region Preprocessing
        private Mat PreprocessImage(Mat image)
        {
            Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Mat thresh = new Mat();
            Cv2.AdaptiveThreshold(gray, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 11, 2);
            return thresh;
        }

        private Rect DetectBoard(Mat preprocessedImage)
        {
            Cv2.FindContours(preprocessedImage, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            int maxAreaIndex = -1;
            double maxArea = 0;

            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    maxAreaIndex = i;
                }
            }

            if (maxAreaIndex == -1)
            {
                Console.WriteLine("Can't detect board");
                throw new Exception();
            }

            return Cv2.BoundingRect(contours[maxAreaIndex]);
        }
        #endregion

        #region Grid Segmentation
        private LetterModel[,] SegmentAndParseGrid(Mat board)
        {
            int cellWidth = board.Width / 5;
            int cellHeight = board.Height / 5;
            var grid = new LetterModel[5, 5];
            var letterCenters = new List<(char letter, Point center, int row, int col)>();

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    OpenCvSharp.Rect cellRect = new OpenCvSharp.Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    Mat cell = new Mat(board, cellRect);
                    char detectedLetter = MatchLetter(cell);
                    Point center = new Point(cellRect.X + cellRect.Width / 2, cellRect.Y + cellRect.Height / 2);
                    grid[row, col] = new LetterModel(detectedLetter, row, col)
                    {
                        LetterCenterX = center.X,
                        LetterCenterY = center.Y
                    };
                    letterCenters.Add((detectedLetter, center, row, col));
                }
            }

            var specialTiles = DetectSpecialTiles(board);
            MapSpecialTilesToClosestLetters(grid, specialTiles, letterCenters);
            PrintGrid(grid);

            return grid;
        }
        #endregion

        #region Special Tiles Detection
        private List<(string type, Point center)> DetectSpecialTiles(Mat board)
        {
            var specialTiles = new List<(string type, Point center)>();

            DetectTemplate(board, _dlTemplate, "DL", specialTiles);
            DetectTemplate(board, _tlTemplate, "TL", specialTiles);
            DetectTemplate(board, _doubleWordTemplate, "2X", specialTiles);

            return specialTiles;
        }

        private void DetectTemplate(Mat board, Mat template, string type, List<(string type, Point center)> specialTiles, double threshold = 0.89)
        {
            Mat result = new Mat();
            Cv2.MatchTemplate(board, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            while (maxVal >= threshold)
            {
                Point center = new Point(maxLoc.X + template.Width / 2, maxLoc.Y + template.Height / 2);
                specialTiles.Add((type, center));

                Console.WriteLine($"{type} marker detected at {center}");

                // Invalidate the matched area to find new matches
                Cv2.FloodFill(result, maxLoc, new Scalar(0), out _, new Scalar(0.1), new Scalar(1.0));
                Cv2.MinMaxLoc(result, out _, out maxVal, out _, out maxLoc);
            }
        }
        #endregion

        #region Letter Matching
        private char MatchLetter(Mat cell)
        {
            Mat grayCell = new Mat();
            Cv2.CvtColor(cell, grayCell, ColorConversionCodes.BGR2GRAY); // Convert cell to grayscale for letter matching

            grayCell = PreprocessCellImage(grayCell);

            char bestMatch = '?';
            double maxVal = 0.0;

            foreach (var template in _letterTemplates)
            {
                double threshold = (template.Key == 'G' || template.Key == 'Q') ? 0.92 : 0.89;
                double result = MatchTemplateValue(grayCell, template.Value, threshold);
                if (result > maxVal)
                {
                    maxVal = result;
                    bestMatch = template.Key;
                }
            }

            return bestMatch;
        }

        private static double MatchTemplateValue(Mat cell, Mat template, double threshold)
        {
            Mat result = new Mat();
            Cv2.MatchTemplate(cell, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal);
            return maxVal >= threshold ? maxVal : 0.0;
        }

        private static Mat PreprocessCellImage(Mat cell)
        {
            if (cell.Type() != MatType.CV_8UC3)
            {
                Mat newCell = new Mat();
                cell.ConvertTo(newCell, MatType.CV_8UC3);
                cell = newCell;
            }
            Cv2.GaussianBlur(cell, cell, new Size(3, 3), 0);
            return cell;
        }
        #endregion

        #region Mapping and Debugging
        private static void MapSpecialTilesToClosestLetters(LetterModel[,] grid, List<(string type, Point center)> specialTiles, List<(char letter, Point center, int row, int col)> letterCenters)
        {
            foreach (var (type, tileCenter) in specialTiles)
            {
                double minDistance = double.MaxValue;
                int closestRow = 0;
                int closestCol = 0;

                foreach (var (_, letterCenter, row, col) in letterCenters)
                {
                    double distance = CalculateDistance(tileCenter, letterCenter);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestRow = row;
                        closestCol = col;
                    }
                }

                switch (type)
                {
                    case "DL":
                        grid[closestRow, closestCol].IsDoubleLetter = true;
                        break;
                    case "TL":
                        grid[closestRow, closestCol].IsTripleLetter = true;
                        break;
                    case "2X":
                        grid[closestRow, closestCol].IsDoubleWord = true;
                        break;
                }

                Console.WriteLine($"{type} marker mapped to letter '{grid[closestRow, closestCol].Letter}' at ({closestRow}, {closestCol})");
            }
        }

        private static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private static void PrintGrid(LetterModel[,] grid)
        {
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    var cell = grid[row, col];
                    Console.Write($"[{cell.Letter} ({cell.LetterCenterX},{cell.LetterCenterY}) ");
                    if (cell.IsDoubleLetter) Console.Write("DL ");
                    if (cell.IsTripleLetter) Console.Write("TL ");
                    if (cell.IsDoubleWord) Console.Write("2X ");
                    Console.Write("] ");
                }
                Console.WriteLine();
            }
        }
        #endregion
    }
}
