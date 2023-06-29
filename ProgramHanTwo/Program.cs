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


public class SentimentData //класс входного набора данных
{
    [LoadColumn(0)] //этот атрибут описывает порядок данных каждого поля в виртуальной таблице. В данном случае э то 0-я колонка
    public string? SentimentText; //текст комментария
    //Label - это иными словами просто метка, 0 или 1.
    [LoadColumn(1), ColumnName("Label")]//этот атрибут описывает порядок данных каждого поля в виртуальной таблице. В данном случае это 1-я колонка
    //ColumnName("Label") - объявляет наименование колонки
    public bool Sentiment;//положительный он или нет
}

public class SentimentPrediction : SentimentData //класс прогноза, используется для оубчения модели
{
    //наследование здесь используется для того, чтобы выводить входные данные вместе с прогнозными
    //этот класс содержит два свойства,вычисляемые в модели. Это Score - необработанная оценка, вычисляемая в модели.
    //Probability - это уже обработанная оценка, вероятность, что коммент положительный.
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}

public class Program
{
    public static string connect = "Server=MYSQL8003.site4now.net;Database=db_a98823_hanazuk;Uid=a98823_hanazuk;Pwd=Hanazuky123";
    public static MySqlConnection con = new MySqlConnection(connect);

    //этот метод нужен для:
    /*загрузки данных;
     * Разделяет скачанный набор данных на наборы данных для обучения и тестирования.
     * Возвращает два набора данных — для обучения и для тестирования
     */
    public static TrainTestData LoadData(MLContext mlContext, string _dataPath)
    {
        IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentData>(_dataPath, hasHeader: false);
        //IDataView - отвечает за создание схемы данных, некой виртуальной таблицы.  hasHeader - будет ли у нашей таблицы заголовок
        //SentimentData - это вот как раз та схема, по которой будет строится таблица. Что-то вроде типа класса, на соснове которого будет строица таблица
        //LoadFromTextFile - принимает в качестве пораметров путь к обучающим и тестовым данным, чтобы наполнить ими нашу таблицу.
        TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
        //TrainTestData - это класс, который делит загружённые данные на тестовые и обучающие
        //testFraction - это процент тестовых данных, в наем случае 20%, так как анализируемых данных много.
        return splitDataView;
    }
    //этот метод нужен для:
    /* извлечение и преобразование данных
     * обучение модели;
     * прогноз тональности на основе тестовых данных;
     * возвращение модели
     */
    public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView splitTrainSet)
    {
        var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentData.SentimentText))
            //FeaturizeText - этот метод преобразует текст из столбца SentimentText вирутальной таблдицы в числовой столбец типа ключа Features, который используется алгоритмом машинного обучения.
            //Он добавляет его как новый столбец набора данных.
            //То екстьт теперьу нас три стоблбца в виртуальной таблице бех заголовка.
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));
        // так как у нас есть только два варинта ответа - это положительный или отрицательный, то нам понадобится двочная клссификация.
        //Метод Append добавляет алгоритм машинного обучения.
        //В данном случае SdcaLogisticRegression, который принимает наши тестовые данные(0 или 1) и наш текст(комментарии) в цифровом,понятном для компьютера виде. 
        var model = estimator.Fit(splitTrainSet); // далее на основе информационного супа компьютера и тестовых данных мы учим нашу модель
        return model; // далее уже обученную модель мы посылаем обратно в метод Main
    }
    //После того, как мы оубчили нашу модель, нам необходимо понять на сколько хорошо она усвоила материал
    //этот метод нужен для:
    /* загрузки тестового набора данных
     * создание средства оценки BinaryClassification;
     * оценка модели и создание метрик;
     * отображение метрик;
     */
    public static void Evaluate(MLContext mlContext, ITransformer model, IDataView splitTestSet)
    {
        Console.WriteLine("=============== Evaluating Model accuracy with Test data===============");
        IDataView predictions = model.Transform(splitTestSet);
        //метод Transform() используется, чтобы сделать прогнозы для нескольких входных строк тестового набора данных.
        //
        CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");
        // тут идёт сравнение того, что выдала модель с тем, что мы ей дали в качестве тестовых данных. в колонке Label хрантся наши правильные значения
        // и данный метод Evaluate как раз таки сравнивает на сколько прогнозы модели совпали с правильными данными.
        //metrics - это что-то вроде отчёта об обшибках, отчёт об её эффективности.
        Console.WriteLine();
        Console.WriteLine("Model quality metrics evaluation");
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
        Console.WriteLine("=============== End of model evaluation ===============");
        // вот этот блок с Console.WriteLine нужен просто для того, чтобы пользователь смог посмотреть отчёт об ошибках
        /*Accuracy - возвращает точность модели(доля правильных прогнозов в теством наборе данных)
         * AreaUnderRocCurve - это метрика показывает веренность модели в правильности классификации с положительными и отрицательными классами. 
         * Значение AreaUnderRocCurve должно быть максимально близким к единице.
         * F1Score - F1Score содержит F1-оценку модели, который является мерой баланса между точностью и полнотой. 
         * Значение F1Score должно быть максимально близким к единице.
         */
    }
    //этот метод нужен для:
    /* создание отдельного комментария тестовых данных;
     * прогноз тональности на основе тестовых данных;
     * объединение тестовых данных и прогнозов для создания отчетов;
     * отображение результатов прогнозирования
     */
    public static byte UseModelWithSingleItem(MLContext mlContext, ITransformer model, string result)
    {
        PredictionEngine<SentimentData, SentimentPrediction> predictionFunction = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
        //Класс PredictionEngine представляет собой удобный API, позволяющий осуществить прогнозирование на основе единственного экземпляра данных.
        //PredictionEngine не является потокобезопасным. Допустимо использовать в средах прототипов или средах с одним потоком.

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

        //Добавьте комментарий для проверки прогнозирования обученной модели в методе UseModelWithSingleItem(), создав экземпляр SentimentData
        //Что-то вроде собственной проверкидля обучения модели, когда мы сами создаём свой комментарий
        var resultPrediction = predictionFunction.Predict(sampleStatement);
        //Передаём тестовый комментарий в PredictionEngine
        //Функция Predict() создает прогноз по одной строке данных.
        if (Convert.ToBoolean(resultPrediction.Prediction))
            return 1;
        else 
            return 0;
    }

    public static MLContext mlContext = new MLContext();

    public static TrainTestData splitDataView = LoadData(mlContext, Directory.GetCurrentDirectory() + "\\wwwroot\\txt\\yelp_labelled.txt");//загрузка обучающих данных ддля модели
    
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


        
        //string _dataPath = "C:\\Users\\hanaz\\source\\repos\\ProgramHanTwo\\ProgramHanTwo\\wwwroot\\txt\\yelp_labelled.txt"; //путь по которому содержатсья наши обучающие данные

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


