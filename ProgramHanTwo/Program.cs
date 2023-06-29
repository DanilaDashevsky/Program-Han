using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.ML;
using Microsoft.ML.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Program_Han.Controllers;
using System;
using System.Net;
using System.Web;
using static Microsoft.ML.DataOperationsCatalog;


public class SentimentData //����� �������� ������ ������
{
    [LoadColumn(0)] //���� ������� ��������� ������� ������ ������� ���� � ����������� �������. � ������ ������ � �� 0-� �������
    public string? SentimentText; //����� �����������
    //Label - ��� ����� ������� ������ �����, 0 ��� 1.
    [LoadColumn(1), ColumnName("Label")]//���� ������� ��������� ������� ������ ������� ���� � ����������� �������. � ������ ������ ��� 1-� �������
    //ColumnName("Label") - ��������� ������������ �������
    public bool Sentiment;//������������� �� ��� ���
}

public class SentimentPrediction : SentimentData //����� ��������, ������������ ��� �������� ������
{
    //������������ ����� ������������ ��� ����, ����� �������� ������� ������ ������ � �����������
    //���� ����� �������� ��� ��������,����������� � ������. ��� Score - �������������� ������, ����������� � ������.
    //Probability - ��� ��� ������������ ������, �����������, ��� ������� �������������.
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}

public class Program
{
    public static string connect = "Server=MYSQL8003.site4now.net;Database=db_a98823_hanazuk;Uid=a98823_hanazuk;Pwd=Hanazuky123";
    public static MySqlConnection con = new MySqlConnection(connect);

    //���� ����� ����� ���:
    /*�������� ������;
     * ��������� ��������� ����� ������ �� ������ ������ ��� �������� � ������������.
     * ���������� ��� ������ ������ � ��� �������� � ��� ������������
     */
    public static TrainTestData LoadData(MLContext mlContext, string _dataPath)
    {
        IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentData>(_dataPath, hasHeader: false);
        //IDataView - �������� �� �������� ����� ������, ����� ����������� �������.  hasHeader - ����� �� � ����� ������� ���������
        //SentimentData - ��� ��� ��� ��� �� �����, �� ������� ����� �������� �������. ���-�� ����� ���� ������, �� ������� �������� ����� ������� �������
        //LoadFromTextFile - ��������� � �������� ���������� ���� � ��������� � �������� ������, ����� ��������� ��� ���� �������.
        TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        //TrainTestData - ��� �����, ������� ����� ���������� ������ �� �������� � ���������
        //testFraction - ��� ������� �������� ������, � ���� ������ 20%, ��� ��� ������������� ������ �����.
        return splitDataView;
    }
    //���� ����� ����� ���:
    /* ���������� � �������������� ������
     * �������� ������;
     * ������� ����������� �� ������ �������� ������;
     * ����������� ������
     */
    public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView splitTrainSet)
    {
        var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentData.SentimentText))
            //FeaturizeText - ���� ����� ����������� ����� �� ������� SentimentText ����������� �������� � �������� ������� ���� ����� Features, ������� ������������ ���������� ��������� ��������.
            //�� ��������� ��� ��� ����� ������� ������ ������.
            //�� ������ ������� ��� ��� �������� � ����������� ������� ��� ���������.
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));
        // ��� ��� � ��� ���� ������ ��� ������� ������ - ��� ������������� ��� �������������, �� ��� ����������� ������� ������������.
        //����� Append ��������� �������� ��������� ��������.
        //� ������ ������ SdcaLogisticRegression, ������� ��������� ���� �������� ������(0 ��� 1) � ��� �����(�����������) � ��������,�������� ��� ���������� ����. 
        var model = estimator.Fit(splitTrainSet); // ����� �� ������ ��������������� ���� ���������� � �������� ������ �� ���� ���� ������
        return model; // ����� ��� ��������� ������ �� �������� ������� � ����� Main
    }
    //����� ����, ��� �� ������� ���� ������, ��� ���������� ������ �� ������� ������ ��� ������� ��������
    //���� ����� ����� ���:
    /* �������� ��������� ������ ������
     * �������� �������� ������ BinaryClassification;
     * ������ ������ � �������� ������;
     * ����������� ������;
     */
    public static void Evaluate(MLContext mlContext, ITransformer model, IDataView splitTestSet)
    {
        Console.WriteLine("=============== Evaluating Model accuracy with Test data===============");
        IDataView predictions = model.Transform(splitTestSet);
        //����� Transform() ������������, ����� ������� �������� ��� ���������� ������� ����� ��������� ������ ������.
        //
        CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");
        // ��� ��� ��������� ����, ��� ������ ������ � ���, ��� �� �� ���� � �������� �������� ������. � ������� Label ������� ���� ���������� ��������
        // � ������ ����� Evaluate ��� ��� ���� ���������� �� ������� �������� ������ ������� � ����������� �������.
        //metrics - ��� ���-�� ����� ������ �� ��������, ����� �� � �������������.
        Console.WriteLine();
        Console.WriteLine("Model quality metrics evaluation");
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
        Console.WriteLine("=============== End of model evaluation ===============");
        // ��� ���� ���� � Console.WriteLine ����� ������ ��� ����, ����� ������������ ���� ���������� ����� �� �������
        /*Accuracy - ���������� �������� ������(���� ���������� ��������� � ������� ������ ������)
         * AreaUnderRocCurve - ��� ������� ���������� ���������� ������ � ������������ ������������� � �������������� � �������������� ��������. 
         * �������� AreaUnderRocCurve ������ ���� ����������� ������� � �������.
         * F1Score - F1Score �������� F1-������ ������, ������� �������� ����� ������� ����� ��������� � ��������. 
         * �������� F1Score ������ ���� ����������� ������� � �������.
         */
    }
    //���� ����� ����� ���:
    /* �������� ���������� ����������� �������� ������;
     * ������� ����������� �� ������ �������� ������;
     * ����������� �������� ������ � ��������� ��� �������� �������;
     * ����������� ����������� ���������������
     */
    public static byte UseModelWithSingleItem(MLContext mlContext, ITransformer model, string result)
    {
        PredictionEngine<SentimentData, SentimentPrediction> predictionFunction = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
        //����� PredictionEngine ������������ ����� ������� API, ����������� ����������� ��������������� �� ������ ������������� ���������� ������.
        //PredictionEngine �� �������� ����������������. ��������� ������������ � ������ ���������� ��� ������ � ����� �������.

        var toLanguage = "ru";//English
        var fromLanguage = "en";//Deutsch
        var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={toLanguage}&tl={fromLanguage}&dt=t&q={HttpUtility.UrlEncode(result)}";

        var webClient = new WebClient
        {
            Encoding = System.Text.Encoding.UTF8
        };
        result = webClient.DownloadString(url);
        result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);

        SentimentData sampleStatement = new SentimentData
        {
            SentimentText = result
        };

        //�������� ����������� ��� �������� ��������������� ��������� ������ � ������ UseModelWithSingleItem(), ������ ��������� SentimentData
        //���-�� ����� ����������� ����������� �������� ������, ����� �� ���� ������ ���� �����������
        var resultPrediction = predictionFunction.Predict(sampleStatement);
        //������� �������� ����������� � PredictionEngine
        //������� Predict() ������� ������� �� ����� ������ ������.
        if (Convert.ToBoolean(resultPrediction.Prediction))
            return 1;
        else 
            return 0;
    }

    public static MLContext mlContext = new MLContext();

    public static TrainTestData splitDataView = LoadData(mlContext, Directory.GetCurrentDirectory() + "\\wwwroot\\txt\\yelp_labelled.txt");//�������� ��������� ������ ���� ������
    
    public static ITransformer model = BuildAndTrainModel(mlContext, splitDataView.TrainSet);

    private static async Task Main(string[] args)
    {
        await con.OpenAsync();
        var builder = WebApplication.CreateBuilder(args);
        Console.WriteLine(AppContext.BaseDirectory);
        builder.Services.AddAuthentication("Cookies");
        builder.Services.AddAuthentication(
           CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();


        
        //string _dataPath = "C:\\Users\\hanaz\\source\\repos\\ProgramHanTwo\\ProgramHanTwo\\wwwroot\\txt\\yelp_labelled.txt"; //���� �� �������� ����������� ���� ��������� ������

        Evaluate(mlContext, model, splitDataView.TestSet);

        //UseModelWithSingleItem(mlContext, model, result);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();
        app.UseAuthentication();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}


