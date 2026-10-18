using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TermiDOOM
{
    internal sealed class Enemy
    {
        public double X;
        public double Y;
        public bool Alive;
        public int FlashFrames;

        public Enemy(double x, double y)
        {
            X = x;
            Y = y;
            Alive = true;
        }
    }

    internal static class Program
    {
        private static readonly string[] Map =
        {
            "####################",
            "#..................#",
            "#..#...............#",
            "#..#...............#",
            "#..#...............#",
            "#......#...........#",
            "#......#...........#",
            "#......#..#........#",
            "#.........#........#",
            "#....#....#....##..#",
            "#....#.........##..#",
            "#....#.............#",
            "#....#.............#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "####################"
        }; /* Map Here, can be configured */
        /* SETTINGS */
        private const double Fov = Math.PI / 2.8;
        private const double MaxDepth = 22.0;
        private const double MoveSpeed = 0.20;
        private const double TurnSpeed = 0.12;

        private static double playerX = 2.5;
        private static double playerY = 2.5;
        private static double playerAngle = 0.0;
        private static int health = 100;
        private static int ammo = 30;
        private static int kills = 0;
        private static int fireFrames = 0;
        private static int hurtFrames = 0;
        private static bool running = true;
        private static readonly List<Enemy> Enemies = new List<Enemy>();
        private static readonly Random Rng = new Random();

        private static int screenWidth;
        private static int screenHeight;
        private static int viewHeight;
        private static int viewWidth;
        private static bool colorEnabled = true;
        /* SETTINGS */
        private static void Main()
        {
            Enemies.Add(new Enemy(10.5, 2.5));
            Enemies.Add(new Enemy(15.5, 4.5));
            Enemies.Add(new Enemy(11.5, 8.5));
            Enemies.Add(new Enemy(4.5, 10.5));
            Enemies.Add(new Enemy(15.5, 13.5));
            Enemies.Add(new Enemy(7.5, 16.5));
            Enemies.Add(new Enemy(17.5, 17.5));

            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.TreatControlCAsInput = true;
            Console.Write("\u001b[?1049h\u001b[?25l\u001b[2J");
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                running = false;
            };

            try
            {
                while (running)
                {
                    UpdateSize();
                    DateTime frameStart = DateTime.UtcNow;
                    ReadInput();
                    UpdateEnemies();
                    Render();
                    int elapsed = (int)(DateTime.UtcNow - frameStart).TotalMilliseconds;
                    int sleep = 45 - elapsed;
                    if (sleep > 0) Thread.Sleep(sleep);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\u001b[0m\u001b[?25h\u001b[?1049l");
                Console.WriteLine("TermiDOOM stopped: " + ex.Message);
            }
            finally
            {
                Console.Write("\u001b[0m\u001b[?25h\u001b[?1049l");
                Console.WriteLine("Thanks for playing TermiDOOM!");
            }
        }

        private static void UpdateSize()
        {
            try
            {
                screenWidth = Math.Max(60, Math.Min(140, Console.WindowWidth - 1));
                screenHeight = Math.Max(20, Math.Min(45, Console.WindowHeight - 1));
            }
            catch
            {
                screenWidth = 80;
                screenHeight = 24;
            }

            viewWidth = screenWidth;
            viewHeight = screenHeight - 3;
            if (viewHeight < 12) viewHeight = 12;
        }

        private static void ReadInput()
        {
            while (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape)
                {
                    running = false;
                    return;
                }

                if (key.Key == ConsoleKey.LeftArrow) playerAngle -= TurnSpeed;
                else if (key.Key == ConsoleKey.RightArrow) playerAngle += TurnSpeed;
                else if (key.Key == ConsoleKey.W || key.Key == ConsoleKey.UpArrow)
                    Move(Math.Cos(playerAngle) * MoveSpeed, Math.Sin(playerAngle) * MoveSpeed);
                else if (key.Key == ConsoleKey.S || key.Key == ConsoleKey.DownArrow)
                    Move(-Math.Cos(playerAngle) * MoveSpeed, -Math.Sin(playerAngle) * MoveSpeed);
                else if (key.Key == ConsoleKey.A)
                    Move(Math.Cos(playerAngle - Math.PI / 2.0) * MoveSpeed,
                         Math.Sin(playerAngle - Math.PI / 2.0) * MoveSpeed);
                else if (key.Key == ConsoleKey.D)
                    Move(Math.Cos(playerAngle + Math.PI / 2.0) * MoveSpeed,
                         Math.Sin(playerAngle + Math.PI / 2.0) * MoveSpeed);
                else if (key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.Enter)
                    Shoot();
                else if (key.Key == ConsoleKey.R)
                {
                    ammo = 30;
                    health = Math.Min(100, health + 10);
                }
                else if (key.Key == ConsoleKey.C)
                    colorEnabled = !colorEnabled;
            }

            playerAngle %= Math.PI * 2.0;
            if (playerAngle < 0) playerAngle += Math.PI * 2.0;
        }

        private static void Move(double dx, double dy)
        {
            double nx = playerX + dx;
            double ny = playerY + dy;
            if (!IsWall(nx, playerY)) playerX = nx;
            if (!IsWall(playerX, ny)) playerY = ny;
        }

        private static bool IsWall(double x, double y)
        {
            int mx = (int)x;
            int my = (int)y;
            if (mx < 0 || my < 0 || my >= Map.Length || mx >= Map[0].Length)
                return true;
            return Map[my][mx] == '#';
        }

        private static double CastRay(double angle)
        {
            double distance = 0.0;
            const double step = 0.025;
            while (distance < MaxDepth)
            {
                distance += step;
                if (IsWall(playerX + Math.Cos(angle) * distance,
                           playerY + Math.Sin(angle) * distance))
                    return distance;
            }
            return MaxDepth;
        }

        private static void Shoot()
        {
            if (ammo <= 0) return;
            ammo--;
            fireFrames = 3;

            Enemy target = null;
            double bestDistance = MaxDepth;
            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Alive) continue;
                double dx = enemy.X - playerX;
                double dy = enemy.Y - playerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double angle = NormalizeAngle(Math.Atan2(dy, dx) - playerAngle);
                if (Math.Abs(angle) < 0.075 && distance < bestDistance &&
                    CastRay(playerAngle) + 0.2 >= distance)
                {
                    target = enemy;
                    bestDistance = distance;
                }
            }

            if (target != null)
            {
                target.Alive = false;
                kills++;
            }
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle < -Math.PI) angle += Math.PI * 2.0;
            while (angle > Math.PI) angle -= Math.PI * 2.0;
            return angle;
        }

        private static void UpdateEnemies()
        {
            if (fireFrames > 0) fireFrames--;
            if (hurtFrames > 0) hurtFrames--;

            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Alive) continue;
                double dx = playerX - enemy.X;
                double dy = playerY - enemy.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > 1.2 && distance < 8.0 && Rng.Next(100) < 12)
                {
                    double stepX = dx / Math.Max(distance, 0.001) * 0.10;
                    double stepY = dy / Math.Max(distance, 0.001) * 0.10;
                    if (!IsWall(enemy.X + stepX, enemy.Y)) enemy.X += stepX;
                    if (!IsWall(enemy.X, enemy.Y + stepY)) enemy.Y += stepY;
                }

                if (distance < 1.1 && Rng.Next(100) < 7)
                {
                    health = Math.Max(0, health - 5);
                    hurtFrames = 2;
                }
            }

            if (health <= 0)
            {
                health = 100;
                ammo = 30;
                kills = 0;
                playerX = 2.5;
                playerY = 2.5;
                playerAngle = 0.0;
                foreach (Enemy enemy in Enemies) enemy.Alive = true;
            }
        }

        private static void Render()
        {
            char[,] chars = new char[viewHeight, viewWidth];
            ConsoleColor[,] colors = new ConsoleColor[viewHeight, viewWidth];
            double[] depth = new double[viewWidth];

            for (int x = 0; x < viewWidth; x++)
            {
                double rayAngle = playerAngle - Fov / 2.0 + ((double)x / viewWidth) * Fov;
                double rawDistance = CastRay(rayAngle);
                double distance = rawDistance * Math.Cos(rayAngle - playerAngle);
                if (distance < 0.1) distance = 0.1;
                depth[x] = distance;

                int wallHeight = (int)(viewHeight / distance * 1.15);
                int ceiling = (viewHeight - wallHeight) / 2;
                int floor = ceiling + wallHeight;

                char wallChar;
                ConsoleColor wallColor;
                if (distance < 2.2) { wallChar = '█'; wallColor = ConsoleColor.White; }
                else if (distance < 4.0) { wallChar = '▓'; wallColor = ConsoleColor.Gray; }
                else if (distance < 7.0) { wallChar = '▒'; wallColor = ConsoleColor.DarkGray; }
                else if (distance < 11.0) { wallChar = '░'; wallColor = ConsoleColor.DarkYellow; }
                else { wallChar = '.'; wallColor = ConsoleColor.DarkGray; }

                for (int y = 0; y < viewHeight; y++)
                {
                    if (y < ceiling)
                    {
                        double sky = (double)y / Math.Max(1, viewHeight / 2);
                        chars[y, x] = sky < 0.25 ? ' ' : sky < 0.55 ? '.' : ':';
                        colors[y, x] = sky < 0.25 ? ConsoleColor.DarkBlue : ConsoleColor.Blue;
                    }
                    else if (y <= floor && y < viewHeight)
                    {
                        chars[y, x] = wallChar;
                        colors[y, x] = wallColor;
                    }
                    else
                    {
                        double shade = (double)(y - viewHeight / 2) / viewHeight;
                        chars[y, x] = shade > 0.75 ? '#' : shade > 0.5 ? 'x' : shade > 0.25 ? '.' : ',';
                        colors[y, x] = shade > 0.75 ? ConsoleColor.DarkGray :
                                       shade > 0.5 ? ConsoleColor.Gray :
                                       shade > 0.25 ? ConsoleColor.DarkGreen : ConsoleColor.Green;
                    }
                }
            }

            foreach (Enemy enemy in Enemies)
            {
                if (!enemy.Alive) continue;
                double dx = enemy.X - playerX;
                double dy = enemy.Y - playerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double angle = NormalizeAngle(Math.Atan2(dy, dx) - playerAngle);
                if (Math.Abs(angle) > Fov / 2.0 || distance < 0.1) continue;

                int centerX = (int)((0.5 + angle / Fov) * viewWidth);
                if (centerX < 0 || centerX >= viewWidth || distance >= depth[centerX]) continue;

                int size = Math.Max(3, Math.Min(viewHeight - 2, (int)(viewHeight / distance * 0.85)));
                int top = Math.Max(0, viewHeight / 2 - size / 2);
                int bottom = Math.Min(viewHeight - 1, top + size);
                int halfWidth = Math.Max(1, size / 3);
                int left = Math.Max(0, centerX - halfWidth);
                int right = Math.Min(viewWidth - 1, centerX + halfWidth);

                for (int y = top; y <= bottom; y++)
                {
                    for (int x = left; x <= right; x++)
                    {
                        int relY = y - top;
                        int relX = x - centerX;
                        char sprite;
                        if (relY < size / 4)
                            sprite = (Math.Abs(relX) <= 1) ? 'O' : '^';
                        else if (relY < size / 2)
                            sprite = (Math.Abs(relX) <= 1) ? 'M' : '/';
                        else
                            sprite = (Math.Abs(relX) <= 1) ? 'W' : '|';

                        chars[y, x] = sprite;
                        colors[y, x] = relY < size / 4 ? ConsoleColor.Red : ConsoleColor.Yellow;
                    }
                }
            }

            // Bright center crosshair.
            int midY = viewHeight / 2;
            int midX = viewWidth / 2;
            chars[midY, midX] = fireFrames > 0 ? '*' : '+';
            colors[midY, midX] = fireFrames > 0 ? ConsoleColor.Yellow : ConsoleColor.White;

            StringBuilder output = new StringBuilder(viewHeight * (viewWidth + 12));
            output.Append("\u001b[H");
            ConsoleColor lastColor = (ConsoleColor)(-1);

            for (int y = 0; y < viewHeight; y++)
            {
                for (int x = 0; x < viewWidth; x++)
                {
                    if (colorEnabled && colors[y, x] != lastColor)
                    {
                        output.Append(ColorCode(colors[y, x]));
                        lastColor = colors[y, x];
                    }
                    output.Append(chars[y, x]);
                }
                output.Append("\u001b[0m\r\n");
                lastColor = (ConsoleColor)(-1);
            }

            int alive = 0;
            foreach (Enemy enemy in Enemies) if (enemy.Alive) alive++;

            output.Append("\u001b[1;37m");
            output.Append(" HP ");
            output.Append(health.ToString("000"));
            output.Append("   AMMO ");
            output.Append(ammo.ToString("00"));
            output.Append("   KILLS ");
            output.Append(kills.ToString("00"));
            output.Append("/");
            output.Append(Enemies.Count.ToString("00"));
            output.Append("   ENEMIES ");
            output.Append(alive.ToString("00"));
            output.Append("\u001b[0m\r\n");

            if (hurtFrames > 0) output.Append("\u001b[1;31m!! YOU ARE TAKING DAMAGE !!\u001b[0m");
            else if (alive == 0) output.Append("\u001b[1;32mAREA CLEARED! Press Q to quit.\u001b[0m");
            else output.Append("\u001b[1;36mWASD move  |  arrows turn  |  SPACE/ENTER fire  |  R reload+heal  |  C color  |  Q quit\u001b[0m");

            Console.Write(output.ToString());
        }

        private static string ColorCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return "\u001b[30m";
                case ConsoleColor.DarkBlue: return "\u001b[34m";
                case ConsoleColor.DarkGreen: return "\u001b[32m";
                case ConsoleColor.DarkCyan: return "\u001b[36m";
                case ConsoleColor.DarkRed: return "\u001b[31m";
                case ConsoleColor.DarkMagenta: return "\u001b[35m";
                case ConsoleColor.DarkYellow: return "\u001b[33m";
                case ConsoleColor.Gray: return "\u001b[37m";
                case ConsoleColor.DarkGray: return "\u001b[90m";
                case ConsoleColor.Blue: return "\u001b[94m";
                case ConsoleColor.Green: return "\u001b[92m";
                case ConsoleColor.Cyan: return "\u001b[96m";
                case ConsoleColor.Red: return "\u001b[91m";
                case ConsoleColor.Magenta: return "\u001b[95m";
                case ConsoleColor.Yellow: return "\u001b[93m";
                default: return "\u001b[97m";
            }
        }
    }
}
