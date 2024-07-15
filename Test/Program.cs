using OpenCvSharp;
using Tesseract;
using System;
using System.Linq;
using System.Reflection;
using Rect = OpenCvSharp.Rect;
using System.Drawing;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using static System.Net.Mime.MediaTypeNames;

public class LicensePlateOCR
{
    private string tessDataPath = @"\TesseractData"; // Modify as necessary

    public void ProcessImage(string inputPath)
    {
        Mat image = Cv2.ImRead(inputPath, ImreadModes.Grayscale);
        int original_width = image.Width;
        int original_height = image.Height;

        Mat image2 = new Mat();
        Mat image3 = new Mat();
        Mat image4 = new Mat();
        Mat drawing = new Mat();
        image.CopyTo(image2);
        image.CopyTo(image3);
        image.CopyTo(image4);

        // Image processing for contours
        //Cv2.CvtColor(image2, image2, ColorConversionCodes.BGR2GRAY);
        Cv2.ImShow("Gray", image2);
        Cv2.GaussianBlur(image2, image2, new Size(5, 5), 0);
        Cv2.ImShow("GausianBlur", image2);
        Cv2.Canny(image2, image2, 100, 200, 3);
        Cv2.ImShow("Canny", image2);

        // Finding contours
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(image2, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
        List<Point[]> contours_poly = new List<Point[]>(contours.Length);
        drawing = Mat.Zeros(image2.Size(), MatType.CV_8UC3);
        List<Rect> boundRect = new List<Rect>(contours.Length);
        List<Rect> boundRect2 = new List<Rect>();

        for (int i = 0; i < contours.Length; i++)
        {
            contours_poly.Add(Cv2.ApproxPolyDP(contours[i], 1, true));
            boundRect.Add(Cv2.BoundingRect(contours_poly[i]));
        }



        int refinery_count = 0;

        for (int i = 0; i < contours.Length; i++)
        {
            double ratio = (double)boundRect[i].Height / boundRect[i].Width;

            if ((ratio <= 2.5) && (ratio >= 0.5) && (boundRect[i].Width * boundRect[i].Height <= 700) && (boundRect[i].Width * boundRect[i].Height >= 100))
            {
                Cv2.DrawContours(drawing, contours, i, new Scalar(0, 255, 255), 1, LineTypes.Link8, hierarchy, 0);
                Cv2.Rectangle(drawing, boundRect[i].TopLeft, boundRect[i].BottomRight, new Scalar(255, 0, 0), 1, LineTypes.Link8);
                boundRect2.Add(boundRect[i]);
                refinery_count += 1;
            }
        }

        boundRect2 = boundRect2.Take(refinery_count).ToList();
        Cv2.ImShow("Contour", drawing);


















        // Sort rectangles by X-coordinate
        boundRect2.Sort((a, b) => a.X.CompareTo(b.X));

        int select = 0;
        int friend_count = 0;

        for (int i = 0; i < boundRect2.Count; i++)
        {
            Cv2.Rectangle(image3, boundRect2[i].TopLeft, boundRect2[i].BottomRight, new Scalar(0, 255, 0), 1, LineTypes.Link8);
            int count = 0;

            for (int j = i + 1; j < boundRect2.Count; j++)
            {
                double delta_x = Math.Abs(boundRect2[j].X - boundRect2[i].X);

                if (delta_x > 150)
                    break;

                double delta_y = Math.Abs(boundRect2[j].Y - boundRect2[i].Y);
                delta_x = delta_x == 0 ? 1 : delta_x;
                delta_y = delta_y == 0 ? 1 : delta_y;

                double gradient = delta_y / delta_x;

                if (gradient < 0.25)
                {
                    count += 1;
                }
            }

            if (count > friend_count)
            {
                select = i;
                friend_count = count;
                Cv2.Rectangle(image3, boundRect2[select].TopLeft, boundRect2[select].BottomRight, new Scalar(255, 0, 255), 1, LineTypes.Link8);
            }
        }

        List<Rect> carNumber = new List<Rect> { boundRect2[select] };
        Cv2.Rectangle(image4, boundRect2[select].TopLeft, boundRect2[select].BottomRight, new Scalar(0, 255, 0), 1, LineTypes.Link8);

        for (int i = 0; i < boundRect2.Count; i++)
        {
            if (boundRect2[select].X > boundRect2[i].X)
                continue;

            double delta_x = Math.Abs(boundRect2[select].X - boundRect2[i].X);

            if (delta_x > 50)
                continue;

            double delta_y = Math.Abs(boundRect2[select].Y - boundRect2[i].Y);
            delta_x = delta_x == 0 ? 1 : delta_x;
            delta_y = delta_y == 0 ? 1 : delta_y;

            double gradient = delta_y / delta_x;

            if (gradient < 0.25)
            {
                select = i;
                carNumber.Add(boundRect2[i]);
                Cv2.Rectangle(image4, boundRect2[i].TopLeft, boundRect2[i].BottomRight, new Scalar(0, 255, 0), 1, LineTypes.Link8);
            }
        }

        Cv2.ImShow("RectanglesOnPlate", image4);

        Mat cropped_image = new Mat();
        image.CopyTo(cropped_image);
        Point center1 = new Point((carNumber[0].TopLeft.X + carNumber[0].BottomRight.X) / 2, (carNumber[0].TopLeft.Y + carNumber[0].BottomRight.Y) / 2);
        Point center2 = new Point((carNumber[carNumber.Count - 1].TopLeft.X + carNumber[carNumber.Count - 1].BottomRight.X) / 2, (carNumber[carNumber.Count - 1].TopLeft.Y + carNumber[carNumber.Count - 1].BottomRight.Y) / 2);
        int plate_center_x = (center1.X + center2.X) / 2;
        int plate_center_y = (center1.Y + center2.Y) / 2;

        int sum_height = carNumber.Sum(rect => rect.Height);

        int plate_width = (int)((center2.X - center1.X + carNumber[carNumber.Count - 1].Width) * 1.05);
        int plate_height = (int)(sum_height / carNumber.Count * 1.2);

        double delta_x_center = center1.X - center2.X;
        double delta_y_center = center1.Y - center2.Y;

        double angle_degree = Math.Atan2(delta_y_center, delta_x_center) * (180 / Math.PI);

        Mat rotation_matrix = Cv2.GetRotationMatrix2D(new Point(plate_center_x, plate_center_y), angle_degree, 1.0);
        Cv2.WarpAffine(cropped_image, cropped_image, rotation_matrix, new Size(original_width, original_height));

        // Cropping the region of interest
        Rect roi = new Rect(plate_center_x - plate_width / 2, plate_center_y - plate_height / 2, plate_width, plate_height);
        Mat plate_image = new Mat(cropped_image, roi);

        Cv2.ImShow("Cropped Plate", plate_image);

        Cv2.GetRectSubPix(image, new Size(plate_width, plate_height), new Point2f(plate_center_x, plate_center_y), cropped_image);
        Cv2.GaussianBlur(cropped_image, cropped_image, new Size(5, 5), 0);
        Cv2.AdaptiveThreshold(cropped_image, cropped_image, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 19, 9);
        Cv2.CopyMakeBorder(cropped_image, cropped_image, 10, 10, 10, 10, BorderTypes.Constant, new Scalar(0, 0, 0));

        Cv2.ImWrite(@"C:\workspace\LMS\Test\Resources\temp.jpg", cropped_image);
        Cv2.ImShow("result", cropped_image);

        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
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
                string text = page.GetText();
                string licensePlate = ExtractCarNumber(text);
                Console.WriteLine("Car Number: " + licensePlate);
            }
        }
    }

    private string ExtractCarNumber(string text)
    {
        text = text.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
        var regex = new System.Text.RegularExpressions.Regex("\\d{2,3}[가-힣]\\d{4}");
        var match = regex.Match(text);
        return match.Success ? match.Value : "OCR 인식 오류 : " + text;
    }

    public static void Main(string[] args)
    {
        var processor = new LicensePlateOCR();
        string imagePath = @"C:\workspace\LMS\Test\Resources\06소6736.jpg";
        processor.ProcessImage(imagePath);
        imagePath = @"C:\workspace\LMS\Test\Resources\temp.jpg";
        processor.ExtractTextFromImage(imagePath);
    }
}
