using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Threading;

namespace beadando3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    class Felulet : FrameworkElement
    {
        ViewModel vm;
        DispatcherTimer dt;
        int mapStart = 1;
        int elet;

        public Felulet()
        {
            vm = new ViewModel();
            MapMaker(mapStart);
            elet = 3;
            //korrigáció a hibásan inicializált y miatt
            vm.J.Y = (vm.J.Y - 1)*20;

            Loaded += Felulet_Loaded;

            //timer
            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, 5);
            dt.Tick += dt_Tick;
            dt.Start();
        }

        void dt_Tick(object sender, EventArgs e)
        {
            
            if (vm.J.Enemy)
            {
                dt.Stop();
                vm.J.Enemy = false;
                MessageBox.Show("Életet vesztettél");
                elet--;

                vm = new ViewModel();
                MapMaker(mapStart);

                //korrigáció a hibásan inicializált y miatt
                vm.J.Y = (vm.J.Y - 1) * 20;

                InvalidateVisual();
                dt.Start();
            }
            if (elet <= 0)
            {
                dt.Stop();
                MessageBox.Show("Vége a játéknak. Nincs több életed. Vesztettél!");
            }
            if (vm.J.Move(vm.Elemek, vm))
            {
                mapStart++;

                if (mapStart <= 3)
                {
                    dt.Stop();
                    MessageBox.Show("Szint teljesítve!");
                    vm = new ViewModel();
                    elet++;
                    MapMaker(mapStart);

                    //korrigáció a hibásan inicializált y miatt
                    vm.J.Y = (vm.J.Y - 1) * 20;

                    InvalidateVisual();
                    dt.Start();
                }
                else
                {
                    dt.Stop();
                    MessageBox.Show("Vége a játéknak. Nyertél!");
                }
                
            }
            InvalidateVisual();
        }

        void Felulet_Loaded(object sender, RoutedEventArgs e)
        {
            (this.Parent as Window).KeyDown += Felulet_KeyDown;
            (this.Parent as Window).KeyUp += Felulet_KeyUp;
        }

        void Felulet_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                vm.J.Jobb = false;
            }
            if (e.Key == Key.Left)
            {
                vm.J.Bal = false;
            }
            if (e.Key == Key.Up)
            {
                vm.J.Jump = false;
            }
        }

        void Felulet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                vm.J.Jobb = true;
            }
            if (e.Key == Key.Left)
            {
                vm.J.Bal = true;
            }
            if (e.Key == Key.Up)
            {
                vm.J.Jump = true;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(Brushes.Orange, new Pen(Brushes.Black, 1), vm.J.Alak);
            foreach (PalyaElem p in vm.Elemek)
            {
                if (p is Exit)
                    drawingContext.DrawGeometry(Brushes.Lime, new Pen(Brushes.Black, 1), p.Alak);
                else
                {
                    if (p is Enemy)
                        drawingContext.DrawGeometry((p as Enemy).Szin, new Pen(Brushes.Black, 1), p.Alak);

                    else
                        drawingContext.DrawGeometry(Brushes.Gray, new Pen(Brushes.Black, 1), p.Alak);
                }
            }
            FormattedText f = new FormattedText("Életek száma: " + elet.ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 30, Brushes.Lime);
            Geometry g = f.BuildGeometry(new Point(0, 0));
            drawingContext.DrawGeometry(Brushes.Lime, new Pen(Brushes.Black, 1), f.BuildGeometry(new Point(0, 0)));
        }

        public void MapMaker(int mapSzam)
        {
            string mapSzamString = "0" + mapSzam.ToString();
            string[] mapMaking = File.ReadAllLines(mapSzamString+".lvl");
            int mapWidth = int.Parse(mapMaking[1]);
            int mapHeight = int.Parse(mapMaking[0]);
            Random rnd = new Random();

            vm.PalyaX = mapWidth*2*PalyaElem.elemMeret;
            vm.PalyaY = mapHeight*2*PalyaElem.elemMeret;

            for (int i = 2; i < mapHeight + 2; i++)
            {
                for (int j = 0; j < mapWidth; j++)
                {
                    if (mapMaking[i][j] == 'w')
                    {
                        vm.Elemek.Add(new PalyaElem(j*PalyaElem.elemMeret, i*PalyaElem.elemMeret));
                    }
                    if (mapMaking[i][j] == 'E')
                    {
                        vm.Elemek.Add(new Exit(j*PalyaElem.elemMeret, i*PalyaElem.elemMeret));
                    }
                    if (mapMaking[i][j] == 'x')
                    {
                        vm.Elemek.Add(new Enemy(j * PalyaElem.elemMeret, i * PalyaElem.elemMeret, rnd.Next(0,3)));
                    }
                    
                }
            }
        }
    }

    class Elem
    {
        protected Geometry alak;
        protected int x, y;

        public int Y { get { return y; } set { y = value; } }

        public Geometry Alak 
        { 
            get 
            {
                Geometry copy = alak.Clone();
                TransformGroup tg = new TransformGroup();
                tg.Children.Add(new TranslateTransform(x, y));
                copy.Transform = tg;
                return copy.GetFlattenedPathGeometry();
            } 
        }

        public Elem(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Utkozik(Elem e)
        {
            return Geometry.Combine(Alak, e.Alak, GeometryCombineMode.Intersect, null).GetArea() > 0;
        }
    }

    class Exit : PalyaElem
    {
        public Exit(int x, int y)
            : base(x, y)
        {

        }
    }

    class PalyaElem : Elem
    {
        public const int elemMeret = 10;
        public PalyaElem(int x, int y)
            : base(x, y)
        {
            alak = new RectangleGeometry(new Rect(x, y, PalyaElem.elemMeret * 2, PalyaElem.elemMeret * 2));
        }
    }

    class Enemy : PalyaElem
    {
        Brush szin;
        int dx, dy;
        int tempX;
        int tempY;

        public Brush Szin { get { return szin; } }

        public Enemy(int x, int y, int tipus)
            :base(x, y)
        {
            tempX = 0;
            tempY = 0;

            if (tipus == 0)
            {
                szin = Brushes.Red;
                alak = new RectangleGeometry(new Rect(x, y, PalyaElem.elemMeret * 2, PalyaElem.elemMeret * 2));
                dx = 5;
                dy = 0;
            }
            if (tipus == 1)
            {
                szin = Brushes.Purple;
                alak = new EllipseGeometry(new Rect(x, y, PalyaElem.elemMeret * 2, PalyaElem.elemMeret * 2));
                dy = -3;
                dx = 0;
            }
            if (tipus == 2)
            {
                szin = Brushes.Yellow;
                alak = new RectangleGeometry(new Rect(x, y, PalyaElem.elemMeret * 2, PalyaElem.elemMeret * 2));
                dy = -4;
                dx = 4;
            }
        }

        public void Move()
        {
            //x : azt veszi 0-nak ahova letettük az elemet
            //Bounds.Left : relatív érték a pálya bal széléhez képest
            //MessageBox.Show("x: "+x.ToString()+" bounds: "+alak.Bounds.Left);
            y += dy;
            x += dx;
            tempX += dx;
            tempY += dy;

            //x
            if (x >= 1600 - 2*PalyaElem.elemMeret - alak.Bounds.Right || x <= 0 - alak.Bounds.Left + 2*PalyaElem.elemMeret)
            {
                dx = -dx;
                tempX = 0;
            }

            //y
            if ( tempY < 0 && tempY <= -(3*2*PalyaElem.elemMeret - 2*PalyaElem.elemMeret))
            {
                dy = -dy;
                tempY = 0;
            }
            if (tempY > 0 && tempY >= 3*2*PalyaElem.elemMeret - 2*PalyaElem.elemMeret)
            {
                dy = -dy;
                tempY = 0;
            }
        }

    }

    class Jatekos : Elem
    {
        bool jobb, bal, jump, enemy;
        public bool Jobb { get { return jobb; } set { jobb = value; } }
        public bool Bal { get { return bal; } set { bal = value; } }
        public bool Jump { get { return jump; } set { jump = value; } }
        int dy, dx, elet;

        public bool Enemy { get { return enemy; } set { enemy = value; } }
        public int Elet { get { return elet; } set { elet = value; } }


        public Jatekos(int x, int y)
            : base(x, y)
        {
            alak = new EllipseGeometry(new Rect(x, y, 2* PalyaElem.elemMeret, 2* PalyaElem.elemMeret));
            jobb = false;
            bal = false;
            jump = false;
            dy = 0;
            dx = 4;
            enemy = false;
        }

        public bool Move(List<PalyaElem> lista, ViewModel vm)
        {
            //MessageBox.Show(y.ToString());
            y += dy;
            if (dy < 2 * PalyaElem.elemMeret)
            {
                dy++;
            }

            if (jobb)
            {
                x += dx;
            }
            if (bal)
            {
                x -= dx;
            }
            foreach (PalyaElem p in lista)
            {
                if (p is Enemy)
                {
                    (p as Enemy).Move();
                }
                if (Utkozik(p))
                {
                    if (p is Exit)
                    {
                        return true;
                    }
                    if (p is Enemy)
                    {
                        enemy = true;
                    }
                    x -= dx;
                    if (Utkozik(p))
                    {
                        x += 2 * dx;
                        if (Utkozik(p))
                        {
                            x -= dx;
                        }
                    }

                    if (Utkozik(p) && y + 2* PalyaElem.elemMeret >= p.Alak.Bounds.Top)
                    {
                        y = (int)p.Alak.Bounds.Bottom - 2 * PalyaElem.elemMeret;
                        dy = 0;
                        dy = -dy;
                    }
                    else { 
                        if (Utkozik(p) && y + 4* PalyaElem.elemMeret >= p.Alak.Bounds.Top)
                        {
                            dy = 0;
                            y = (int)p.Alak.Bounds.Top - 4 * PalyaElem.elemMeret;
                        }
                    }
                    
                    if (jump)
                    {
                        dy = - 12;
                    }

                    jump = false;

                }
                
            }
            if (y > vm.PalyaY + 2*PalyaElem.elemMeret)
            {
                vm.J.Enemy = true;
            }
            return false;
        }
    }

    class ViewModel
    {
        Jatekos j;
        List<PalyaElem> elemek;
        int palyaX, palyaY;

        public int PalyaX { get { return palyaX; } set { palyaX = value; } }
        public int PalyaY { get { return palyaY; } set { palyaY = value; } }

        public Jatekos J { get { return j; } set { j = value; } }
        public List<PalyaElem> Elemek { get { return elemek; } set { elemek = value; } }


        public ViewModel()
        {
            
            elemek = new List<PalyaElem>();
            j = new Jatekos(20, 20);
        }
    }

    class BusinessLogic
    {

    }
}
