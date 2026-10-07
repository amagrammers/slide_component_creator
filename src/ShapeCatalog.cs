using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SlideComponentCreator;

/// <summary>図形の定義。adjust は 0〜1 の調整値。</summary>
public record ShapeDef(
    string Name,
    string AdjustLabel,
    double DefaultAdjust,
    double DefaultWidth,
    double DefaultHeight,
    Func<double, double, double, Geometry> Build)
{
    public bool HasAdjust => AdjustLabel != "（なし）";

    /// <summary>周りを丸や四角で囲める図形（アイコン用）。</summary>
    public bool Enclosable { get; init; }

    /// <summary>四辺ごとの形（0=凸 1=凹 2=平、上・右・下・左の順）を受け取る図形用のビルダー。</summary>
    public Func<double, double, double, int[], Geometry>? BuildSided { get; init; }

    public override string ToString() => Name;
}

public record SubCategory(string Name, IReadOnlyList<ShapeDef> Shapes)
{
    public override string ToString() => Name;
}

public record Category(string Name, IReadOnlyList<SubCategory> SubCategories)
{
    public override string ToString() => Name;
}

public static class ShapeCatalog
{
    public static IReadOnlyList<Category> All { get; } = new[]
    {
        new Category("基本図形", new[]
        {
            new SubCategory("矢羽根", new[]
            {
                new ShapeDef("ホームベース", "先端の深さ", 0.5, 300, 100, HomePlate),
                new ShapeDef("シェブロン", "切り込みの深さ", 0.5, 300, 100, Chevron),
            }),
            new SubCategory("四角", new[]
            {
                new ShapeDef("四角形", "（なし）", 0, 300, 160, (w, h, a) => new RectangleGeometry(new Rect(0, 0, w, h))),
                new ShapeDef("角丸四角形", "角の丸み", 0.3, 300, 160, RoundRect),
                new ShapeDef("平行四辺形", "傾き", 0.3, 300, 120, Parallelogram),
                new ShapeDef("ひし形", "（なし）", 0, 200, 200, (w, h, a) => Poly(new(w / 2, 0), new(w, h / 2), new(w / 2, h), new(0, h / 2))),
            }),
            new SubCategory("丸", new[]
            {
                new ShapeDef("円・楕円", "（なし）", 0, 200, 200, (w, h, a) => new EllipseGeometry(new Rect(0, 0, w, h))),
                new ShapeDef("小判", "（なし）", 0, 300, 100, (w, h, a) => new RectangleGeometry(new Rect(0, 0, w, h), Math.Min(w, h) / 2, Math.Min(w, h) / 2)),
            }),
            new SubCategory("歯車", new[]
            {
                new ShapeDef("歯車（8歯）", "歯の深さ", 0.5, 200, 200, (w, h, a) => Gear(w, h, a, 8, true)),
                new ShapeDef("歯車（12歯）", "歯の深さ", 0.4, 200, 200, (w, h, a) => Gear(w, h, a, 12, true)),
                new ShapeDef("歯車（穴なし）", "歯の深さ", 0.5, 200, 200, (w, h, a) => Gear(w, h, a, 8, false)),
            }),
            new SubCategory("ファイル", new[]
            {
                new ShapeDef("ファイル", "折り返しの大きさ", 0.5, 160, 200, (w, h, a) => FileShape(w, h, a, false)),
                new ShapeDef("ファイル（罫線入り）", "折り返しの大きさ", 0.5, 160, 200, (w, h, a) => FileShape(w, h, a, true)),
            }),
            new SubCategory("データベース", new[]
            {
                new ShapeDef("データベース（1段）", "上面の厚み", 0.5, 160, 200, (w, h, a) => Cylinder(w, h, a, 1)),
                new ShapeDef("データベース（3段）", "上面の厚み", 0.5, 160, 240, (w, h, a) => Cylinder(w, h, a, 3)),
            }),
            new SubCategory("三角", new[]
            {
                new ShapeDef("三角形", "頂点の位置", 0.5, 200, 180, (w, h, a) => Poly(new(w * a, 0), new(w, h), new(0, h))),
                new ShapeDef("逆三角形", "頂点の位置", 0.5, 200, 180, (w, h, a) => Poly(new(0, 0), new(w, 0), new(w * a, h))),
                new ShapeDef("右向き三角形", "頂点の位置", 0.5, 180, 200, (w, h, a) => Poly(new(0, 0), new(w, h * a), new(0, h))),
                new ShapeDef("左向き三角形", "頂点の位置", 0.5, 180, 200, (w, h, a) => Poly(new(w, 0), new(w, h), new(0, h * a))),
            }),
            new SubCategory("吹き出し", new[]
            {
                new ShapeDef("四角吹き出し", "しっぽの位置", 0.2, 300, 200, (w, h, a) => Bubble(w, h, a, 0)),
                new ShapeDef("角丸吹き出し", "しっぽの位置", 0.2, 300, 200, (w, h, a) => Bubble(w, h, a, 1)),
                new ShapeDef("楕円吹き出し", "しっぽの位置", 0.2, 300, 200, (w, h, a) => Bubble(w, h, a, 2)),
            }),
            new SubCategory("矢印", new[]
            {
                new ShapeDef("右矢印", "軸の太さ", 0.5, 300, 120, RightArrow),
                new ShapeDef("下矢印", "軸の太さ", 0.5, 120, 300, DownArrow),
            }),
        }),
        new Category("アイコン", new[]
        {
            new SubCategory("記号", new[]
            {
                new ShapeDef("丸チェック", "チェックの太さ", 0.5, 200, 200, CircleCheck),
                new ShapeDef("丸バツ", "バツの太さ", 0.5, 200, 200, CircleCross),
                new ShapeDef("四角チェック", "チェックの太さ", 0.5, 200, 200, SquareCheck),
                new ShapeDef("四角バツ", "バツの太さ", 0.5, 200, 200, SquareCross),
                new ShapeDef("チェックボックス", "チェックの太さ", 0.5, 200, 200, CheckBox),
            }),
            new SubCategory("コード", new[]
            {
                new ShapeDef("コード </>", "線の太さ", 0.5, 200, 140, CodeIcon) { Enclosable = true },
                new ShapeDef("コード（四角＋行）", "（なし）", 0, 200, 200, CodeWindowIcon) { Enclosable = true },
            }),
            new SubCategory("虫眼鏡", new[]
            {
                new ShapeDef("虫眼鏡", "リングの太さ", 0.5, 200, 200, MagnifierIcon) { Enclosable = true },
            }),
            new SubCategory("ロケット", new[]
            {
                new ShapeDef("ロケット", "（なし）", 0, 200, 200, RocketIcon) { Enclosable = true },
            }),
            new SubCategory("ロボット", new[]
            {
                new ShapeDef("ロボット（顔）", "（なし）", 0, 200, 200, RobotIcon) { Enclosable = true },
            }),
            new SubCategory("棒グラフ", new[]
            {
                new ShapeDef("棒グラフ（3本）", "（なし）", 0, 200, 200, BarChartIcon) { Enclosable = true },
                new ShapeDef("棒グラフ（右上がり矢印）", "（なし）", 0, 200, 200, BarChartArrowIcon) { Enclosable = true },
            }),
            new SubCategory("人", new[]
            {
                new ShapeDef("人（1人）", "（なし）", 0, 200, 200, PersonIcon) { Enclosable = true },
                new ShapeDef("人（3人）", "（なし）", 0, 200, 200, PeopleIcon) { Enclosable = true },
            }),
            new SubCategory("セキュリティ", new[]
            {
                new ShapeDef("盾とチェック", "チェックの太さ", 0.5, 200, 200, ShieldIcon) { Enclosable = true },
            }),
            new SubCategory("電球", new[]
            {
                new ShapeDef("電球", "（なし）", 0, 200, 200, LightBulbIcon) { Enclosable = true },
            }),
            new SubCategory("パズル", new[]
            {
                new ShapeDef("パズルのピース", "凸凹の大きさ", 0.5, 200, 200, (w, h, a) => Puzzle(w, h, a, new[] { 0, 0, 1, 1 }))
                    { Enclosable = true, BuildSided = Puzzle },
            }),
            new SubCategory("タイマー", new[]
            {
                new ShapeDef("タイマー（針）", "（なし）", 0, 200, 200, ClockIcon) { Enclosable = true },
                new ShapeDef("ストップウォッチ", "（なし）", 0, 200, 200, StopwatchIcon) { Enclosable = true },
            }),
            new SubCategory("天秤", new[]
            {
                new ShapeDef("天秤", "（なし）", 0, 200, 200, BalanceIcon) { Enclosable = true },
            }),
            new SubCategory("的", new[]
            {
                new ShapeDef("的", "（なし）", 0, 200, 200, (w, h, a) => TargetIcon(w, h, false)) { Enclosable = true },
                new ShapeDef("的（矢が当たっている）", "（なし）", 0, 200, 200, (w, h, a) => TargetIcon(w, h, true)) { Enclosable = true },
            }),
            new SubCategory("ガードレール", new[]
            {
                new ShapeDef("ガードレール", "（なし）", 0, 200, 200, GuardrailIcon) { Enclosable = true },
            }),
            new SubCategory("鍵", new[]
            {
                new ShapeDef("鍵", "（なし）", 0, 200, 200, (w, h, a) => Fit(KeyShape(), w, h)) { Enclosable = true },
                new ShapeDef("鍵と錠", "（なし）", 0, 200, 200, KeyAndLockIcon) { Enclosable = true },
                new ShapeDef("錠", "（なし）", 0, 200, 200, (w, h, a) => Fit(LockShape(), w, h)) { Enclosable = true },
            }),
            new SubCategory("スピードメータ", new[]
            {
                new ShapeDef("スピードメータ", "（なし）", 0, 200, 200, SpeedometerIcon) { Enclosable = true },
            }),
            new SubCategory("脳", new[]
            {
                new ShapeDef("脳", "（なし）", 0, 200, 200, BrainIcon) { Enclosable = true },
            }),
            new SubCategory("マーク", new[]
            {
                new ShapeDef("星", "とがり具合", 0.5, 200, 200, Star) { Enclosable = true },
                new ShapeDef("ハート", "（なし）", 0, 200, 180, Heart) { Enclosable = true },
            }),
        }),
    };

    private static Geometry Poly(params Point[] pts)
    {
        var fig = new PathFigure { StartPoint = pts[0], IsClosed = true, IsFilled = true };
        for (int i = 1; i < pts.Length; i++) fig.Segments.Add(new LineSegment(pts[i], true));
        return new PathGeometry(new[] { fig });
    }

    private static Geometry OpenLine(params Point[] pts)
    {
        var fig = new PathFigure { StartPoint = pts[0], IsClosed = false, IsFilled = false };
        for (int i = 1; i < pts.Length; i++) fig.Segments.Add(new LineSegment(pts[i], true));
        return new PathGeometry(new[] { fig });
    }

    // ---- アイコン用ヘルパー（座標は 100 x srcH の仮想キャンバス上で組み立て、最後に w x h へ拡縮する） ----

    private static Geometry Fit(Geometry g, double w, double h, double srcH = 100)
    {
        if (g.IsFrozen) g = g.Clone();
        g.Transform = new ScaleTransform(w / 100, h / srcH);
        return g;
    }

    private static Geometry Circ(double cx, double cy, double r) => new EllipseGeometry(new Point(cx, cy), r, r);

    private static Geometry Box(double x, double y, double w, double h, double r = 0) =>
        new RectangleGeometry(new Rect(x, y, w, h), r, r);

    private static Geometry Union(params Geometry[] gs)
    {
        var acc = gs[0];
        for (int i = 1; i < gs.Length; i++) acc = new CombinedGeometry(GeometryCombineMode.Union, acc, gs[i]);
        return acc;
    }

    private static Geometry Minus(Geometry a, params Geometry[] holes)
    {
        foreach (var hole in holes) a = new CombinedGeometry(GeometryCombineMode.Exclude, a, hole);
        return a;
    }

    /// <summary>折れ線を太さ付きの面にする。</summary>
    private static Geometry Thick(double t, params Point[] pts) =>
        OpenLine(pts).GetWidenedPathGeometry(new Pen(Brushes.Black, t)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        });

    private static Geometry CodeIcon(double w, double h, double a)
    {
        double t = 4 + 10 * a;
        var g = Union(
            Thick(t, new(32, 12), new(10, 35), new(32, 58)),
            Thick(t, new(68, 12), new(90, 35), new(68, 58)),
            Thick(t, new(57, 8), new(43, 62)));
        return Fit(g, w, h, 70);
    }

    private static Geometry MagnifierIcon(double w, double h, double a)
    {
        double ring = 4 + 12 * a;
        var lens = Minus(Circ(42, 42, 30), Circ(42, 42, 30 - ring));
        var handle = Geometry.Parse("M63,63 L66.5,59.5 L91.5,84.5 L84.5,91.5 L59.5,66.5 Z");
        return Fit(Union(lens, handle), w, h);
    }

    private static Geometry RocketIcon(double w, double h, double a)
    {
        var body = Geometry.Parse("M50,4 C72,20 74,52 66,72 L34,72 C26,52 28,20 50,4 Z");
        var finL = Geometry.Parse("M35,50 L17,68 L17,84 L35,72 Z");
        var finR = Geometry.Parse("M65,50 L83,68 L83,84 L65,72 Z");
        var flame = Geometry.Parse("M41,77 L50,98 L59,77 Z");
        var rocket = Minus(Union(body, finL, finR), Circ(50, 36, 8));
        return Fit(Union(rocket, flame), w, h);
    }

    private static Geometry RobotIcon(double w, double h, double a)
    {
        var head = Minus(Box(10, 24, 80, 68, 12), Circ(33, 52, 8), Circ(67, 52, 8), Box(32, 72, 36, 6, 3));
        var g = Union(head,
            Box(2, 46, 8, 22, 3), Box(90, 46, 8, 22, 3),     // 耳
            Box(47, 12, 6, 14), Circ(50, 9, 6));             // アンテナ
        return Fit(g, w, h);
    }

    /// <summary>
    /// アイコンを枠で囲む。枠は塗りなしの線として加えるので、塗りつぶしてもアイコンだけが塗られる。
    /// kind: 0=なし 1=丸 2=角丸四角 3=四角
    /// </summary>
    public static Geometry Enclose(ShapeDef shape, double w, double h, double a, int kind, int[]? sides = null)
    {
        Geometry Make(double bw, double bh) =>
            shape.BuildSided != null && sides != null ? shape.BuildSided(bw, bh, a, sides) : shape.Build(bw, bh, a);

        if (kind == 0) return Make(w, h);

        const double scale = 0.58;
        double iw = w * scale, ih = h * scale;
        var inner = new GeometryGroup { Transform = new TranslateTransform((w - iw) / 2, (h - ih) / 2) };
        inner.Children.Add(Make(iw, ih));

        Geometry frame = kind switch
        {
            1 => new EllipseGeometry(new Rect(0, 0, w, h)),
            2 => new RectangleGeometry(new Rect(0, 0, w, h), Math.Min(w, h) * 0.18, Math.Min(w, h) * 0.18),
            _ => new RectangleGeometry(new Rect(0, 0, w, h)),
        };
        var outline = frame.GetFlattenedPathGeometry(0.01, ToleranceType.Absolute).Clone();
        foreach (var fig in outline.Figures) fig.IsFilled = false;

        var group = new GeometryGroup();
        group.Children.Add(inner);
        group.Children.Add(outline);
        return group;
    }

    private static Geometry BarChartIcon(double w, double h, double a)
    {
        var g = Union(
            Box(12, 60, 20, 25, 2), Box(40, 40, 20, 45, 2), Box(68, 20, 20, 65, 2),
            Box(4, 90, 92, 6, 3));                           // 下線
        return Fit(g, w, h);
    }

    private static Geometry BarChartArrowIcon(double w, double h, double a)
    {
        var arrow = Union(
            Thick(5, new(8, 52), new(34, 34), new(52, 44), new(86, 13)),
            Geometry.Parse("M94,6 L88.2,20.5 L78.9,10.1 Z"));
        var g = Union(
            Box(10, 70, 16, 20, 2), Box(36, 58, 16, 32, 2), Box(62, 46, 16, 44, 2),
            Box(4, 93, 92, 5, 2.5),
            arrow);
        return Fit(g, w, h);
    }

    private static Geometry CodeWindowIcon(double w, double h, double a)
    {
        var frame = Minus(Box(4, 10, 92, 80, 8), Box(9, 15, 82, 70, 4));
        var lines = Union(
            Thick(5, new(20, 32), new(44, 32)), Thick(5, new(50, 32), new(70, 32)),
            Thick(5, new(28, 46), new(58, 46)),
            Thick(5, new(28, 60), new(40, 60)), Thick(5, new(46, 60), new(72, 60)),
            Thick(5, new(20, 74), new(36, 74)));
        return Fit(Union(frame, lines), w, h);
    }

    private static Geometry LightBulbIcon(double w, double h, double a)
    {
        var bulb = Union(
            Circ(50, 45, 24),
            Geometry.Parse("M36,62 L64,62 L59,78 L41,78 Z"),
            Box(38, 81, 24, 5, 2.5), Box(43, 89, 14, 5, 2.5));
        bulb = Minus(bulb, Thick(3, new(42, 56), new(46, 47), new(50, 56), new(54, 47), new(58, 56)));

        // 光の筋（上・斜め上・左右）
        var rays = new List<Geometry>();
        foreach (double deg in new[] { -90, -45, -135, 0, 180 })
        {
            double rad = deg * Math.PI / 180;
            Point P(double r) => new(50 + Math.Cos(rad) * r, 45 + Math.Sin(rad) * r);
            rays.Add(Thick(4, P(33), P(43)));
        }
        return Fit(Union(bulb, Union(rays.ToArray())), w, h);
    }

    private static Geometry ClockIcon(double w, double h, double a)
    {
        var ring = Minus(Circ(50, 50, 44), Circ(50, 50, 36));
        var hands = Union(Thick(5, new(50, 50), new(50, 26)), Thick(5, new(50, 50), new(66, 58)));
        return Fit(Union(ring, hands), w, h);
    }

    private static Geometry StopwatchIcon(double w, double h, double a)
    {
        var ring = Minus(Circ(50, 56, 38), Circ(50, 56, 31));
        var hands = Union(Thick(5, new(50, 56), new(50, 34)), Thick(5, new(50, 56), new(64, 63)));
        var buttons = Union(
            Box(40, 4, 20, 7, 3), Box(46, 10, 8, 12),        // 上のボタン
            Thick(7, new(79, 22), new(86, 15)));             // 斜めのボタン
        return Fit(Union(ring, hands, buttons), w, h);
    }

    private static Geometry BalanceIcon(double w, double h, double a)
    {
        var g = Union(
            Circ(50, 15, 6), Box(47, 18, 6, 66, 2), Box(26, 84, 48, 8, 3),   // 支柱と台
            Thick(5, new(14, 26), new(86, 26)),                              // 横棒
            Thick(2.5, new(14, 26), new(2, 60)), Thick(2.5, new(14, 26), new(26, 60)),
            Thick(2.5, new(86, 26), new(74, 60)), Thick(2.5, new(86, 26), new(98, 60)),
            Geometry.Parse("M0,60 L28,60 A14,14 0 0 1 0,60 Z"),              // 左の皿
            Geometry.Parse("M72,60 L100,60 A14,14 0 0 1 72,60 Z"));          // 右の皿
        return Fit(g, w, h);
    }

    /// <summary>パズルのピース。sides は上・右・下・左の順で 0=凸 1=凹 2=平。</summary>
    private static Geometry Puzzle(double w, double h, double a, int[] sides)
    {
        double d = Math.Min(w, h) * (0.1 + 0.15 * a);       // 凸凹の出っ張り量。ピース本体はこの分だけ内側に取る
        double r = d * 0.6, nk = Math.Sqrt(r * r - (d - r) * (d - r));
        var tl = new Point(d, d);
        var tr = new Point(w - d, d);
        var br = new Point(w - d, h - d);
        var bl = new Point(d, h - d);

        var segs = new List<PathSegment>();
        void Edge(Point p, Point q, int kind)
        {
            if (kind == 2)
            {
                segs.Add(new LineSegment(q, true));
                return;
            }
            var v = q - p;
            var u = v / v.Length;
            var m = new Point((p.X + q.X) / 2, (p.Y + q.Y) / 2);
            segs.Add(new LineSegment(m - u * nk, true));
            // 時計回りにたどるので、凸は時計回り、凹は反時計回りの大きな弧になる
            segs.Add(new ArcSegment(m + u * nk, new Size(r, r), 0, true,
                kind == 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));
            segs.Add(new LineSegment(q, true));
        }
        Edge(tl, tr, sides[0]);
        Edge(tr, br, sides[1]);
        Edge(br, bl, sides[2]);
        Edge(bl, tl, sides[3]);

        return new PathGeometry(new[] { new PathFigure(tl, segs, true) });
    }

    private static Geometry Bubble(double w, double h, double a, int kind)
    {
        double bodyH = h * 0.8;
        double bw = w * 0.14;
        double x1 = w * (0.25 + 0.4 * a);
        Geometry body = kind switch
        {
            0 => new RectangleGeometry(new Rect(0, 0, w, bodyH)),
            1 => new RectangleGeometry(new Rect(0, 0, w, bodyH), Math.Min(w, bodyH) * 0.18, Math.Min(w, bodyH) * 0.18),
            _ => new EllipseGeometry(new Rect(0, 0, w, bodyH)),
        };
        // しっぽの付け根は本体の内側に置き、先端だけを下にはみ出させる
        var tail = Poly(new(x1, bodyH * 0.75), new(x1 + bw, bodyH * 0.75), new(x1 - bw * 0.3, h));
        return new CombinedGeometry(GeometryCombineMode.Union, body, tail);
    }

    private static Geometry TargetIcon(double w, double h, bool arrow)
    {
        var parts = new List<Geometry>
        {
            Minus(Circ(50, 50, 44), Circ(50, 50, 36)),
            Minus(Circ(50, 50, 28), Circ(50, 50, 20)),
            Circ(50, 50, 11),
        };
        if (arrow)
        {
            // 右上から中心へ刺さる矢。s は中心からの距離、side は矢に直交する方向のずれ
            Point Q(double s, double side) => new(50 + 0.7071 * (s + side), 50 - 0.7071 * (s - side));
            parts.Add(Thick(4, Q(0, 0), Q(52, 0)));
            foreach (double s in new[] { 38, 46 })
            {
                parts.Add(Thick(4, Q(s, 0), Q(s + 9, 8)));
                parts.Add(Thick(4, Q(s, 0), Q(s + 9, -8)));
            }
        }
        return Fit(Union(parts.ToArray()), w, h);
    }

    private static Geometry GuardrailIcon(double w, double h, double a)
    {
        var beam = Minus(Box(0, 24, 100, 30, 7), Box(8, 32, 84, 3.5, 1.75), Box(8, 42, 84, 3.5, 1.75));
        var g = Union(beam, Box(14, 54, 9, 38, 2), Box(45.5, 54, 9, 38, 2), Box(77, 54, 9, 38, 2));
        return Fit(g, w, h);
    }

    private static Geometry LockShape()
    {
        var shackle = Minus(
            Union(Circ(50, 34, 22), Box(28, 34, 44, 14)),
            Union(Circ(50, 34, 14), Box(36, 34, 28, 14)));
        var body = Minus(Box(18, 44, 64, 48, 8), Union(Circ(50, 62, 6), Box(47, 64, 6, 14, 2)));
        return Union(shackle, body);
    }

    private static Geometry KeyShape()
    {
        var head = Minus(Circ(24, 50, 22), Circ(24, 50, 9));
        return Union(head, Box(42, 44, 54, 12, 3), Box(70, 56, 8, 13, 1.5), Box(85, 56, 8, 17, 1.5));
    }

    private static Geometry KeyAndLockIcon(double w, double h, double a)
    {
        var lck = LockShape();
        lck.Transform = new MatrixTransform(new Matrix(0.62, 0, 0, 0.62, 2, 17.6));
        var key = Union(
            Minus(Circ(80, 24, 14), Circ(80, 24, 6)),
            Box(76, 36, 8, 52, 2), Box(84, 62, 10, 7, 1.5), Box(84, 74, 8, 7, 1.5));
        return Fit(Union(lck, key), w, h);
    }

    private static Geometry SpeedometerIcon(double w, double h, double a)
    {
        Point P(double deg, double r) =>
            new(50 + Math.Cos(deg * Math.PI / 180) * r, 52 + Math.Sin(deg * Math.PI / 180) * r);

        // 下側が開いた 270° の目盛り板
        var arc = new PathGeometry(new[]
        {
            new PathFigure(P(135, 38), new PathSegment[]
            {
                new ArcSegment(P(45, 38), new Size(38, 38), 0, true, SweepDirection.Clockwise, true),
            }, false) { IsFilled = false },
        });
        var dial = arc.GetWidenedPathGeometry(new Pen(Brushes.Black, 8)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        });

        var parts = new List<Geometry> { dial, Circ(50, 52, 7), Thick(5, P(0, 0), P(-50, 30)) };
        for (int k = 0; k < 5; k++)
        {
            double deg = 135 + k * 67.5;
            parts.Add(Thick(3, P(deg, 24), P(deg, 29)));
        }
        return Fit(Union(parts.ToArray()), w, h);
    }

    private static Geometry BrainIcon(double w, double h, double a)
    {
        // 左半球を作って、左右反転した右半球と合わせる
        Geometry Half(bool mirror)
        {
            double X(double x) => mirror ? 100 - x : x;
            return Union(Circ(X(32), 38, 20), Circ(X(20), 58, 15), Circ(X(38), 68, 18), Circ(X(44), 48, 16));
        }

        var brain = Union(Half(false), Half(true), Box(43, 76, 14, 18, 4));
        var folds = new List<Geometry>
        {
            Thick(3, new(50, 18), new(47, 34), new(53, 50), new(49, 66), new(50, 78)),   // 中央の溝
        };
        foreach (bool m in new[] { false, true })
        {
            double X(double x) => m ? 100 - x : x;
            folds.Add(Thick(2.5, new(X(24), 32), new(X(33), 41), new(X(26), 52)));
            folds.Add(Thick(2.5, new(X(36), 55), new(X(30), 64), new(X(40), 71)));
        }
        return Fit(Minus(brain, folds.ToArray()), w, h);
    }

    private static Geometry PersonIcon(double w, double h, double a)
    {
        var body = Geometry.Parse("M16,94 C16,68 30,56 50,56 C70,56 84,68 84,94 Z");
        return Fit(Union(Circ(50, 28, 17), body), w, h);
    }

    private static Geometry PeopleIcon(double w, double h, double a)
    {
        var center = Union(Circ(50, 26, 14), Geometry.Parse("M28,92 C28,66 38,53 50,53 C62,53 72,66 72,92 Z"));
        var left = Union(Circ(13, 43, 9), Geometry.Parse("M2,90 C2,72 7,61 15,61 C19,61 22,62 24,63 L24,90 Z"));
        var right = Union(Circ(87, 43, 9), Geometry.Parse("M98,90 C98,72 93,61 85,61 C81,61 78,62 76,63 L76,90 Z"));
        return Fit(Union(center, left, right), w, h);
    }

    private static Geometry ShieldIcon(double w, double h, double a)
    {
        var shield = Geometry.Parse("M50,4 L88,18 L88,48 C88,72 70,88 50,97 C30,88 12,72 12,48 L12,18 Z");
        var check = Thick(6 + 8 * a, new(31, 50), new(45, 64), new(70, 34));
        return Fit(Minus(shield, check), w, h);
    }

    private static Geometry Gear(double w, double h, double a, int teeth, bool hole)
    {
        double depth = 0.1 + 0.3 * a;          // 歯の高さ（外半径に対する比）
        double pitch = 2 * Math.PI / teeth;
        Point At(double ang, double r) =>
            new(w / 2 + Math.Cos(ang) * r * w / 2, h / 2 + Math.Sin(ang) * r * h / 2);

        var pts = new List<Point>();
        for (int i = 0; i < teeth; i++)
        {
            double c = -Math.PI / 2 + i * pitch;
            pts.Add(At(c - pitch * 0.28, 1 - depth));
            pts.Add(At(c - pitch * 0.17, 1));
            pts.Add(At(c + pitch * 0.17, 1));
            pts.Add(At(c + pitch * 0.28, 1 - depth));
        }
        var outline = Poly(pts.ToArray());
        if (!hole) return outline;

        double hr = (1 - depth) * 0.4;
        var hole1 = new EllipseGeometry(new Point(w / 2, h / 2), hr * w / 2, hr * h / 2);
        return new CombinedGeometry(GeometryCombineMode.Exclude, outline, hole1);
    }

    private static Geometry FileShape(double w, double h, double a, bool withLines)
    {
        double c = Math.Min(w, h) * a * 0.5;
        var g = new PathGeometry();
        g.Figures.Add(new PathFigure(new Point(0, 0), new PathSegment[]
        {
            new LineSegment(new Point(w - c, 0), true),
            new LineSegment(new Point(w, c), true),
            new LineSegment(new Point(w, h), true),
            new LineSegment(new Point(0, h), true),
        }, true));
        // 折り返しの線
        g.Figures.Add(new PathFigure(new Point(w - c, 0), new PathSegment[]
        {
            new LineSegment(new Point(w - c, c), true),
            new LineSegment(new Point(w, c), true),
        }, false) { IsFilled = false });

        if (withLines)
        {
            // 本文の罫線。最後の1本だけ短くする
            double[] ys = { 0.42, 0.55, 0.68, 0.81 };
            for (int i = 0; i < ys.Length; i++)
            {
                double x2 = i == ys.Length - 1 ? 0.55 : 0.82;
                g.Figures.Add(new PathFigure(new Point(w * 0.18, h * ys[i]), new PathSegment[]
                {
                    new LineSegment(new Point(w * x2, h * ys[i]), true),
                }, false) { IsFilled = false });
            }
        }
        return g;
    }

    /// <summary>円筒。tiers 段ぶんの区切り線（手前に膨らむ弧）を入れる。</summary>
    private static Geometry Cylinder(double w, double h, double a, int tiers)
    {
        double ry = Math.Min(h * 0.25, a * w * 0.25);
        double body = h - 2 * ry;
        var size = new Size(w / 2, ry);

        var g = new PathGeometry();
        g.Figures.Add(new PathFigure(new Point(0, ry), new PathSegment[]
        {
            new ArcSegment(new Point(w, ry), size, 0, false, SweepDirection.Clockwise, true),
            new LineSegment(new Point(w, h - ry), true),
            new ArcSegment(new Point(0, h - ry), size, 0, false, SweepDirection.Clockwise, true),
        }, true));

        for (int k = 0; k < tiers; k++)
        {
            double y = ry + k * body / tiers;
            g.Figures.Add(new PathFigure(new Point(0, y), new PathSegment[]
            {
                new ArcSegment(new Point(w, y), size, 0, false, SweepDirection.Counterclockwise, true),
            }, false) { IsFilled = false });
        }
        return g;
    }

    private static Geometry HomePlate(double w, double h, double a)
    {
        double d = Math.Min(w, a * h);
        return Poly(new(0, 0), new(w - d, 0), new(w, h / 2), new(w - d, h), new(0, h));
    }

    private static Geometry Chevron(double w, double h, double a)
    {
        double d = Math.Min(w / 2, a * h);
        return Poly(new(0, 0), new(w - d, 0), new(w, h / 2), new(w - d, h), new(0, h), new(d, h / 2));
    }

    private static Geometry RoundRect(double w, double h, double a)
    {
        double r = a * Math.Min(w, h) / 2;
        return new RectangleGeometry(new Rect(0, 0, w, h), r, r);
    }

    private static Geometry Parallelogram(double w, double h, double a)
    {
        double d = Math.Min(w * 0.9, a * h);
        return Poly(new(d, 0), new(w, 0), new(w - d, h), new(0, h));
    }

    private static Geometry RightArrow(double w, double h, double a)
    {
        double shaft = h * (0.15 + 0.7 * a);
        double sy = (h - shaft) / 2, ey = sy + shaft;
        double hl = Math.Min(w * 0.9, h * 0.6);
        return Poly(new(0, sy), new(w - hl, sy), new(w - hl, 0), new(w, h / 2), new(w - hl, h), new(w - hl, ey), new(0, ey));
    }

    private static Geometry DownArrow(double w, double h, double a)
    {
        double shaft = w * (0.15 + 0.7 * a);
        double sx = (w - shaft) / 2, ex = sx + shaft;
        double hl = Math.Min(h * 0.9, w * 0.6);
        return Poly(new(sx, 0), new(ex, 0), new(ex, h - hl), new(w, h - hl), new(w / 2, h), new(0, h - hl), new(sx, h - hl));
    }

    private static Geometry Star(double w, double h, double a)
    {
        double inner = 0.25 + 0.5 * (1 - a);
        var pts = new Point[10];
        for (int i = 0; i < 10; i++)
        {
            double r = i % 2 == 0 ? 1 : inner;
            double ang = -Math.PI / 2 + i * Math.PI / 5;
            pts[i] = new Point(w / 2 + Math.Cos(ang) * r * w / 2, h / 2 + Math.Sin(ang) * r * h / 2);
        }
        return Poly(pts);
    }

    private static Geometry Heart(double w, double h, double a)
    {
        var g = Geometry.Parse("M50,90 C10,60 0,35 0,25 C0,10 12,0 27,0 C38,0 46,6 50,14 C54,6 62,0 73,0 C88,0 100,10 100,25 C100,35 90,60 50,90 Z").Clone();
        g.Transform = new ScaleTransform(w / 100, h / 90);
        return g;
    }

    /// <summary>円から記号（線）を切り抜いたジオメトリ。</summary>
    private static Geometry Cut(double w, double h, Geometry mark, double thickness)
    {
        var circle = new EllipseGeometry(new Rect(0, 0, w, h));
        var widened = mark.GetWidenedPathGeometry(new Pen(Brushes.Black, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        });
        return new CombinedGeometry(GeometryCombineMode.Exclude, circle, widened);
    }

    private static Geometry CircleCheck(double w, double h, double a)
    {
        var mark = OpenLine(new(w * 0.27, h * 0.52), new(w * 0.43, h * 0.68), new(w * 0.74, h * 0.34));
        return Cut(w, h, mark, Math.Min(w, h) * (0.06 + 0.14 * a));
    }

    private static Geometry RoundBox(double w, double h) =>
        new RectangleGeometry(new Rect(0, 0, w, h), Math.Min(w, h) * 0.18, Math.Min(w, h) * 0.18);

    private static Geometry Widen(Geometry mark, double thickness) =>
        mark.GetWidenedPathGeometry(new Pen(Brushes.Black, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        });

    private static Geometry SquareCheck(double w, double h, double a)
    {
        var mark = OpenLine(new(w * 0.25, h * 0.52), new(w * 0.43, h * 0.70), new(w * 0.76, h * 0.32));
        return new CombinedGeometry(GeometryCombineMode.Exclude, RoundBox(w, h),
            Widen(mark, Math.Min(w, h) * (0.06 + 0.14 * a)));
    }

    private static Geometry SquareCross(double w, double h, double a)
    {
        var mark = new GeometryGroup();
        mark.Children.Add(OpenLine(new(w * 0.32, h * 0.32), new(w * 0.68, h * 0.68)));
        mark.Children.Add(OpenLine(new(w * 0.68, h * 0.32), new(w * 0.32, h * 0.68)));
        return new CombinedGeometry(GeometryCombineMode.Exclude, RoundBox(w, h),
            Widen(mark, Math.Min(w, h) * (0.06 + 0.14 * a)));
    }

    /// <summary>枠だけの四角の中にチェックを入れた、チェックボックス風の形。</summary>
    private static Geometry CheckBox(double w, double h, double a)
    {
        double t = Math.Min(w, h) * 0.07;
        var frame = new CombinedGeometry(GeometryCombineMode.Exclude, RoundBox(w, h),
            new RectangleGeometry(new Rect(t, t, w - 2 * t, h - 2 * t), Math.Min(w, h) * 0.1, Math.Min(w, h) * 0.1));
        var mark = OpenLine(new(w * 0.25, h * 0.52), new(w * 0.43, h * 0.70), new(w * 0.76, h * 0.32));
        return new CombinedGeometry(GeometryCombineMode.Union, frame,
            Widen(mark, Math.Min(w, h) * (0.06 + 0.14 * a)));
    }

    private static Geometry CircleCross(double w, double h, double a)
    {
        var mark = new GeometryGroup();
        mark.Children.Add(OpenLine(new(w * 0.32, h * 0.32), new(w * 0.68, h * 0.68)));
        mark.Children.Add(OpenLine(new(w * 0.68, h * 0.32), new(w * 0.32, h * 0.68)));
        return Cut(w, h, mark, Math.Min(w, h) * (0.06 + 0.14 * a));
    }
}
