namespace PlacementOfStamps.Models;

/// Простой класс для представления прямоугольника
public class Rectangle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rectangle()
    {
    }

    public Rectangle(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width.ToMillimeters();
        Height = height.ToMillimeters();
    }

    // Проверка на перекрытие с другим прямоугольником
    public bool Intersects(Rectangle other)
    {
        return !(X + Width < other.X || 
                 other.X + other.Width < X || 
                 Y + Height < other.Y || 
                 other.Y + other.Height < Y);
    }
}