using OpenCvSharp;
using Tesseract;
using System;
using System.Linq;
using System.Reflection;

public class LicensePlateOCR
{
    private string tessDataPath = @"\TesseractData"; // Modify as necessary

    public void ProcessImage(string inputPath)
    {
        using (var src = Cv2.ImRead(inputPath))
        using (var gray = new Mat())
        using (var blur = new Mat())
        using (var edges = new Mat())
        {
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(5, 5), 0);
            Cv2.Canny(blur, edges, 100, 300);

            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(edges, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                double area = rect.Width * rect.Height;

                if ((double)rect.Height / rect.Width <= 2.5 && (double)rect.Height / rect.Width >= 0.5 && area <= 700 && area >= 100)
                {
                    Cv2.Rectangle(src, rect, Scalar.Green, 1);
                }
            }

            Cv2.ImShow("Detected Plates", src);
            Cv2.WaitKey(0);
            Cv2.DestroyAllWindows();
        }
    }

    public void ExtractTextFromImage(string imagePath)
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
        path = Path.Combine(path, "tessdata");
        path = path.Replace("file:\\", "");
        using (var engine = new TesseractEngine(path, "kor", EngineMode.TesseractOnly))
        using (var img = Pix.LoadFromFile(imagePath))
        {
            using (var page = engine.Process(img, PageSegMode.SingleLine))
            {
                var text = page.GetText();
                string carNumber = ExtractCarNumber(text);
                Console.WriteLine("Car Number: " + carNumber);
            }
        }
    }

    private string ExtractCarNumber(string text)
    {
        var regex = new System.Text.RegularExpressions.Regex("\\d{2,3}[가-힣]\\d{4}");
        var match = regex.Match(text);
        return match.Success ? match.Value : "No number found";
    }

    public static void Main(string[] args)
    {
        var processor = new LicensePlateOCR();
        string imagePath = @"C:\workspace\LMS\Test\Resources\01마3235.jpg";
        processor.ProcessImage(imagePath);
        processor.ExtractTextFromImage(imagePath);
    }
}
