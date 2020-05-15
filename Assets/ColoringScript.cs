using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

public class ColoringScript : MonoBehaviour
{
    public MeshRenderer target = null;
    public GameObject canvas = null;
    public RawImage viewL, viewR;
    UnityEngine.Rect capRect;
    Texture2D capTexture;
    Texture2D colTexture;
    Texture2D binTexture;
    Mat bgr, bin;
    // Start is called before the first frame update
    void Start()
    {
        int w = Screen.width;
        int h = Screen.height;
        int sx = (int)(w * 0.2);
        int sy = (int)(h*0.3);
        w = (int)(w*0.6);
        h = (int)(h * 0.4);
        capRect = new UnityEngine.Rect(sx,sy,w,h);
        capTexture = new Texture2D(w,h,TextureFormat.RGB24, false);
    }

    IEnumerator ImageProcessing()
    {
        canvas.SetActive(false);//Canvas上のUIを一時的に消す.
		yield return new WaitForEndOfFrame();//フレーム終了を待つ.
        CreateImage();
        Point[] corners;
        FindRect( out corners );
        TransformImage( corners );
        ShowImage();
        bgr.Release();
        bin.Release();
	}
	void TransformImage( Point[] corners)
	{
        //4頂点が検出されていなければ何もしない
		if (corners == null) return;
        SortCorners(corners);
		//検出された4頂点の座標を入力
		Point2f[] input = { corners[0], corners[1], corners[2], corners[3] };
		//テクスチャとして使用する正方形画像の4頂点の座標を入力
		Point2f[] square = { new Point2f(0, 0), new Point2f(0, 255), new Point2f(255, 255), new Point2f(255, 0) };
		//歪んだ四角形を正方形に変換するパラメータを計算
		Mat transform = Cv2.GetPerspectiveTransform(input, square);
		//変換パラメータに基づいて画像を生成
		Cv2.WarpPerspective(bgr,bgr,transform, new Size(256, 256));
		int s = (int)(256*0.05);
		int w = (int)(256*0.9);
		OpenCvSharp.Rect innerRect = new OpenCvSharp.Rect(s,s,w,w);
		bgr = bgr[innerRect];
    }
	void SortCorners(Point[] corners)
	{
        System.Array.Sort(corners, (a, b) => a.X.CompareTo(b.X));
		if (corners[0].Y > corners[1].Y)
		{
			Point temp = corners[0];
			corners[0] = corners[1];
			corners[1] = temp;
		}
        if (corners[3].Y > corners[2].Y)
		{
			Point temp = corners[2];
			corners[2] = corners[3];
			corners[3] = temp;
		}
	}
    void FindRect(out Point[] corners)
	{
        //頂点をnull(空)で初期化
		corners = null;
		//輪郭を構成する点と階層
		Point[][] contours;
		HierarchyIndex[] h;
		//輪郭認識
		bin.FindContours(out contours, out h, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
		//面積が最大となる輪郭を処理対象とする
		double maxArea = 0;
		for(int i = 0; i < contours.Length; i++)
        {
            // 輪郭.
            double length = Cv2.ArcLength(contours[i], true);
			//多角形近似(全周の1%以内の誤差を許容)
			Point[] tmp = Cv2.ApproxPolyDP( contours[i], length * 0.01f, true);
            double area = Cv2.ContourArea(contours[i]);
			if ( tmp.Length == 4 && area > maxArea)
			{
				maxArea = area;
				corners = tmp;
			}
		}
		//if (corners != null)
		//{
  //          bgr.DrawContours(new Point[][] { corners }, 0, Scalar.Red, 5);
  //          //各頂点の位置に円を描画
		//	for(int i = 0; i < corners.Length; i++)
		//	{
		//		bgr.Circle(corners[i], 20, Scalar.Blue, 5);
		//	}
  //      }
    }
    void CreateImage()
	{
        capTexture.ReadPixels(capRect, 0, 0);
		capTexture.Apply();
        //Texure2DをMatに変換.
		bgr = OpenCvSharp.Unity.TextureToMat(capTexture);
		//カラー画像をグレースケール(濃淡)画像に変換 .
		bin = bgr.CvtColor(ColorConversionCodes.BGR2GRAY);
		//しきい値を自動で見つけて二値化。結果を白黒反転.
		bin = bin.Threshold(100, 255, ThresholdTypes.Otsu);
		Cv2.BitwiseNot(bin, bin);
        canvas.SetActive(true);//Canvas上のUIを再表示
    }
    void ShowImage()
	{
        //Textureが画像を保持しているならいったん削除
		if (colTexture != null) { DestroyImmediate(colTexture); }
		if (binTexture != null) { DestroyImmediate(binTexture); }
		//Matをテクスチャに変換
		colTexture = OpenCvSharp.Unity.MatToTexture(bgr);
		binTexture = OpenCvSharp.Unity.MatToTexture(bin);
		//RawImageに画像を表示
		viewL.texture = colTexture;
		viewR.texture = binTexture;
		//スクショ画像をモデルに適用
		target.material.mainTexture = colTexture;
		canvas.SetActive(true);
    }
    public void StartCV()
    {
        StartCoroutine(ImageProcessing());//コルーチンの実行 } ここに記述
    }
     // Update is called once per frame
    void Update()
    {
        
    }
}
